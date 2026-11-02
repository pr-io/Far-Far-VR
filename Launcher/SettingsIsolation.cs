using System.Diagnostics;
using System.Text.Json;

namespace FarFarWestVR;

internal sealed class SessionState
{
    public bool InVrSession { get; set; }
    public int ProcessId { get; set; }
    public DateTime Timestamp { get; set; }
}

internal static class SettingsIsolation
{
    public static readonly string GameSavedFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FarFarWest", "Saved");
    public static readonly string GameConfigFolder = Path.Combine(GameSavedFolder, "Config", "Windows");
    public static readonly string GameSaveGamesFolder = Path.Combine(GameSavedFolder, "SaveGames");

    public static readonly string GameIniPath = Path.Combine(GameConfigFolder, "GameUserSettings.ini");
    public static readonly string GameInputSavePath = Path.Combine(GameSaveGamesFolder, "EnhancedInputUserSettings.sav");
    public static readonly string GameUserSavePath = Path.Combine(GameSaveGamesFolder, "userSettings.sav");

    public static readonly string LauncherDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FarFarWestVRLauncher");
    public static readonly string VrSettingsFolder = Path.Combine(LauncherDataFolder, "vr_settings");
    public static readonly string DesktopSettingsFolder = Path.Combine(LauncherDataFolder, "desktop_settings");
    public static readonly string SessionStatePath = Path.Combine(LauncherDataFolder, "session_state.json");

    public static string BundledVrSettingsFolder => Path.Combine(AppContext.BaseDirectory, "Profile", "vr_settings");
    public static string BundledDefaultSettingsFolder => Path.Combine(AppContext.BaseDirectory, "Profile", "default_pc_settings");

