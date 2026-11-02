using FarFarWestVR;

// Verify SettingsIsolation file and folder paths
Check(!string.IsNullOrWhiteSpace(SettingsIsolation.GameSavedFolder));
Check(!string.IsNullOrWhiteSpace(SettingsIsolation.VrSettingsFolder));
Check(!string.IsNullOrWhiteSpace(SettingsIsolation.DesktopSettingsFolder));
Check(!string.IsNullOrWhiteSpace(SettingsIsolation.SessionStatePath));

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

// Check EnsureDesktopState safely handles missing session file
SettingsIsolation.EnsureDesktopState();

Console.WriteLine("Settings isolation paths, simulation, and desktop preservation checks passed.");

void Check(bool condition)
{
    if (!condition) throw new Exception("Settings isolation test failed.");
}
