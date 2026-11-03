# Build and release

## Build the launcher

On Windows x64 with the .NET 8 SDK:

```powershell
dotnet publish Launcher\FarFarWestVR.csproj -c Release -r win-x64 -o Launcher\publish
```

The published launcher is a self-contained Windows x64 executable and includes the .NET runtime, so players do not need to install .NET separately. Copy `Launcher\publish\FarFarWestVR.exe` to `Far Far VR.exe` in the repository root or package folder.

Run the unit test suites:

```powershell
# C# launcher, settings isolation, and social bindings tests
dotnet run --project tests\SessionLinkTests\SessionLinkTests.csproj
dotnet run --project tests\SettingsIsolationTests\SettingsIsolationTests.csproj
dotnet run --project tests\SocialBindingsTests\SocialBindingsTests.csproj
dotnet run --project tests\UpdaterTests\UpdaterTests.csproj

# Python / Lua behavior tests
python -m unittest discover -s tests -v
```

## Reproduce the compatible engine backend

The native UEVR engine is not vendored directly in Git. The automation scripts clone the pinned UEVR revision into `vendor\UEVR`, verify the revision, and apply `Engine\cinema-comfort.patch` and `Engine\farfar-console.patch`.

Prerequisites:
- Visual Studio 2022 C++ Build Tools (`C:\BuildTools\VS2022`)
- Windows SDK, CMake
- Epic-linked GitHub account for UESDK source access

```powershell
pwsh -File scripts\Prepare-Engine.ps1
pwsh -File scripts\Build-Engine.ps1
pwsh -File scripts\Test-Engine.ps1
```

The current local-test backend SHA-256 is `2F30D9353B02435F0D34A6505D2FF494F4DACD20783F8D72CB408F9AEBE41558`. Native engine builds are for local testing; the player archive excludes third-party runtime DLLs.

## Build a player package

The package builder builds the launcher, copies the player profile and documentation, and stages the runtime-separated player archive in `dist\FarFarVR`. It rejects bundled UEVR runtime DLLs:

```powershell
pwsh -File scripts\Build-Package.ps1
```

The resulting package contains:
- `Far Far VR.exe`
- `UE5.8/README-RUNTIME.txt` (upstream runtime acquisition instructions; no runtime DLLs)
- `Profile/` (config.txt, cameras.txt, VRSettings.json, Touch bindings, Lua scripts)
- Player documentation (`README.md`, `ROADMAP.md`, `THIRD_PARTY.md`)
- `manifest.json` (SHA-256 checksums of all package files)


## Local runtime-delivery candidate (0.4.3)

The publisher-only command `python scripts/Build-RuntimePatch.py --base <pinned-upstream-UEVRBackend.dll> --target <tested-custom-UEVRBackend.dll>` creates the local `Runtime/backend.ffvrdelta` artifact. The player installer uses Windows MSDelta; Python and build tools are not player dependencies. The builder pins the delta hash and the installer pins the final tested backend hash. Run `dotnet run --project tests/RuntimeDeliveryTests/RuntimeDeliveryTests.csproj -- <pinned-upstream-zip> Runtime/backend.ffvrdelta` to exercise reconstruction, the real online installer, integrity refusals, cancellation and rollback.

This produces a technically functioning local package, not a verified grant to distribute the engine delta. See [runtime delivery status](Engine/RUNTIME-DELIVERY.md). Do not publish the local candidate as rights-cleared.
