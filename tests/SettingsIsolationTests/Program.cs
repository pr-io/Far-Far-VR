using FarFarWestVR;

// Verify SettingsIsolation file and folder paths
Check(!string.IsNullOrWhiteSpace(SettingsIsolation.GameSavedFolder));
Check(!string.IsNullOrWhiteSpace(SettingsIsolation.VrSettingsFolder));
Check(!string.IsNullOrWhiteSpace(SettingsIsolation.DesktopSettingsFolder));
Check(!string.IsNullOrWhiteSpace(SettingsIsolation.SessionStatePath));
string repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
Check(SettingsIsolation.IsValidVrInputSave(Path.Combine(repositoryRoot, "Profile", "vr_settings", "EnhancedInputUserSettings.sav")));
Check(!SettingsIsolation.IsValidVrInputSave(Path.Combine(repositoryRoot, "Profile", "default_pc_settings", "EnhancedInputUserSettings.sav")));

// Test isolation logic with test temporary directories
string testRoot = Path.Combine(Path.GetTempPath(), "farfarvr-settings-test-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(testRoot);
    string dFolder = Path.Combine(testRoot, "desktop_settings");
    string vFolder = Path.Combine(testRoot, "vr_settings");
    string activeFolder = Path.Combine(testRoot, "active");
    Directory.CreateDirectory(dFolder);
    Directory.CreateDirectory(vFolder);
    Directory.CreateDirectory(activeFolder);

    string dFile = Path.Combine(dFolder, "GameUserSettings.ini");
    string vFile = Path.Combine(vFolder, "GameUserSettings.ini");
    string aFile = Path.Combine(activeFolder, "GameUserSettings.ini");

    File.WriteAllText(dFile, "Quality=High");
    File.WriteAllText(vFile, "Quality=LowVR");
    File.WriteAllText(aFile, "Quality=High");

    // Simulating Enter VR: backup active -> desktop, copy vr -> active
    File.Copy(aFile, dFile, true);
    File.Copy(vFile, aFile, true);
    Check(File.ReadAllText(aFile) == "Quality=LowVR");

    // Simulating VR edit during session
    File.WriteAllText(aFile, "Quality=LowVR_Tuned");

    // Simulating Exit VR: save active -> vr, restore desktop -> active
    File.Copy(aFile, vFile, true);
    File.Copy(dFile, aFile, true);
    Check(File.ReadAllText(aFile) == "Quality=High");
    Check(File.ReadAllText(vFile) == "Quality=LowVR_Tuned");
}
finally
{
    if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true);
}

// Session cleanup may delete only an unchanged loader deployed by this launcher.
string loaderTest = Path.Combine(Path.GetTempPath(), "farfarvr-loader-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(loaderTest);
    string source = Path.Combine(loaderTest, "source.dll"), target = Path.Combine(loaderTest, "openxr_loader.dll");
    File.WriteAllBytes(source, [1,2,3]);
    File.WriteAllBytes(target, [9,8,7]);
    SettingsIsolation.DeploySessionOpenXrLoader(source, loaderTest);
    SettingsIsolation.CleanupSessionOpenXrLoader(loaderTest);
    Check(File.ReadAllBytes(target).SequenceEqual(new byte[]{9,8,7}));
    File.Delete(target);
    SettingsIsolation.DeploySessionOpenXrLoader(source, loaderTest);
    SettingsIsolation.CleanupSessionOpenXrLoader(loaderTest);
    Check(!File.Exists(target));
    SettingsIsolation.DeploySessionOpenXrLoader(source, loaderTest);
    File.WriteAllBytes(target, [4,5,6]);
    SettingsIsolation.CleanupSessionOpenXrLoader(loaderTest);
    Check(File.ReadAllBytes(target).SequenceEqual(new byte[]{4,5,6}));
}
finally { if (Directory.Exists(loaderTest)) Directory.Delete(loaderTest, true); }

Console.WriteLine("Settings isolation paths, simulation, and desktop preservation checks passed.");

