using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FarFarWestVR;

internal sealed record ReleaseUpdate(string Tag, string Version, string Notes, string AssetName, Uri DownloadUrl);

internal sealed class UpdateService
{
    internal const string CurrentVersion = "0.4.0";
    internal const string Repository = "pr-io/Far-Far-VR";
    internal static readonly string LocalRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FarFarWestVRLauncher");
    internal static readonly string PendingRoot = Path.Combine(LocalRoot, "pending_update");
    private static readonly string TokenPath = Path.Combine(LocalRoot, "github_token.txt");
    private readonly HttpClient client;
    private readonly string profile;

    internal UpdateService(string profile, HttpClient? client = null)
    {
        this.profile = profile;
        this.client = client ?? new HttpClient();
    }

    internal static bool StartPendingApply(string[] args)
    {
        string package = Path.Combine(PendingRoot, "package");
        string marker = Path.Combine(PendingRoot, "prepared.txt");
        bool explicitlyRequested = args.Contains("--apply-update", StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(marker) || !Directory.Exists(package))
        {
            if (explicitlyRequested) throw new IOException("There is no prepared Far Far VR update to apply.");
            return false;
        }

        string script = Path.Combine(package, "Apply-Update.ps1");
        if (!File.Exists(script)) throw new IOException("The prepared update is missing its safe-apply helper.");
        var start = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        foreach (string value in new[]
        {
            "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script,
            "-InstallRoot", AppContext.BaseDirectory,
            "-StagingRoot", package,
            "-WaitForProcessId", Environment.ProcessId.ToString()
        }) start.ArgumentList.Add(value);
        if (Process.Start(start) is null) throw new IOException("Windows could not start the update apply helper.");
        return true;
    }

    internal async Task<ReleaseUpdate?> CheckAsync(CancellationToken cancellationToken = default)
    {
        string token = ReadToken();
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.github.com/repos/{Repository}/releases?per_page=30");
        request.Headers.UserAgent.ParseAdd("FarFarVR-Launcher/0.4.0");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new IOException("The private update repository is unavailable. Check the launcher token and repository access.");
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return FindUpdate(document.RootElement, CurrentVersion);
    }

    internal static string StatusPath(string profile) => Path.Combine(profile, "farfarvr-updates.txt");

    internal async Task StageLatestAsync(CancellationToken cancellationToken = default)
    {
        ReleaseUpdate? release = await CheckAsync(cancellationToken);
        if (release is null) throw new IOException("No newer published Far Far VR release is available.");

        Directory.CreateDirectory(PendingRoot);
        string archive = Path.Combine(PendingRoot, "download.zip");
        string temporary = Path.Combine(PendingRoot, "package-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, release.DownloadUrl);
            request.Headers.UserAgent.ParseAdd("FarFarVR-Launcher/0.4.0");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ReadToken());
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > 2_000_000_000)
                throw new IOException("The update package exceeds the 2 GB safety limit.");
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var destination = new FileStream(archive, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                byte[] buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += read;
                    if (total > 2_000_000_000) throw new IOException("The update package exceeds the 2 GB safety limit.");
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }

