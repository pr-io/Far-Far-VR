using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FarFarWestVR;

internal sealed class Setup
{
    public string GameFolder { get; set; } = "";
    public string RuntimeFolder { get; set; } = "";
    public bool CloseAfterLaunch { get; set; } = true;
    public const string ShippingRelative = @"FarFarWest\Binaries\Win64\FarFarWest-Win64-Shipping.exe";
    internal static bool UsePreviewPreferences { get; set; }
    internal const string CurrentBackendHash = "240C5ACCB5A1E02862101A5809B10C22D9403A1B5ED1A3095DDF4CB29BA5EC73";
    private const string OpenXrRegistryPath = @"SOFTWARE\Khronos\OpenXR\1";
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
        if (string.IsNullOrWhiteSpace(value.GameFolder)) value.GameFolder = DetectGame() ?? "";
        if (!UsePreviewPreferences)
        {
            // Root and packaged launchers must select the same corrected
            // runtime. An older selected runtime cannot silently lose fixes.
            foreach (string candidate in new[] { Path.Combine(AppContext.BaseDirectory, "UE5.8"), Path.Combine(AppContext.BaseDirectory, "dist", "FarFarVR", "UE5.8") })
            {
                string backend = Path.Combine(candidate, "UEVRBackend.dll");
                if (File.Exists(backend) && Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(backend))) == CurrentBackendHash)
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
    public void Validate()
    {
        ValidateSteam();
        ValidateGame();
        ValidateRuntime();
        ValidateOpenXrRuntime();
    }

    public static string SteamStatus()
    {
        try
        {
            string? install = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            install = string.IsNullOrWhiteSpace(install)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")
                : install.Replace('/', Path.DirectorySeparatorChar);
            return File.Exists(Path.Combine(install, "steam.exe"))
                ? "Steam installation found."
                : "Steam is required to start Far Far West. Install Steam and sign in before continuing.";
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return "Could not check Steam: " + ex.Message;
        }
    }

    private static void ValidateSteam()
    {
        string status = SteamStatus();
        if (!status.StartsWith("Steam installation found.", StringComparison.Ordinal))
            throw new IOException(status);
    }

    public void ValidateGame()
    {
        if (string.IsNullOrWhiteSpace(GameFolder) || !File.Exists(GamePath)) throw new IOException("Select the FarFarWest installation folder in Setup & options.");
    }

    public void ValidateRuntime()
    {
        foreach (string name in new[] { "UEVRBackend.dll", "LuaVR.dll", "openxr_loader.dll", "openvr_api.dll" })
            if (string.IsNullOrWhiteSpace(RuntimeFolder) || !File.Exists(Path.Combine(RuntimeFolder, name)))
                throw new IOException($"The compatible Far Far VR runtime is missing '{name}'. Use a complete, licensed Far Far VR package; stock UEVR builds are not compatible.");
        using var backend = File.OpenRead(BackendPath);
        string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(backend));
        bool accepted = hash == CurrentBackendHash;
        bool menuPreview = UsePreviewPreferences && hash == "DCDFF1DAB571EF183DE7D5697A60F450A8D50308F0E67B15785382919261B019";
        if (!accepted && !menuPreview)
            throw new IOException($"This VR runtime is not the required Far Far West build (expected backend SHA-256 {CurrentBackendHash}, found {hash}). Stock or other UEVR builds are rejected to preserve the required game fixes.");
    }

    public static string OpenXrStatus()
    {
        try
        {
            string? activeRuntime = Registry.GetValue($@"HKEY_CURRENT_USER\{OpenXrRegistryPath}", "ActiveRuntime", null) as string
                ?? Registry.GetValue($@"HKEY_LOCAL_MACHINE\{OpenXrRegistryPath}", "ActiveRuntime", null) as string;
            if (string.IsNullOrWhiteSpace(activeRuntime))
                return "No active OpenXR runtime is configured. Install and start your headset's PCVR software, then set it as the active OpenXR runtime.";
            if (!Path.IsPathRooted(activeRuntime) || !File.Exists(activeRuntime))
                return $"The active OpenXR runtime is configured but its target is missing: {activeRuntime}";
            return $"OpenXR runtime configured: {Path.GetFileName(activeRuntime)}. Start the headset's PCVR app and connect the headset before playing.";
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return "Could not check the active OpenXR runtime: " + ex.Message;
        }
    }

    private static void ValidateOpenXrRuntime()
    {
        string status = OpenXrStatus();
        if (!status.StartsWith("OpenXR runtime configured:", StringComparison.Ordinal))
            throw new IOException(status);
    }
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
        foreach (string name in new[] { "farfarvr_settings.lua", "farfarvr_menu.lua", "farfarvr_input.lua", "farfarvr_hud.lua", "farfarvr_objectives.lua", "farfarvr_comfort.lua", "farfarvr_mount.lua", "farfarvr_render_fix.lua" })
        {
            string source = Path.Combine(AppContext.BaseDirectory, "Profile", "scripts", name);
            if (!File.Exists(source)) throw new IOException("A Far Far VR module is missing. Use a complete package with its Profile folder.");
            string target = Path.Combine(ProfileFolder, "scripts", name);
            if (!File.Exists(target) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(target))) changes.Add((source,target));
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
            ["VR_AimMethod"]="0", ["UI_FollowView"]="false",
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
            bool valid = pair[0] == "VR_AimMethod" ? Regex.IsMatch(value, "^[0-5]$") : value is "true" or "false";
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

internal static class PackageVerifier
{
    private static readonly string[] ProfileFiles =
    [
        "config.txt",
        "cameras.txt",
        "VRSettings.json",
        "_interaction_profiles_oculus_touch_controller.json",
        "scripts/farfarvr_settings.lua",
        "scripts/farfarvr_menu.lua",
        "scripts/farfarvr_input.lua",
        "scripts/farfarvr_hud.lua",
        "scripts/farfarvr_objectives.lua",
        "scripts/farfarvr_comfort.lua",
        "scripts/farfarvr_mount.lua",
        "scripts/farfarvr_render_fix.lua"
    ];

    internal static void Validate(string packageRoot)
    {
        string profileRoot = Path.Combine(packageRoot, "Profile");
        foreach (string relative in ProfileFiles)
        {
            string path = Path.Combine(profileRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) throw new IOException("Packaged Far Far VR profile file is missing: " + relative);
        }

        var setup = new Setup { RuntimeFolder = Path.Combine(packageRoot, "UE5.8") };
        setup.ValidateRuntime();
    }
}