void Check(bool condition)
{
    if (!condition) throw new Exception("Settings isolation test failed.");
}
// Exercise the production settings lifecycle against isolated folders.
string lifecycleRoot = Path.Combine(Path.GetTempPath(), "farfarvr-lifecycle-" + Guid.NewGuid().ToString("N"));
try
{
    SettingsIsolation.StorageRoot = lifecycleRoot;
    SettingsIsolation.GameRunning = () => false;
    SettingsIsolation.EnsureDirectories();
    Directory.CreateDirectory(SettingsIsolation.BundledVrSettingsFolder);
    File.WriteAllText(Path.Combine(SettingsIsolation.BundledVrSettingsFolder,"GameUserSettings.ini"), "[ScalabilityGroups]\nsg.ShadowQuality=0\n[/Script/Engine.GameUserSettings]\nbUseVSync=False\n");
    string desktop = "[ScalabilityGroups]\nsg.ShadowQuality=3\n[/Script/Engine.GameUserSettings]\nbUseVSync=True\nResolutionSizeX=2560\n";
    File.WriteAllText(SettingsIsolation.GameIniPath, desktop);
    SettingsIsolation.EnterVrSession();
    Check(SettingsIsolation.IsInVrSession());
    Check(File.ReadAllText(SettingsIsolation.GameIniPath).Contains("sg.ShadowQuality=0"));
    Check(File.ReadAllText(SettingsIsolation.GameIniPath).Contains("ResolutionSizeX=2560"));
    SettingsIsolation.ExitVrSession();
    Check(File.ReadAllText(SettingsIsolation.GameIniPath)==desktop && !File.Exists(SettingsIsolation.SessionStatePath));

    // Running game must be rejected before any backup or deployment.
    SettingsIsolation.GameRunning=()=>true;
    bool refused=false;
    try { SettingsIsolation.EnterVrSession(); } catch(IOException) { refused=true; }
    Check(refused && File.ReadAllText(SettingsIsolation.GameIniPath)==desktop && !File.Exists(SettingsIsolation.SessionStatePath));
    SettingsIsolation.GameRunning=()=>false;

    // Startup failure after graphics deployment restores the prior files.
    File.WriteAllBytes(Path.Combine(SettingsIsolation.VrSettingsFolder,"EnhancedInputUserSettings.sav"),[0,1,2]);
    File.WriteAllBytes(SettingsIsolation.GameInputSavePath,[8,9]);
    bool failed=false;
    try { SettingsIsolation.EnterVrSession(); } catch(IOException) { failed=true; }
    Check(failed && File.ReadAllText(SettingsIsolation.GameIniPath)==desktop);
    Check(File.ReadAllBytes(SettingsIsolation.GameInputSavePath).SequenceEqual(new byte[]{8,9}));
    Check(!SettingsIsolation.IsInVrSession());
    File.Delete(Path.Combine(SettingsIsolation.VrSettingsFolder,"EnhancedInputUserSettings.sav"));
    File.Delete(SettingsIsolation.GameInputSavePath);

    // Simulate process interruption after deployment, then execute startup recovery.
    SettingsIsolation.EnterVrSession();
    SettingsIsolation.EnsureDesktopState();
    Check(File.ReadAllText(SettingsIsolation.GameIniPath)==desktop && !SettingsIsolation.IsInVrSession());

    // Nonexistence is part of the desktop snapshot and is restored exactly.
    File.Delete(SettingsIsolation.GameIniPath);
    SettingsIsolation.EnterVrSession();
    Check(File.Exists(SettingsIsolation.GameIniPath));
    SettingsIsolation.ExitVrSession();
    Check(!File.Exists(SettingsIsolation.GameIniPath));
}
finally
{
    SettingsIsolation.StorageRoot=null;
    SettingsIsolation.GameRunning=()=>System.Diagnostics.Process.GetProcessesByName("FarFarWest-Win64-Shipping").Length>0;
    if(Directory.Exists(lifecycleRoot))Directory.Delete(lifecycleRoot,true);
}
Console.WriteLine("Production lifecycle: start, exit, running-game guard, partial failure, interruption and absent-file recovery passed.");
