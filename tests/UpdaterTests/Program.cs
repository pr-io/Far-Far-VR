using FarFarWestVR;
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
ReleaseUpdate? update = UpdateService.FindUpdate(releases.RootElement, "0.4.0");
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
    Check(Setup.CompatibleBackendHashes.Contains(Setup.BaseBackendHash));
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
