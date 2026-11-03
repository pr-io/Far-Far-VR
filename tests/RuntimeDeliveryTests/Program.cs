using FarFarWestVR;
using System.Security.Cryptography;

if (args.Length != 2) throw new ArgumentException("Supply the pinned upstream archive and Far Far VR patch.");
string archive = Path.GetFullPath(args[0]), patch = Path.GetFullPath(args[1]);
string root = Path.Combine(Path.GetTempPath(), "farfarvr-runtime-proof-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int checks = 0;
void Check(bool result) { if (!result) throw new Exception("Runtime delivery check failed: " + checks); ++checks; }
void ExpectFailure(Action action) { bool failed = false; try { action(); } catch (IOException) { failed = true; } Check(failed); }
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
try
{
    string fresh = Path.Combine(root, "fresh", "UE5.8");
    Check(!Directory.Exists(fresh));
    RuntimePatch.InstallArchive(archive, patch, fresh);
    var setup = new Setup { RuntimeFolder = fresh };
    Check(setup.IsRuntimeInstalled);
    Check(Hash(Path.Combine(fresh, "UEVRBackend.dll")) == Setup.CurrentBackendHash);
    Check(Directory.GetFiles(fresh).Length == 4);
    // An installed exact runtime needs neither another download nor the patch file.
    await Setup.EnsureRuntimeAsync(setup);
    Check(setup.IsRuntimeInstalled);

    // Exercise the actual first-launch network installer, not just the archive helper.
    string packagedRuntime = Path.Combine(AppContext.BaseDirectory, "Runtime");
    Directory.CreateDirectory(packagedRuntime);
    string packagedPatch = Path.Combine(packagedRuntime, RuntimePatch.PatchFile);
    File.Copy(patch, packagedPatch, overwrite: true);
    try
    {
        string online = Path.Combine(root, "online", "UE5.8");
        var onlineSetup = new Setup { RuntimeFolder = online };
        await Setup.EnsureRuntimeAsync(onlineSetup);
        Check(onlineSetup.IsRuntimeInstalled);
        Check(Hash(Path.Combine(online, "UEVRBackend.dll")) == Setup.CurrentBackendHash);
        Check(File.Exists(Path.Combine(online, "BUILD-SOURCE.txt")));
    }
    finally { File.Delete(packagedPatch); }

    string existing = Path.Combine(root, "existing");
    Directory.CreateDirectory(existing);
    string[] names = ["UEVRBackend.dll", "LuaVR.dll", "openxr_loader.dll", "openvr_api.dll"];
    foreach (string name in names) File.WriteAllText(Path.Combine(existing, name), "old " + name);
    File.WriteAllText(Path.Combine(existing, "user-note.txt"), "preserve me");
    // Force failure after the first component has already been promoted.
    using (var locked = new FileStream(Path.Combine(existing, "LuaVR.dll"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        ExpectFailure(() => RuntimePatch.InstallArchive(archive, patch, existing));
    foreach (string name in names) Check(File.ReadAllText(Path.Combine(existing, name)) == "old " + name);
    Check(File.ReadAllText(Path.Combine(existing, "user-note.txt")) == "preserve me");
    Check(!Directory.EnumerateDirectories(root, ".farfarvr-runtime-*").Any());

    string corruptPatch = Path.Combine(root, "corrupt.delta");
    byte[] broken = File.ReadAllBytes(patch); broken[broken.Length / 2] ^= 1;
    File.WriteAllBytes(corruptPatch, broken);
    ExpectFailure(() => RuntimePatch.InstallArchive(archive, corruptPatch, existing));
    foreach (string name in names) Check(File.ReadAllText(Path.Combine(existing, name)) == "old " + name);

    string badArchive = Path.Combine(root, "wrong.zip");
    File.WriteAllText(badArchive, "not the pinned upstream download");
    ExpectFailure(() => RuntimePatch.InstallArchive(badArchive, patch, existing));
    bool cancelled = false;
    try { RuntimePatch.InstallArchive(archive, patch, existing, new CancellationToken(canceled: true)); }
    catch (OperationCanceledException) { cancelled = true; }
    Check(cancelled);
    foreach (string name in names) Check(File.ReadAllText(Path.Combine(existing, name)) == "old " + name);

    RuntimePatch.InstallArchive(archive, patch, existing);
    Check(new Setup { RuntimeFolder = existing }.IsRuntimeInstalled);
    Check(File.ReadAllText(Path.Combine(existing, "user-note.txt")) == "preserve me");
    Check(!Directory.EnumerateDirectories(root, ".farfarvr-runtime-*").Any());
    Console.WriteLine($"{checks} runtime delivery checks passed: exact fresh reconstruction, no-network reuse, integrity refusal, cancellation, partial-promotion rollback and retry.");
}
finally { Directory.Delete(root, recursive: true); }