    private static readonly object Gate = new();

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(GameConfigFolder);
        Directory.CreateDirectory(GameSaveGamesFolder);
        Directory.CreateDirectory(LauncherDataFolder);
        Directory.CreateDirectory(VrSettingsFolder);
        Directory.CreateDirectory(DesktopSettingsFolder);
    }

    public static void SeedIfMissing()
    {
        EnsureDirectories();

        // Seed VR settings from bundled template if not yet present
        if (Directory.Exists(BundledVrSettingsFolder))
        {
            CopyDirectoryFilesIfMissing(BundledVrSettingsFolder, VrSettingsFolder);
        }

        // Seed Desktop settings from bundled default template if not yet present
        if (Directory.Exists(BundledDefaultSettingsFolder))
        {
            CopyDirectoryFilesIfMissing(BundledDefaultSettingsFolder, DesktopSettingsFolder);
        }
    }

    private static void CopyDirectoryFilesIfMissing(string sourceDir, string targetDir)
    {
        foreach (string file in Directory.EnumerateFiles(sourceDir))
        {
            string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
            if (!File.Exists(targetFile))
            {
                File.Copy(file, targetFile, true);
            }
        }
    }

    public static bool IsInVrSession()
    {
        lock (Gate)
        {
            if (!File.Exists(SessionStatePath)) return false;
            try
            {
                var state = JsonSerializer.Deserialize<SessionState>(File.ReadAllText(SessionStatePath));
                return state?.InVrSession == true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static void EnsureDesktopState()
    {
        lock (Gate)
        {
            SeedIfMissing();
            if (!File.Exists(SessionStatePath)) return;

            try
            {
                var state = JsonSerializer.Deserialize<SessionState>(File.ReadAllText(SessionStatePath));
                if (state != null && state.InVrSession)
                {
                    bool processRunning = false;
                    if (state.ProcessId > 0)
                    {
                        try
                        {
                            using var p = Process.GetProcessById(state.ProcessId);
                            if (!p.HasExited && p.ProcessName == "FarFarWest-Win64-Shipping")
                                processRunning = true;
                        }
                        catch
                        {
                            processRunning = false;
                        }
                    }

                    if (!processRunning)
                    {
                        LauncherLog.Write("Previous VR session interrupted; restoring desktop settings.");
                        ExitVrSessionInternal(saveVrEdits: false);
                    }
                }
            }
            catch (Exception ex)
            {
                LauncherLog.Write("Error checking session state: " + ex.Message);
            }
        }
    }

    public static void EnterVrSession(int processId = 0)
    {
        lock (Gate)
        {
            SeedIfMissing();

            // If we are not currently in a VR session, backup the current desktop settings
            if (!IsInVrSession())
            {
                BackupDesktopSettings();
            }

            // Deploy VR settings to active game folders
            DeployVrSettings();

            // Repair VR social bindings on the active input save
            if (File.Exists(GameInputSavePath))
            {
                try
                {
                    if (SocialBindings.Repair(GameInputSavePath))
                    {
                        LauncherLog.Write("Verified VR social gamepad bindings and push-to-talk.");
                    }
                }
                catch (Exception ex)
                {
                    LauncherLog.Write("Note on VR input binding check: " + ex.Message);
                }
            }

            // Record session state
            var state = new SessionState
            {
                InVrSession = true,
                ProcessId = processId,
                Timestamp = DateTime.UtcNow
            };
            File.WriteAllText(SessionStatePath, JsonSerializer.Serialize(state));
            LauncherLog.Write($"VR session active (PID={processId}). VR settings and controller bindings deployed.");
        }
    }

    public static void UpdateSessionProcessId(int processId)
    {
        lock (Gate)
        {
            if (!File.Exists(SessionStatePath)) return;
            try
            {
                var state = JsonSerializer.Deserialize<SessionState>(File.ReadAllText(SessionStatePath)) ?? new SessionState();
                state.InVrSession = true;
                state.ProcessId = processId;
                File.WriteAllText(SessionStatePath, JsonSerializer.Serialize(state));
            }
            catch (Exception ex)
            {
                LauncherLog.Write("Error updating session process ID: " + ex.Message);
            }
        }
    }

    public static void ExitVrSession()
    {
        lock (Gate)
        {
            if (!IsInVrSession()) return;
            ExitVrSessionInternal(saveVrEdits: true);
        }
    }

    private static void ExitVrSessionInternal(bool saveVrEdits)
    {
        if (saveVrEdits)
        {
            SaveActiveToVrSettings();
        }

        RestoreDesktopSettings();

        try
        {
            if (File.Exists(SessionStatePath))
            {
                File.Delete(SessionStatePath);
            }
        }
        catch (Exception ex)
        {
            LauncherLog.Write("Could not delete session state file: " + ex.Message);
        }

        LauncherLog.Write("Exited VR session. Desktop settings and controller bindings restored for normal gameplay.");
    }

    private static void BackupDesktopSettings()
    {
        EnsureDirectories();
        if (File.Exists(GameIniPath))
        {
            File.Copy(GameIniPath, Path.Combine(DesktopSettingsFolder, "GameUserSettings.ini"), true);
        }
        if (File.Exists(GameInputSavePath))
        {
            File.Copy(GameInputSavePath, Path.Combine(DesktopSettingsFolder, "EnhancedInputUserSettings.sav"), true);
        }
        if (File.Exists(GameUserSavePath))
        {
            File.Copy(GameUserSavePath, Path.Combine(DesktopSettingsFolder, "userSettings.sav"), true);
        }
        LauncherLog.Write("Desktop settings backed up to desktop_settings.");
    }

    private static void DeployVrSettings()
    {
        EnsureDirectories();
        string vrIni = Path.Combine(VrSettingsFolder, "GameUserSettings.ini");
        string vrInput = Path.Combine(VrSettingsFolder, "EnhancedInputUserSettings.sav");
        string vrUser = Path.Combine(VrSettingsFolder, "userSettings.sav");

        if (File.Exists(vrIni))
        {
            File.Copy(vrIni, GameIniPath, true);
        }
        if (File.Exists(vrInput))
        {
            File.Copy(vrInput, GameInputSavePath, true);
        }
        if (File.Exists(vrUser))
        {
            File.Copy(vrUser, GameUserSavePath, true);
        }
    }

    private static void SaveActiveToVrSettings()
    {
        EnsureDirectories();
        if (File.Exists(GameIniPath))
        {
            File.Copy(GameIniPath, Path.Combine(VrSettingsFolder, "GameUserSettings.ini"), true);
        }
        if (File.Exists(GameInputSavePath))
        {
            File.Copy(GameInputSavePath, Path.Combine(VrSettingsFolder, "EnhancedInputUserSettings.sav"), true);
        }
        if (File.Exists(GameUserSavePath))
        {
            File.Copy(GameUserSavePath, Path.Combine(VrSettingsFolder, "userSettings.sav"), true);
        }
    }

    private static void RestoreDesktopSettings()
    {
        EnsureDirectories();
        string dIni = Path.Combine(DesktopSettingsFolder, "GameUserSettings.ini");
        string dInput = Path.Combine(DesktopSettingsFolder, "EnhancedInputUserSettings.sav");
        string dUser = Path.Combine(DesktopSettingsFolder, "userSettings.sav");

        if (File.Exists(dIni))
        {
            File.Copy(dIni, GameIniPath, true);
        }
        if (File.Exists(dInput))
        {
            File.Copy(dInput, GameInputSavePath, true);
        }
        if (File.Exists(dUser))
        {
            File.Copy(dUser, GameUserSavePath, true);
        }
    }

    public static void ResetDesktopToDefault()
    {
        lock (Gate)
        {
            EnsureDirectories();
            if (Directory.Exists(BundledDefaultSettingsFolder))
            {
                foreach (string file in Directory.EnumerateFiles(BundledDefaultSettingsFolder))
                {
                    string target = Path.Combine(DesktopSettingsFolder, Path.GetFileName(file));
                    File.Copy(file, target, true);
                }
            }
            RestoreDesktopSettings();
            LauncherLog.Write("Desktop settings reset to clean defaults.");
        }
    }
}