            Directory.CreateDirectory(temporary);
            using (var zip = ZipFile.OpenRead(archive))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    string destination = Path.GetFullPath(Path.Combine(temporary, entry.FullName));
                    string prefix = Path.GetFullPath(temporary) + Path.DirectorySeparatorChar;
                    if (!destination.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                        !destination.Equals(Path.GetFullPath(temporary), StringComparison.OrdinalIgnoreCase))
                        throw new IOException("The update package contains an invalid path.");
                }
                zip.ExtractToDirectory(temporary, overwriteFiles: false);
            }
            ValidatePackage(temporary);

            string package = Path.Combine(PendingRoot, "package");
            if (Directory.Exists(package)) Directory.Delete(package, recursive: true);
            Directory.Move(temporary, package);
            File.WriteAllText(Path.Combine(PendingRoot, "prepared.txt"), release.Tag);
            WriteStatus("prepared", release.Version, "Update staged. Exit Far Far West, then reopen the launcher to install it.");
        }
        finally
        {
            if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true);
        }
    }

    internal void WriteStatus(string state, string version, string notes)
    {
        Directory.CreateDirectory(profile);
        string path = StatusPath(profile);
        string temporary = path + ".pending";
        File.WriteAllText(temporary, $"state={state}\nversion={version}\nnotes:\n{notes}");
        File.Move(temporary, path, overwrite: true);
    }

    internal static ReleaseUpdate? FindUpdate(JsonElement releases, string currentVersion)
    {
        if (releases.ValueKind != JsonValueKind.Array) throw new IOException("GitHub returned an invalid release list.");
        Version current = ParseVersion(currentVersion);
        ReleaseUpdate? best = null;
        Version? bestVersion = null;
        foreach (JsonElement release in releases.EnumerateArray())
        {
            if (release.TryGetProperty("draft", out var draft) && draft.ValueKind == JsonValueKind.True) continue;
            if (!release.TryGetProperty("tag_name", out var tagElement) || tagElement.ValueKind != JsonValueKind.String) continue;
            string tag = tagElement.GetString()!;
            if (!TryParseVersion(tag, out Version? version) || version <= current || (bestVersion != null && version <= bestVersion)) continue;
            if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) continue;
            string expectedAsset = $"FarFarVR-{tag}.zip";
            JsonElement matchingAsset = assets.EnumerateArray().FirstOrDefault(asset =>
                asset.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String &&
                name.GetString()!.Equals(expectedAsset, StringComparison.Ordinal));
            if (matchingAsset.ValueKind != JsonValueKind.Object ||
                !matchingAsset.TryGetProperty("browser_download_url", out var url) || url.ValueKind != JsonValueKind.String ||
                !Uri.TryCreate(url.GetString(), UriKind.Absolute, out Uri? download) || download.Scheme != Uri.UriSchemeHttps)
                continue;
            string notes = release.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.String
                ? body.GetString() ?? ""
                : "";
            if (notes.Length > 12000) notes = notes[..12000] + "\n…";
            bestVersion = version;
            best = new ReleaseUpdate(tag, "v" + version, notes, expectedAsset, download);
        }
        return best;
    }

    internal static bool TryParseVersion(string value, out Version? version)
    {
        version = null;
        Match match = Regex.Match(value, "^v?(\\d+)\\.(\\d+)\\.(\\d+)(?:[-+][A-Za-z0-9.-]+)?$");
        if (!match.Success || !Version.TryParse($"{match.Groups[1]}.{match.Groups[2]}.{match.Groups[3]}", out Version? parsed)) return false;
        version = parsed;
        return true;
    }

    private static Version ParseVersion(string value) =>
        TryParseVersion(value, out Version? version) ? version! : throw new ArgumentException("Invalid launcher version.", nameof(value));

    private string ReadToken()
    {
        if (!File.Exists(TokenPath))
            throw new IOException($"Private update access is not configured. Save a GitHub token with repository Contents: read access to {TokenPath}.");
        string token = File.ReadAllText(TokenPath).Trim();
        if (token.Length is < 20 or > 512 || token.Any(char.IsWhiteSpace))
            throw new IOException("The GitHub token file is empty or malformed.");
        return token;
    }

    private static void ValidatePackage(string root)
    {
        foreach (string required in new[] { "Far Far VR.exe", "UE5.8\\UEVRBackend.dll", "Profile\\config.txt", "manifest.json", "Apply-Update.ps1" })
            if (!File.Exists(Path.Combine(root, required))) throw new IOException("The release package is incomplete: " + required);
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "manifest.json")));
        if (manifest.RootElement.ValueKind != JsonValueKind.Array) throw new IOException("The update package manifest is invalid.");
        string? backendHash = null;
        foreach (JsonElement entry in manifest.RootElement.EnumerateArray())
        {
            if (!entry.TryGetProperty("File", out var fileValue) || fileValue.ValueKind != JsonValueKind.String ||
                !entry.TryGetProperty("SHA256", out var hashValue) || hashValue.ValueKind != JsonValueKind.String)
                throw new IOException("The update package manifest contains an invalid entry.");
            string relative = fileValue.GetString()!.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            string full = Path.GetFullPath(Path.Combine(root, relative));
            string prefix = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
                throw new IOException("The update package manifest references an invalid or missing file.");
            string expected = hashValue.GetString()!;
            if (!Regex.IsMatch(expected, "^[0-9A-Fa-f]{64}$")) throw new IOException("The update package manifest has an invalid SHA-256 hash.");
            string actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(full)));
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new IOException("The update package failed its file integrity check: " + relative);
            if (relative.Equals(@"UE5.8\UEVRBackend.dll", StringComparison.OrdinalIgnoreCase)) backendHash = actual;
        }
        if (backendHash is null || !Setup.CompatibleBackendHashes.Contains(backendHash, StringComparer.OrdinalIgnoreCase))
            throw new IOException("The update package does not contain a pinned compatible VR backend.");
    }
}
