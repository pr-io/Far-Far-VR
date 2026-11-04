# Developer guide

Players do not need any tools below. Download the complete release ZIP to play.

## Launcher and package

Use Windows x64 and the .NET 8 SDK:

```powershell
dotnet publish Launcher\FarFarWestVR.csproj -c Release -r win-x64 -o Launcher\publish
```

The launcher includes its .NET runtime. The release builder also needs the pinned customization delta in the ignored local `Runtime` folder. Obtain the matching [0.4.15 delta](https://github.com/pr-io/Far-Far-VR/releases/download/v0.4.15/backend.ffvrdelta); its hash is checked by the builder and installer.

Publisher privacy preparation sanitizes the pinned native candidate before generating its delta. Preserve the original candidate and use a separate output file:

```powershell
python scripts\Sanitize-RuntimePaths.py --input <pinned-native-candidate> --output <sanitized-backend>
python scripts\Build-RuntimePatch.py --base <pinned-upstream-backend> --target <sanitized-backend> --output Runtime\backend.ffvrdelta
```

The sanitizer accepts only its explicitly pinned input, preserves all executable sections and file size, and replaces profile roots only in read-only diagnostic data. A different native build requires deliberate validation and pin updates. Never publish unsanitized local build artifacts.

```powershell
pwsh -File scripts\Build-Package.ps1
```

Output: `dist/FarFarVR-v0.4.15.zip`. It includes the launcher, player profile, delta, updater helper, documentation, images and SHA-256 manifest. Tests, source checkouts, development reports and complete upstream runtime DLLs are excluded.

## Regression checks

Python 3.13 with Lua test support:

```powershell
python -m pip install --target tools/lua-test -r tests/requirements.txt
dotnet build tests\UpdaterTests\UpdaterTests.csproj
python -m unittest discover -s tests -v
```

Launcher/settings/update checks:

```powershell
dotnet run --project tests\SessionLinkTests\SessionLinkTests.csproj
dotnet run --project tests\SettingsIsolationTests\SettingsIsolationTests.csproj
dotnet run --project tests\SocialBindingsTests\SocialBindingsTests.csproj
dotnet run --project tests\UpdaterTests\UpdaterTests.csproj
```

Shutdown-monitor checks use a separate disposable process fixture. Build it first, then pass its executable to the checks:

```powershell
dotnet build tests\ExitMonitorFixture\ExitMonitorFixture.csproj
dotnet run --project tests\ExitMonitorTests\ExitMonitorTests.csproj -- tests\ExitMonitorFixture\bin\Debug\net8.0\FarFarWest-Win64-Shipping.exe
```

The fixture checks that active sessions are preserved, a mismatched process identity is refused, and a completed shutdown is cleaned up. It does not launch Far Far West or establish headset shutdown behavior.

## Automated GitHub checks

GitHub Actions runs the Windows launcher build, launcher/settings/updater checks, shutdown-monitor fixture, and Python/Lua regression suite on pushes and pull requests to `main`. You can also run the workflow from the repository's **Actions > Regression checks > Run workflow** page.

These checks do not require the proprietary game, a headset, or private UESDK access. Native engine builds and real upstream runtime-delivery checks remain separate local checks described below. Automated checks do not establish gameplay, headset visual quality, controller feel, or performance.

Runtime installation, real upstream download and rollback checks require the pinned upstream archive and customization delta:

```powershell
dotnet run --project tests\RuntimeDeliveryTests\RuntimeDeliveryTests.csproj -- <upstream-zip> Runtime\backend.ffvrdelta
```

Native product policy checks are provided by `scripts/Test-FarFar*.ps1`. Software checks do not establish headset visual quality, physical control behavior or game performance; test those in representative gameplay before changing an accepted runtime.

## Engine source

Rebuilding the engine is only needed when changing its native customizations. Requirements: Git, Visual Studio 2022 C++ Build Tools, Windows SDK, CMake, and an Epic-linked GitHub account with UESDK access.

```powershell
git clone --filter=blob:none https://github.com/joeyhodge/UEVR.git vendor/UEVR
git -C vendor/UEVR checkout 963b157573eaef20528b602d1d9790b8105819b3
git -C vendor/UEVR submodule update --init --recursive
pwsh -File scripts\Prepare-Engine.ps1
pwsh -File scripts\Build-Engine.ps1
pwsh -File scripts\Test-Engine.ps1
```

Pinned revisions and runtime hashes are in [Engine/README.md](Engine/README.md). The publisher-only `scripts/Build-RuntimePatch.py` accepts the pinned base engine and tested custom target to generate the Windows delta. A new native build requires its own validation and deliberate hash updates before use; do not silently replace the accepted binary.

See [THIRD_PARTY.md](THIRD_PARTY.md) for upstream attribution and licensing terms.
