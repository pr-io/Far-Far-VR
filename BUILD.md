# Build and release

## Build the launcher

On Windows x64 with the .NET 8 SDK:

```powershell
dotnet publish Launcher\FarFarWestVR.csproj -c Release -r win-x64 --self-contained true -o Launcher\publish
```

The published single-file launcher includes the .NET runtime. Players do not need the .NET SDK, a separate .NET runtime install, source compilation, or UEVR installed separately.

Run the dependency-free controller mapping checks with:

```powershell
dotnet run --project tests\ControllerBindingsSmoke.csproj
```

The Lua behavior tests run on Python 3.13 or later with `lupa` installed:

```powershell
python -m pip install lupa
python -m unittest discover -s tests -v
```

## Reproduce the compatible backend

The native project is not vendored. The scripts clone the pinned UEVR revision into the ignored `vendor\UEVR` directory, verify the revision, and apply `Engine\cinema-comfort.patch`. UESDK is pinned at `811b01498b61e6a0246615b4f2b4fb4f4067fb64` and may require the builder's Epic-linked GitHub access. A compatible MSVC x64 toolchain, Windows SDK, CMake, and the pinned Tracy dependency are required for native builds; they are development prerequisites only.

```powershell
pwsh -File scripts\Prepare-Engine.ps1
pwsh -File scripts\Build-Engine.ps1
pwsh -File scripts\Test-Engine.ps1
```

The engine build writes into ignored build directories and never replaces an accepted runtime automatically. Compare the resulting backend with the recorded accepted SHA-256 in `THIRD_PARTY.md`; a different hash requires review and the full relevant test set before use. Source build success alone does not establish a playable release.

## Build a player package

The package builder requires a complete runtime directory containing `UEVRBackend.dll`, `LuaVR.dll`, `openxr_loader.dll`, `openvr_api.dll`, and `BUILD-SOURCE.txt`. It verifies the accepted backend hash, builds a self-contained launcher, includes the profile and player documentation, and creates a zip under `dist`. The runtime directory must be supplied by the release operator and must not be a generic UEVR download.

The builder refuses runtime packaging by default. Only after written redistribution permission covers the exact runtime and dependencies should an authorized operator run:

```powershell
pwsh -File scripts\Build-Package.ps1 -RuntimeFolder 'C:\path\to\rights-cleared\UE5.8' -AllowRuntimeRedistribution
```

The current recorded UEVR license does not grant that permission, and the exact `240C5A…5EC73` backend is not in the pinned public release. Therefore a usable installable package/release cannot presently be built from only the redistributable source in this repository. Do not weaken the hash check, substitute stock files, or publish a source-only zip as a player installer.

After rights are resolved, keep the GitHub repository private, inspect the package contents for game files, logs, local configuration and build intermediates, test it from a clean extracted directory with empty launcher/profile settings, then upload the verified zip as a private GitHub release asset. Record the uploaded commit and package SHA-256. Do not claim headset acceptance without a real headset test.
