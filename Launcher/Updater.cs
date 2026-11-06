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
    internal static readonly string CurrentVersion = ProductVersion.Value;
    internal const string Repository = "pr-io/Far-Far-VR";
    internal static readonly string LocalRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FarFarWestVRLauncher");
    internal static readonly string PendingRoot = Path.Combine(LocalRoot, "pending_update");
    private static readonly string TokenPath = Path.Combine(LocalRoot, "github_token.txt");
    private readonly HttpClient client;
    private readonly string profile;
    private readonly Func<string?> tokenProvider;
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DownloadInactivityTimeout = TimeSpan.FromSeconds(30);

    internal UpdateService(string profile, HttpClient? client = null, Func<string?>? tokenProvider = null)
    {
        this.profile = profile;
        this.client = client ?? new HttpClient();
        this.tokenProvider = tokenProvider ?? ReadToken;
    }

    internal static bool StartPendingApply(string[] args)
    {
        string package = Path.Combine(PendingRoot, "package");
        string marker = Path.Combine(PendingRoot, "prepared.txt");
        bool recovering = File.Exists(Path.Combine(AppContext.BaseDirectory,".farfarvr-update-transaction.json"));
        bool explicitlyRequested = args.Contains("--apply-update", StringComparer.OrdinalIgnoreCase);
        if (!recovering && (!File.Exists(marker) || !Directory.Exists(package)))
        {
            if (explicitlyRequested) throw new IOException("There is no prepared Far Far VR update to apply.");
            return false;
        }

        string script = recovering ? RecoveryScript(AppContext.BaseDirectory) : Path.Combine(package, "Apply-Update.ps1");
        if (!File.Exists(script)) throw new IOException("The prepared update is missing its safe-apply helper.");
        if (!recovering) ValidatePackage(package);
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
        if (recovering) start.ArgumentList.Add("-RecoverOnly");
        if (Process.Start(start) is null) throw new IOException("Windows could not start the update apply helper.");
        return true;
    }

    internal static string RecoveryScript(string installRoot)
    {
        string root=Path.GetFullPath(installRoot);
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,".farfarvr-update-transaction.json")));
        string folder=document.RootElement.GetProperty("BackupFolder").GetString() ?? "";
        if(Path.GetFileName(folder)!=folder || !folder.StartsWith(".farfarvr-update-rollback-",StringComparison.Ordinal))
            throw new IOException("The interrupted update has an invalid recovery folder. Backups were preserved.");
        // This copy exists before the first replacement, even if the installed
        // launcher and apply helper were interrupted between different versions.
        return Path.Combine(root,folder,"recover.ps1");
    }

    internal async Task<ReleaseUpdate?> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CheckTimeout);
        try
        {
            using var request = CreateRequest(HttpMethod.Get,
                $"https://api.github.com/repos/{Repository}/releases?per_page=30");
            request.Headers.UserAgent.ParseAdd("FarFarVR-Launcher/" + CurrentVersion);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new IOException("The update repository is unavailable. If it is private, check the launcher token and repository access.");
            response.EnsureSuccessStatusCode();
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token), cancellationToken: timeout.Token);
            return FindUpdate(document.RootElement, CurrentVersion);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IOException("The update check timed out. Check your connection and try again.");
        }
    }

    internal static string StatusPath(string profile) => Path.Combine(profile, "data", "farfarvr-updates.txt");
    internal static string CommandPath(string profile) => Path.Combine(profile, "data", "farfarvr-update-command.txt");

    internal async Task StageLatestAsync(CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(DownloadTimeout);
        var token = timeout.Token;
        ReleaseUpdate? release = await CheckAsync(token);
        if (release is null) throw new IOException("No newer published Far Far VR release is available.");

        Directory.CreateDirectory(PendingRoot);
        string archive = Path.Combine(PendingRoot, "download.zip");
        string temporary = Path.Combine(PendingRoot, "package-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var request = CreateRequest(HttpMethod.Get, release.DownloadUrl);
            request.Headers.UserAgent.ParseAdd("FarFarVR-Launcher/" + CurrentVersion);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > 2_000_000_000)
                throw new IOException("The update package exceeds the 2 GB safety limit.");
            await using (var source = await response.Content.ReadAsStreamAsync(token))
            await using (var destination = new FileStream(archive, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                await CopyDownloadAsync(source, destination, DownloadInactivityTimeout, token);
            }

            Directory.CreateDirectory(temporary);
            using (var zip = ZipFile.OpenRead(archive))
            {
                long expandedBytes = 0;
                if (zip.Entries.Count > 10000) throw new IOException("The update contains too many files.");
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    token.ThrowIfCancellationRequested();
                    expandedBytes = checked(expandedBytes + entry.Length);
                    if (expandedBytes > 4_000_000_000) throw new IOException("The expanded update exceeds the 4 GB limit.");
                    string destination = Path.GetFullPath(Path.Combine(temporary, entry.FullName));
                    string prefix = Path.GetFullPath(temporary) + Path.DirectorySeparatorChar;
                    if (!destination.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                        !destination.Equals(Path.GetFullPath(temporary), StringComparison.OrdinalIgnoreCase))
                        throw new IOException("The update package contains an invalid path.");
                }
                zip.ExtractToDirectory(temporary, overwriteFiles: false);
            }
            ValidatePackage(temporary);
            token.ThrowIfCancellationRequested();

            string package = Path.Combine(PendingRoot, "package");
            if (Directory.Exists(package)) Directory.Delete(package, recursive: true);
            Directory.Move(temporary, package);
            File.WriteAllText(Path.Combine(PendingRoot, "prepared.txt"), release.Tag);
            WriteStatus("prepared", release.Version, "Update staged. Exit Far Far West, then reopen the launcher to install it.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IOException("The update download timed out. Check your connection and try again.");
        }
        finally
        {
            if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true);
            if (File.Exists(archive)) File.Delete(archive);
        }
    }

    internal static async Task CopyDownloadAsync(Stream source, Stream destination,
        TimeSpan inactivityTimeout, CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(inactivityTimeout);
            int read;
            try { read = await source.ReadAsync(buffer.AsMemory(), timeout.Token); }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new IOException("The update download stopped responding. Check your connection and try again.");
            }
            if (read == 0) return;
            total += read;
            if (total > 2_000_000_000) throw new IOException("The update package exceeds the 2 GB safety limit.");
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    internal void WriteStatus(string state, string version, string notes)
    {
        // The headset bridge is only needed after profile installation. A desktop
        // update check must not create a partial profile before first launch.
        if (!File.Exists(Path.Combine(profile, "config.txt"))) return;
        Directory.CreateDirectory(Path.GetDirectoryName(StatusPath(profile))!);
        string path = StatusPath(profile);
        string temporary = path + ".pending";
        File.WriteAllText(temporary, $"state={state}\nversion={version}\nresponseId={Guid.NewGuid():N}\nnotes:\n{notes}");
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
            if (release.TryGetProperty("prerelease", out var prerelease) && prerelease.ValueKind == JsonValueKind.True) continue;
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
                !Uri.TryCreate(url.GetString(), UriKind.Absolute, out Uri? download) ||
                !IsReleaseDownload(download, tag, expectedAsset))
                continue;
            string notes = release.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.String
                ? body.GetString() ?? ""
                : "";
            if (notes.Length > 12000) notes = notes[..12000] + "\nâ€¦";
            bestVersion = version;
            best = new ReleaseUpdate(tag, "v" + version, notes, expectedAsset, download);
        }
        return best;
    }

    internal static bool TryParseVersion(string value, out Version? version)
    {
        version = null;
        Match match = Regex.Match(value, "^v?(\\d+)\\.(\\d+)\\.(\\d+)(?:[+][A-Za-z0-9.-]+)?$");
        if (!match.Success || !Version.TryParse($"{match.Groups[1]}.{match.Groups[2]}.{match.Groups[3]}", out Version? parsed)) return false;
        version = parsed;
        return true;
    }

    internal static bool IsReleaseDownload(Uri uri, string tag, string asset) =>
        uri.Scheme == Uri.UriSchemeHttps && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
        uri.IsDefaultPort && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
        uri.AbsolutePath.Equals($"/{Repository}/releases/download/{tag}/{asset}", StringComparison.Ordinal);

    private static Version ParseVersion(string value) =>
        TryParseVersion(value, out Version? version) ? version! : throw new ArgumentException("Invalid launcher version.", nameof(value));

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri) =>
        CreateRequest(method, new Uri(uri, UriKind.Absolute));

    private HttpRequestMessage CreateRequest(HttpMethod method, Uri uri)
    {
        var request = new HttpRequestMessage(method, uri);
        // A repository token belongs only on the repository API request. Never
        // send it to an asset URL or a host supplied by release metadata.
        string? token = uri.Scheme == Uri.UriSchemeHttps && uri.IsDefaultPort &&
            uri.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase) &&
            uri.AbsolutePath.StartsWith($"/repos/{Repository}/", StringComparison.Ordinal)
            ? tokenProvider() : null;
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return request;
    }

    private static string? ReadToken()
    {
        if (!File.Exists(TokenPath))
            return null;
        string token = File.ReadAllText(TokenPath).Trim();
        if (token.Length is < 20 or > 512 || token.Any(char.IsWhiteSpace))
            throw new IOException("The GitHub token file is empty or malformed.");
        return token;
    }

    internal static void ValidatePackage(string root)
    {
        string[] requiredFiles = { "Far Far VR.exe", "UE5.8\\README-RUNTIME.txt", "Profile\\config.txt", "manifest.json", "Apply-Update.ps1", "Runtime\\backend.ffvrdelta" };
        // Player releases acquire UEVR from upstream; never redistribute it.
        string[] excludedRuntime = { "UEVRBackend.dll", "LuaVR.dll", "openxr_loader.dll", "openvr_api.dll" };
        if (Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Any(file => excludedRuntime.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)))
            throw new IOException("The player update contains third-party runtime binaries; use the runtime-separated release package.");
        foreach (string required in requiredFiles)
            if (!File.Exists(Path.Combine(root, required))) throw new IOException("The release package is incomplete: " + required);
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "manifest.json")));
        JsonElement entries = manifest.RootElement;
        if (entries.ValueKind == JsonValueKind.Object &&
            entries.TryGetProperty("files", out JsonElement files))
            entries = files;
        if (entries.ValueKind != JsonValueKind.Array) throw new IOException("The update package manifest is invalid.");
        var verifiedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement entry in entries.EnumerateArray())
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
            if (!verifiedFiles.Add(relative)) throw new IOException("The update manifest contains duplicate files.");
        }
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, file);
            if (relative != "manifest.json" && !verifiedFiles.Contains(relative))
                throw new IOException("The update contains a file missing from its integrity manifest: " + relative);
        }
        // The downloaded release may carry a newer engine than this launcher.
        // Verify completeness/integrity here; its launcher owns the new pins.
        foreach (string required in requiredFiles.Where(file => file != "manifest.json"))
            if (!verifiedFiles.Contains(required.Replace('\\', Path.DirectorySeparatorChar)))
                throw new IOException("The update manifest does not cover a required file: " + required);
    }
}
