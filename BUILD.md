# Build and release

## Build the launcher

On Windows x64 with the .NET 8 SDK:

```powershell
dotnet publish Launcher\FarFarWestVR.csproj -c Release -r win-x64 -o Launcher\publish
```

The published launcher includes self-contained dependencies. Copy `Launcher\publish\FarFarWestVR.exe` to `Far Far VR.exe` in the repository root or package folder.

Run the unit test suites:

```powershell
# C# launcher & social bindings tests
dotnet run --project tests\SessionLinkTests\SessionLinkTests.csproj
dotnet run --project tests\SocialBindingsTests\SocialBindingsTests.csproj

# Python / Lua behavior tests (96 tests)
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

The verified backend SHA-256 is `2F30D9353B02435F0D34A6505D2FF494F4DACD20783F8D72CB408F9AEBE41558`.

## Build a player package

The package builder verifies the accepted backend hash, builds the launcher, copies the active profile and documentation, and stages a complete release package in `dist\FarFarVR`:

```powershell
pwsh -File scripts\Build-Package.ps1
```

The resulting package contains:
- `Far Far VR.exe`
- `UE5.8/` (UEVRBackend.dll, LuaVR.dll, openxr_loader.dll, openvr_api.dll, BUILD-SOURCE.txt)
- `Profile/` (config.txt, cameras.txt, VRSettings.json, Touch bindings, Lua scripts)
- `Engine/` (patches, C++ headers, and validation notes)
- Player documentation (`README.md`, `ROADMAP.md`, `THIRD_PARTY.md`)
- `manifest.json` (SHA-256 checksums of all package files)
