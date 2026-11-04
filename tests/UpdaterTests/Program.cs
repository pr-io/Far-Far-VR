using FarFarWestVR;
using System.Security.Cryptography;
using System.Text.Json;

if(args.Length==2 && args[0]=="--bridge-profile")
{
    string profile=Path.GetFullPath(args[1]);
    if(File.ReadAllText(UpdateService.CommandPath(profile)).Trim()!="check") throw new Exception("Lua update command did not reach the launcher.");
    new UpdateService(profile).WriteStatus("available","v0.4.8","Bridge integration verified.");
    Console.WriteLine("Launcher received the Lua command and wrote its response.");
    return;
}

var releasesJson = """
[
  {"tag_name":"v0.4.3","draft":true,"assets":[{"name":"FarFarVR-v0.4.3.zip","browser_download_url":"https://github.com/pr-io/Far-Far-VR/releases/download/v0.4.3/FarFarVR-v0.4.3.zip"}]},
  {"tag_name":"v0.4.2","draft":false,"assets":[{"name":"FarFarVR-v0.4.2.zip","browser_download_url":"https://github.com/pr-io/Far-Far-VR/releases/download/v0.4.2/FarFarVR-v0.4.2.zip"}],"body":"Fixes and comfort improvements."},
  {"tag_name":"v0.4.1","draft":false,"assets":[{"name":"FarFarVR-v0.4.1.zip","browser_download_url":"https://github.com/pr-io/Far-Far-VR/releases/download/v0.4.1/FarFarVR-v0.4.1.zip"}],"body":"Older update."},
  {"tag_name":"v0.9.0","draft":false,"assets":[],"body":"No package asset."}
]
""";
using JsonDocument releases = JsonDocument.Parse(releasesJson);
Check(UpdateService.CurrentVersion == "0.4.8");
ReleaseUpdate? update = UpdateService.FindUpdate(releases.RootElement, "0.4.1");
Check(update is { Tag: "v0.4.2", Version: "v0.4.2", Notes: "Fixes and comfort improvements." });
Check(update!.AssetName == "FarFarVR-v0.4.2.zip" && update.DownloadUrl.Scheme == Uri.UriSchemeHttps);
Check(UpdateService.FindUpdate(releases.RootElement, "0.4.2") is null);
using JsonDocument emptyReleases = JsonDocument.Parse("[]");
Check(UpdateService.FindUpdate(emptyReleases.RootElement, "0.4.0") is null);
Check(!UpdateService.TryParseVersion("v0.4.invalid", out _));
Check(!UpdateService.TryParseVersion("v0.4.1-playtest", out _));

using JsonDocument prereleases=JsonDocument.Parse("""
[{"tag_name":"v9.0.0","prerelease":true,"assets":[{"name":"FarFarVR-v9.0.0.zip","browser_download_url":"https://github.com/pr-io/Far-Far-VR/releases/download/v9.0.0/FarFarVR-v9.0.0.zip"}]},
 {"tag_name":"v9.1.0-playtest","prerelease":false,"assets":[{"name":"FarFarVR-v9.1.0-playtest.zip","browser_download_url":"https://github.com/pr-io/Far-Far-VR/releases/download/v9.1.0-playtest/FarFarVR-v9.1.0-playtest.zip"}]}]
""");
Check(UpdateService.FindUpdate(prereleases.RootElement,"0.4.7") is null);
Check(UpdateService.StatusPath("profile")==Path.Combine("profile","data","farfarvr-updates.txt"));
Check(UpdateService.CommandPath("profile")==Path.Combine("profile","data","farfarvr-update-command.txt"));

