using Microsoft.Win32;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FarFarWestVR;

internal sealed class Setup
{
    public string GameFolder { get; set; } = "";
    public string RuntimeFolder { get; set; } = "";
    public bool CloseAfterLaunch { get; set; } = false;
    public const string ShippingRelative = @"FarFarWest\Binaries\Win64\FarFarWest-Win64-Shipping.exe";
    internal static bool UsePreviewPreferences { get; set; }
    internal const string CurrentBackendHash = "2F30D9353B02435F0D34A6505D2FF494F4DACD20783F8D72CB408F9AEBE41558";
    internal const string BaseBackendHash = "FEFF6417359F6E36ABAC6577598A86B96D485A023A850F8614043AA0A508C8CB";
    internal static readonly string[] CompatibleBackendHashes = new[]
    {
        CurrentBackendHash,
        BaseBackendHash,
        "0CFB68B928FFA5F5516BA866AFB676EB716F99A72B05B736CC5FB3053DA94029",
        "179BB4A6F61574AC16621E2C7C9E2E27F3AA328462243417D177527647315558"
    };
    public const string UevrRuntimeDownloadUrl = "https://github.com/joeyhodge/UEVR/releases/download/102626_FFR_SWGR_Patch/uevr.zip";
    public const string UevrRuntimeArchiveHash = "FDF325C62BA43BC1DA98ED5F453633FA106CAEF0DF9CB8121566F7AA138F6D55";
    internal const string CompatibleLuaVrHash = "0431946710BD006C221C89863C1D49ADF80F8847817EFCE198D549F5B1B64B0A";
    internal const string CompatibleOpenVrApiHash = "845D4B947CB5DD2DE2964F6C71536D03366939E4798E574FDD20838D318B540D";
    internal const string CompatibleOpenXrLoaderHash = "3BB4EB190024F92EECF4A0BF33669B5D0B27571DDF4C36A33C58DE199EE536C1";
    public static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FarFarWestVRLauncher", UsePreviewPreferences ? "preview-settings.json" : "settings.json");
    public static string ProfileFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UnrealVRMod", "FarFarWest-Win64-Shipping");

