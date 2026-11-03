using FarFarWestVR;
using System.Security.Cryptography;
using System.Text.Json;

var releasesJson = """
[
  {"tag_name":"v0.4.3","draft":true,"assets":[{"name":"FarFarVR-v0.4.3.zip","browser_download_url":"https://github.com/pr-io/Far-Far-VR/releases/download/v0.4.3/FarFarVR-v0.4.3.zip"}]},
  {"tag_name":"v0.4.2","draft":false,"assets":[{"name":"FarFarVR-v0.4.2.zip","browser_download_url":"https://github.com/pr-io/Far-Far-VR/releases/download/v0.4.2/FarFarVR-v0.4.2.zip"}],"body":"Fixes and comfort improvements."},
  {"tag_name":"v0.4.1","draft":false,"assets":[{"name":"FarFarVR-v0.4.1.zip","browser_download_url":"https://github.com/pr-io/Far-Far-VR/releases/download/v0.4.1/FarFarVR-v0.4.1.zip"}],"body":"Older update."},
  {"tag_name":"v0.9.0","draft":false,"assets":[],"body":"No package asset."}
]
""";
using JsonDocument releases = JsonDocument.Parse(releasesJson);
Check(UpdateService.CurrentVersion == "0.4.2");
ReleaseUpdate? update = UpdateService.FindUpdate(releases.RootElement, "0.4.1");
Check(update is { Tag: "v0.4.2", Version: "v0.4.2", Notes: "Fixes and comfort improvements." });
Check(update!.AssetName == "FarFarVR-v0.4.2.zip" && update.DownloadUrl.Scheme == Uri.UriSchemeHttps);
Check(UpdateService.FindUpdate(releases.RootElement, "0.4.2") is null);
using JsonDocument emptyReleases = JsonDocument.Parse("[]");
Check(UpdateService.FindUpdate(emptyReleases.RootElement, "0.4.0") is null);
Check(!UpdateService.TryParseVersion("v0.4.invalid", out _));
Check(UpdateService.TryParseVersion("v0.4.1-playtest", out var playtest) && playtest == new Version(0, 4, 1));

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
    Check(status == "state=available\nversion=v0.4.2\nnotes:\nNotes\nwith details");
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
        ["Apply-Update.ps1"] = [10, 11, 12]
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