var recoveryRoot=Path.Combine(Path.GetTempPath(),"ffvr-recovery-bootstrap-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(recoveryRoot);
try
{
    string journal=Path.Combine(recoveryRoot,".farfarvr-update-transaction.json");
    File.WriteAllText(journal,JsonSerializer.Serialize(new {BackupFolder=".farfarvr-update-rollback-fixture"}));
    Check(UpdateService.RecoveryScript(recoveryRoot)==Path.Combine(recoveryRoot,".farfarvr-update-rollback-fixture","recover.ps1"));
    File.WriteAllText(journal,JsonSerializer.Serialize(new {BackupFolder="../outside"}));
    bool invalid=false;
    try { UpdateService.RecoveryScript(recoveryRoot); } catch(IOException) { invalid=true; }
    Check(invalid);
}
finally { Directory.Delete(recoveryRoot,true); }

var statusRoot = Path.Combine(Path.GetTempPath(), "farfarvr-updater-test-" + Guid.NewGuid().ToString("N"));
try
{
    var publicHandler = new CapturingHandler();
    var service = new UpdateService(statusRoot, new HttpClient(publicHandler), () => null);
    Check(await service.CheckAsync() is null);
    Check(publicHandler.Authorization is null);
    Check(publicHandler.RequestUri == $"https://api.github.com/repos/{UpdateService.Repository}/releases?per_page=30");

    var privateHandler = new CapturingHandler();
    var privateService = new UpdateService(statusRoot, new HttpClient(privateHandler), () => "test-token");
    Check(await privateService.CheckAsync() is null);
    Check(privateHandler.Authorization == "Bearer test-token");

    service.WriteStatus("available", "v0.4.2", "Notes\nwith details");
    string status = File.ReadAllText(UpdateService.StatusPath(statusRoot));
    Check(System.Text.RegularExpressions.Regex.Replace(status,"responseId=[^\n]*\n", "") == "state=available\nversion=v0.4.2\nnotes:\nNotes\nwith details");
}
finally
{
    if (Directory.Exists(statusRoot)) Directory.Delete(statusRoot, recursive: true);
}

var tempRuntime = Path.Combine(Path.GetTempPath(), "farfarvr-runtime-test-" + Guid.NewGuid().ToString("N"));
try
{
    var testSetup = new Setup { RuntimeFolder = tempRuntime };
    Check(!testSetup.IsRuntimeInstalled);
    Check(Setup.CompatibleBackendHashes.Contains(Setup.CurrentBackendHash));
    Check(!Setup.CompatibleBackendHashes.Contains(Setup.BaseBackendHash));
    Check(!string.IsNullOrWhiteSpace(Setup.UevrRuntimeDownloadUrl));
    Check(!string.IsNullOrWhiteSpace(Setup.UevrRuntimeArchiveHash));
    Check(!string.IsNullOrWhiteSpace(Setup.CompatibleLuaVrHash));
    Check(!string.IsNullOrWhiteSpace(Setup.CompatibleOpenVrApiHash));
    Check(!string.IsNullOrWhiteSpace(Setup.CompatibleOpenXrLoaderHash));
}
finally
{
    if (Directory.Exists(tempRuntime)) Directory.Delete(tempRuntime, true);
}

Console.WriteLine("Runtime separation, setup hash verification, and updater checks passed.");
if (args.Length > 0) UpdateService.ValidatePackage(args[0]);

var packageRoot = Path.Combine(Path.GetTempPath(), "farfarvr-package-test-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(Path.Combine(packageRoot, "UE5.8"));
    Directory.CreateDirectory(Path.Combine(packageRoot, "Profile"));
    var packageFiles = new Dictionary<string, byte[]>
    {
        ["Far Far VR.exe"] = [1, 2, 3],
        [@"UE5.8\README-RUNTIME.txt"] = [4, 5, 6],
        [@"Profile\config.txt"] = [7, 8, 9],
        ["Apply-Update.ps1"] = [10, 11, 12],
        [@"Runtime\backend.ffvrdelta"] = [13, 14, 15]
    };
    foreach (var (relative, content) in packageFiles)
    {
        string path = Path.Combine(packageRoot, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
    }
    var manifest = new
    {
        product = "Far Far VR",
        version = "0.4.1",
        files = packageFiles.Select(entry => new
        {
            File = entry.Key,
            SHA256 = Convert.ToHexString(SHA256.HashData(entry.Value))
        }).ToArray()
    };
    File.WriteAllText(Path.Combine(packageRoot, "manifest.json"), JsonSerializer.Serialize(manifest));
    UpdateService.ValidatePackage(packageRoot);

    string forbiddenRuntime = Path.Combine(packageRoot, @"UE5.8\UEVRBackend.dll");
    File.WriteAllBytes(forbiddenRuntime, [1,2,3]);
    bool runtimeRejected = false;
    try { UpdateService.ValidatePackage(packageRoot); } catch (IOException) { runtimeRejected = true; }
    Check(runtimeRejected);
    File.Delete(forbiddenRuntime);

    string uncovered = Path.Combine(packageRoot, "unexpected.ps1");
    File.WriteAllText(uncovered, "unexpected");
    bool uncoveredRejected = false;
    try { UpdateService.ValidatePackage(packageRoot); } catch (IOException) { uncoveredRejected = true; }
    Check(uncoveredRejected);
    File.Delete(uncovered);

    File.WriteAllBytes(Path.Combine(packageRoot, "Profile", "config.txt"), [0]);
    bool rejected = false;
    try { UpdateService.ValidatePackage(packageRoot); } catch (IOException) { rejected = true; }
    Check(rejected);
}
finally
{
    if (Directory.Exists(packageRoot)) Directory.Delete(packageRoot, recursive: true);
}

var attachmentRoot = Path.Combine(Path.GetTempPath(), "farfarvr-attachment-test-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(Path.Combine(attachmentRoot, "uobjecthook"));
    string oldFile = Path.Combine(attachmentRoot, "uobjecthook", "10047652364120066497_props.json");
    File.WriteAllText(oldFile, """
{
    "hide": false,
    "hide_legacy": false,
    "path": [
        "Acknowledged Pawn",
        "Properties",
        "Socket_Item",
        "Properties",
        "RelativeScale3D"
    ],
    "properties": [
        {
            "data": 4603039107336110080,
            "name": "Z"
        },
        {
            "data": 4603039107336110080,
            "name": "Y"
        },
        {
            "data": 4603039107336110080,
            "name": "X"
        }
    ],
    "type": "properties"
}
""");
    Check(Setup.FindRetiredGunAttachments(attachmentRoot).Single() == oldFile);
    File.AppendAllText(oldFile, " ");
    Check(!Setup.FindRetiredGunAttachments(attachmentRoot).Any());
    File.WriteAllText(Path.Combine(attachmentRoot, "uobjecthook", "user.json"), "{}");
    Check(!Setup.FindRetiredGunAttachments(attachmentRoot).Any());
}
finally { if (Directory.Exists(attachmentRoot)) Directory.Delete(attachmentRoot, recursive: true); }

var aimRoot = Path.Combine(Path.GetTempPath(), "farfarvr-aim-migration-" + Guid.NewGuid().ToString("N"));
try
{
    Directory.CreateDirectory(aimRoot);
    string config = "VR_WorldScale=1.25\r\nVR_CameraUpOffset=7\r\nOpenXR_ResolutionScale=0.8\r\nUObjectHook_EnabledAtStartup=false\r\nVR_AimModifyPlayerControlRotation=false\r\nVR_AimUsePawnControlRotation=false\r\nVR_AimMethod=2\r\nUI_Size=2.5\r\n";
    string path = Path.Combine(aimRoot, "config.txt");
    File.WriteAllText(path, config);
    Check(Setup.MigrateTrackedAimProfile(aimRoot));
    string migrated = File.ReadAllText(path);
    Check(migrated == config.Replace("=false", "=true"));
    Check(Directory.GetFiles(aimRoot, "config.txt.before-tracked-aim-*").Length == 1);
    Check(File.ReadAllText(Directory.GetFiles(aimRoot, "config.txt.before-tracked-aim-*")[0]) == config);
    Check(!Setup.MigrateTrackedAimProfile(aimRoot));
    // An intentional later edit is not continually overwritten on future starts.
    File.WriteAllText(path, config);
    Check(!Setup.MigrateTrackedAimProfile(aimRoot) && File.ReadAllText(path) == config);
    string missing = Setup.UpgradeTrackedAimDefaults("VR_CameraUpOffset=7");
    Check(missing.Contains("UObjectHook_EnabledAtStartup=true") && missing.Contains("VR_AimModifyPlayerControlRotation=true") && missing.Contains("VR_AimUsePawnControlRotation=true"));
    Check(Setup.UpgradeTrackedAimDefaults(missing) == missing);
    string shipped = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ProfileDefaults.txt"));
    Check(Setup.UpgradeTrackedAimDefaults(shipped) == shipped);
}
finally { Directory.Delete(aimRoot, recursive: true); }

void Check(bool condition)
{
    if (!condition) throw new Exception("Updater smoke test failed.");
}

sealed class CapturingHandler : HttpMessageHandler
{
    public string? Authorization { get; private set; }
    public string? RequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Authorization = request.Headers.Authorization?.ToString();
        RequestUri = request.RequestUri?.ToString();
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        });
    }
}