    public static Setup Load()
    {
        Setup value = new();
        if (File.Exists(SettingsPath))
        {
            try { value = JsonSerializer.Deserialize<Setup>(File.ReadAllText(SettingsPath)) ?? new(); }
            catch (Exception ex) when (ex is IOException or JsonException) { LauncherLog.Write("Settings could not be read: " + ex.Message); }
        }
        value.CloseAfterLaunch = false; // Far Far VR remains available throughout the session.
        if (string.IsNullOrWhiteSpace(value.GameFolder) && OperatingSystem.IsWindows()) value.GameFolder = DetectGame() ?? "";
        if (!UsePreviewPreferences)
        {
            // Root and packaged launchers must select the same corrected
            // runtime. An older selected runtime cannot silently lose fixes.
            foreach (string candidate in new[] { Path.Combine(AppContext.BaseDirectory, "UE5.8"), Path.Combine(AppContext.BaseDirectory, "dist", "FarFarVR", "UE5.8") })
            {
                string backend = Path.Combine(candidate, "UEVRBackend.dll");
                if (File.Exists(backend) && CompatibleBackendHashes.Contains(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(backend)))))
                {
                    value.RuntimeFolder = candidate;
                    break;
                }
            }
        }
        if (string.IsNullOrWhiteSpace(value.RuntimeFolder)) value.RuntimeFolder = Path.Combine(AppContext.BaseDirectory, "UE5.8");
        return value;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        string pending = SettingsPath + ".tmp";
        File.WriteAllText(pending, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(pending, SettingsPath, true);
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public string GamePath => Path.Combine(GameFolder, ShippingRelative);

    [System.Text.Json.Serialization.JsonIgnore]
    public string BackendPath => Path.Combine(RuntimeFolder, "UEVRBackend.dll");

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsRuntimeInstalled
    {
        get
        {
            if (string.IsNullOrWhiteSpace(RuntimeFolder) || !Directory.Exists(RuntimeFolder)) return false;
            foreach (string name in new[] { "UEVRBackend.dll", "LuaVR.dll", "openxr_loader.dll", "openvr_api.dll" })
            {
                if (!File.Exists(Path.Combine(RuntimeFolder, name))) return false;
            }
            try
            {
                using var backend = File.OpenRead(BackendPath);
                string backendHash = Convert.ToHexString(SHA256.HashData(backend));
                bool backendAccepted = CompatibleBackendHashes.Contains(backendHash);
                bool menuPreview = UsePreviewPreferences && backendHash == "DCDFF1DAB571EF183DE7D5697A60F450A8D50308F0E67B15785382919261B019";
                if (!backendAccepted && !menuPreview) return false;

                using var lua = File.OpenRead(Path.Combine(RuntimeFolder, "LuaVR.dll"));
                if (Convert.ToHexString(SHA256.HashData(lua)) != CompatibleLuaVrHash) return false;

                using var openvr = File.OpenRead(Path.Combine(RuntimeFolder, "openvr_api.dll"));
                if (Convert.ToHexString(SHA256.HashData(openvr)) != CompatibleOpenVrApiHash) return false;

                using var openxr = File.OpenRead(Path.Combine(RuntimeFolder, "openxr_loader.dll"));
                if (Convert.ToHexString(SHA256.HashData(openxr)) != CompatibleOpenXrLoaderHash) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public void Validate(bool allowPendingRuntime = false)
    {
        if (string.IsNullOrWhiteSpace(GameFolder) || !File.Exists(GamePath)) throw new IOException("Select the FarFarWest installation folder in Setup & options.");
        if (allowPendingRuntime && !IsRuntimeInstalled) return;
        foreach (string name in new[] { "UEVRBackend.dll", "LuaVR.dll", "openxr_loader.dll", "openvr_api.dll" })
            if (!File.Exists(Path.Combine(RuntimeFolder, name))) throw new IOException("VR runtime file missing: " + name + ". Start in VR to download automatically, or select the compatible runtime folder in Settings.");

        using var backend = File.OpenRead(BackendPath);
        string backendHash = Convert.ToHexString(SHA256.HashData(backend));
        bool accepted = CompatibleBackendHashes.Contains(backendHash);
        bool menuPreview = UsePreviewPreferences && backendHash == "DCDFF1DAB571EF183DE7D5697A60F450A8D50308F0E67B15785382919261B019";
        if (!accepted && !menuPreview)
            throw new IOException("This VR runtime does not contain the current Far Far West fixes. Use the UE5.8 folder from the current Far Far VR package.");

        using var lua = File.OpenRead(Path.Combine(RuntimeFolder, "LuaVR.dll"));
        if (Convert.ToHexString(SHA256.HashData(lua)) != CompatibleLuaVrHash)
            throw new IOException("LuaVR.dll does not match the compatible runtime checksum. Start in VR to repair automatically.");

        using var openvr = File.OpenRead(Path.Combine(RuntimeFolder, "openvr_api.dll"));
        if (Convert.ToHexString(SHA256.HashData(openvr)) != CompatibleOpenVrApiHash)
            throw new IOException("openvr_api.dll does not match the compatible runtime checksum. Start in VR to repair automatically.");

        using var openxr = File.OpenRead(Path.Combine(RuntimeFolder, "openxr_loader.dll"));
        if (Convert.ToHexString(SHA256.HashData(openxr)) != CompatibleOpenXrLoaderHash)
            throw new IOException("openxr_loader.dll does not match the compatible runtime checksum. Start in VR to repair automatically.");
    }

    public static async Task EnsureRuntimeAsync(Setup setup, IProgress<string>? progress = null, CancellationToken token = default)
    {
        if (setup.IsRuntimeInstalled) return;

        progress?.Report("VR runtime missing. Fetching compatible UEVR components...");
        LauncherLog.Write($"VR runtime missing in '{setup.RuntimeFolder}'. Starting auto-fetch from {UevrRuntimeDownloadUrl}");

        Directory.CreateDirectory(setup.RuntimeFolder);
        string tempZip = Path.Combine(Path.GetTempPath(), $"uevr_{Guid.NewGuid():N}.zip");
        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FarFarVR-Launcher/0.4.0 (PR_VR)");
                using var response = await httpClient.GetAsync(UevrRuntimeDownloadUrl, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                await using var contentStream = await response.Content.ReadAsStreamAsync(token);
                await using var fileStream = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                var buffer = new byte[81920];
                long totalRead = 0;
                int read;
                DateTime lastReport = DateTime.UtcNow;

                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read, token);
                    totalRead += read;
                    if (DateTime.UtcNow - lastReport > TimeSpan.FromMilliseconds(250))
                    {
                        lastReport = DateTime.UtcNow;
                        if (totalBytes.HasValue && totalBytes.Value > 0)
                        {
                            int pct = (int)((totalRead * 100) / totalBytes.Value);
                            progress?.Report($"Downloading VR runtime from public repository... {pct}%");
                        }
                        else
                        {
                            progress?.Report($"Downloading VR runtime from public repository... ({totalRead / 1048576} MB)");
                        }
                    }
                }
            }

            progress?.Report("Verifying VR runtime archive integrity...");
            byte[] hashBytes;
            using (var fileStream = File.OpenRead(tempZip))
            {
                hashBytes = SHA256.HashData(fileStream);
            }
            string archiveHash = Convert.ToHexString(hashBytes);
            if (!string.Equals(archiveHash, UevrRuntimeArchiveHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"VR runtime download checksum mismatch (expected {UevrRuntimeArchiveHash}, got {archiveHash}).");
            }

            progress?.Report("Extracting VR runtime components...");
            LauncherLog.Write("Archive verified. Extracting components...");
            using (var archive = ZipFile.OpenRead(tempZip))
            {
                var filesToExtract = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "UEVRBackend.dll",
                    "LuaVR.dll",
                    "openxr_loader.dll",
                    "openvr_api.dll"
                };

                foreach (var entry in archive.Entries)
                {
                    if (filesToExtract.Contains(entry.Name))
                    {
                        string destPath = Path.Combine(setup.RuntimeFolder, entry.Name);
                        entry.ExtractToFile(destPath, overwrite: true);
                        LauncherLog.Write($"Extracted {entry.Name} to {destPath}");
                    }
                }
            }

            string sourceNotice = Path.Combine(setup.RuntimeFolder, "BUILD-SOURCE.txt");
            if (!File.Exists(sourceNotice))
            {
                File.WriteAllText(sourceNotice,
                    "UEVR compatibility build: joeyhodge/UEVR\n" +
                    "Release: 102626_FFR_SWGR_Patch\n" +
                    "https://github.com/joeyhodge/UEVR/releases/tag/102626_FFR_SWGR_Patch\n" +
                    "Auto-fetched by Far Far VR Launcher (PR_VR) on first setup.\n" +
                    $"Archive SHA256: {UevrRuntimeArchiveHash}\n" +
                    "Powered by UEVR created by praydog.\n");
            }

            progress?.Report("VR runtime components installed successfully.");
            LauncherLog.Write("VR runtime components extracted and ready.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LauncherLog.Write("Failed to auto-fetch VR runtime: " + ex);
            throw new IOException("Failed to download VR runtime: " + ex.Message + ". Verify your internet connection or install compatible UEVR runtime files manually.", ex);
        }
        finally
        {
            try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { }
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static string? DetectGame()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? steam = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
        if (!string.IsNullOrWhiteSpace(steam)) roots.Add(steam);
        roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));
        foreach (string root in roots.ToArray())
        {
            string vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf)) continue;
            try
            {
                foreach (Match match in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                    roots.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
            }
            catch (IOException ex) { LauncherLog.Write("Steam library unavailable: " + ex.Message); }
        }
        foreach (string root in roots)
        {
            string manifest = Path.Combine(root, "steamapps", "appmanifest_3124540.acf");
            string name = "FarFarWest";
            if (File.Exists(manifest))
            {
                try { var match = Regex.Match(File.ReadAllText(manifest), "\"installdir\"\\s+\"([^\"]+)\""); if (match.Success) name = match.Groups[1].Value; }
                catch (IOException ex) { LauncherLog.Write(ex.Message); }
            }
            string folder = Path.GetFullPath(Path.Combine(root, "steamapps", "common", name));
            if (File.Exists(Path.Combine(folder, ShippingRelative))) return folder;
        }
        return null;
    }

    public static void EnsureProfile()
    {
        // An existing profile belongs to the player. Never merge or overwrite it implicitly.
        if (File.Exists(Path.Combine(ProfileFolder, "config.txt"))) { EnsureMenuModule(); RecoverComfortOverrides(); return; }
        if (Directory.Exists(ProfileFolder) && Directory.EnumerateFileSystemEntries(ProfileFolder).Any())
            throw new IOException("An incomplete VR profile exists. Back it up and resolve it before starting; it was left unchanged.");
        string bundled = Path.Combine(AppContext.BaseDirectory, "Profile");
        if (!File.Exists(Path.Combine(bundled, "config.txt"))) throw new IOException("Bundled profile is missing. Use a complete Far Far VR package.");
        string staging = ProfileFolder + ".install-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(staging);
        foreach (string file in Directory.EnumerateFiles(bundled, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(staging, Path.GetRelativePath(bundled, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        if (Directory.Exists(ProfileFolder)) Directory.Delete(ProfileFolder); // Empty directory only; races fail safely.
        Directory.Move(staging, ProfileFolder);
        LauncherLog.Write("Installed bundled profile for first use.");
    }

    private static void EnsureMenuModule()
    {
        var changes = new List<(string Source, string Target)>();
        foreach (string name in new[] { "farfarvr_settings.lua", "farfarvr_menu.lua", "farfarvr_input.lua", "farfarvr_hud.lua", "farfarvr_objectives.lua", "farfarvr_comfort.lua", "farfarvr_mount.lua", "farfarvr_effects.lua", "farfarvr_render_fix.lua" })
        {
            string source = Path.Combine(AppContext.BaseDirectory, "Profile", "scripts", name);
            if (!File.Exists(source)) throw new IOException("A Far Far VR module is missing. Use a complete package with its Profile folder.");
            string target = Path.Combine(ProfileFolder, "scripts", name);
            if (!File.Exists(target) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(target))) changes.Add((source,target));
        }
        foreach (string name in new[] { "farfarvr_controls.png", "farfarvr_controls.jpg" })
        {
            string source = Path.Combine(AppContext.BaseDirectory, "Profile", name);
            if (File.Exists(source))
            {
                string target = Path.Combine(ProfileFolder, name);
                if (!File.Exists(target) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(target))) changes.Add((source,target));
                string targetTex = Path.Combine(ProfileFolder, "textures", name);
                if (!File.Exists(targetTex) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(targetTex))) changes.Add((source,targetTex));
            }
        }
        if (changes.Count == 0) return;
        var running = Process.GetProcessesByName("FarFarWest-Win64-Shipping");
        bool gameRunning = running.Length > 0;
        foreach (var process in running) process.Dispose();
        if (gameRunning) throw new IOException("Close Far Far West so the launcher can install the updated VR modules before starting.");
        foreach (var change in changes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(change.Target)!);
            File.Copy(change.Source, change.Target + ".pending", true);
            if (File.Exists(change.Target)) File.Replace(change.Target + ".pending", change.Target, change.Target + ".backup-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ"));
            else File.Move(change.Target + ".pending", change.Target);
        }
        LauncherLog.Write("Installed Far Far VR modules; existing profile settings preserved.");
    }

    private static void RecoverComfortOverrides()
    {
        // UEVR can save temporary menu/cinema values before script teardown.
        // Recover only our recorded overrides, before the backend reads
        // config. Never touch a running game or unrelated player preferences.
        var processes = Process.GetProcessesByName("FarFarWest-Win64-Shipping");
        bool running = processes.Length > 0;
        foreach (var process in processes) process.Dispose();
        if (running) return;
        string journal = Path.Combine(ProfileFolder, "data", "farfarvr-comfort-restore.txt");
        if (!File.Exists(journal)) return;
        var overrides = new Dictionary<string,string> {
            ["VR_MovementOrientation"]="0", ["VR_AimMethod"]="0", ["UI_FollowView"]="false",
            ["VR_DecoupledPitchUIAdjust"]="false", ["VR_2DScreenMode"]="true",
            ["VR_RoomscaleMovement"]="false", ["VR_NativeStereoFix"]="false",
            ["VR_DecoupledPitch"]="false"
        };
        string configPath = Path.Combine(ProfileFolder, "config.txt");
        string original = File.ReadAllText(configPath), recovered = original;
        foreach (string line in File.ReadLines(journal))
        {
            string[] pair = line.Split('=', 2);
            if (pair.Length != 2 || !overrides.TryGetValue(pair[0], out string? temporary)) continue;
            string value = pair[1];
            bool valid = (pair[0] == "VR_AimMethod" || pair[0] == "VR_MovementOrientation") ? Regex.IsMatch(value, "^[0-5]$") : value is "true" or "false";
            if (!valid) continue;
            recovered = Regex.Replace(recovered, "(?m)^(" + Regex.Escape(pair[0]) + "=)" + temporary + "(?=\\r?$)", match => match.Groups[1].Value + value);
        }
        if (recovered == original) return;
        File.Copy(configPath, configPath + ".before-comfort-recovery-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ"));
        File.WriteAllText(configPath + ".pending", recovered);
        File.Move(configPath + ".pending", configPath, true);
        LauncherLog.Write("Recovered recorded menu/cinema overrides before VR startup.");
    }
}
