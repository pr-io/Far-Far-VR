# Developer guide

Players do not need any tools below. Download the complete release ZIP to play.

## Launcher and package

Use Windows x64 and the .NET 8 SDK:

```powershell
dotnet publish Launcher\FarFarWestVR.csproj -c Release -r win-x64 -o Launcher\publish
```

The launcher includes its .NET runtime. The release builder also needs the pinned customization delta in the ignored local `Runtime` folder. Obtain the matching [0.4.7 delta](https://github.com/pr-io/Far-Far-VR/releases/download/v0.4.7/backend.ffvrdelta); its hash is checked by the builder and installer.

```powershell
pwsh -File scripts\Build-Package.ps1
```

Output: `dist/FarFarVR-v0.4.7.zip`. It includes the launcher, player profile, delta, updater helper, documentation, images and SHA-256 manifest. Tests, source checkouts, development reports and complete upstream runtime DLLs are excluded.

## Regression checks

Python 3.13 with Lua test support:

```powershell
python -m pip install --target tools/lua-test lupa
python -m unittest discover -s tests -v
```

Launcher/settings/update checks:

```powershell
dotnet run --project tests\SessionLinkTests\SessionLinkTests.csproj
dotnet run --project tests\SettingsIsolationTests\SettingsIsolationTests.csproj
dotnet run --project tests\SocialBindingsTests\SocialBindingsTests.csproj
dotnet run --project tests\UpdaterTests\UpdaterTests.csproj
```

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
