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
