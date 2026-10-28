# Far Far VR

Far Far VR is an unofficial VR mod for **Far Far West**. This repository contains the mod launcher, player profile, game-specific Lua modules, OpenXR Touch bindings, source patches and build/package scripts. It does not contain the game or a copy of the UEVR source tree.

> **Standalone release status:** no installable release is published yet. The required Far Far VR backend is the custom build with SHA-256 `240C5ACCB5A1E02862101A5809B10C22D9403A1B5ED1A3095DDF4CB29BA5EC73`. The corresponding public UEVR release does not provide that build, and the recorded UEVR license does not grant redistribution rights. The launcher rejects stock or mismatched backends; do not replace the required build with a generic UEVR download. See [third-party notices](THIRD_PARTY.md).

## Intended player installation

The installable release will be a self-contained Windows x64 package. A player with Far Far West installed and a working PCVR headset will not need to install or configure UEVR separately.

1. Download and extract the Far Far VR release package.
2. Run **Far Far VR.exe**. In **Settings**, confirm or browse to the Steam game folder.
3. Start the headset's PCVR software and connect the headset. The setup page checks that Windows has a valid active OpenXR runtime; it cannot confirm physical headset connectivity.
4. Choose **Start in VR**. Far Far VR installs its profile and Lua modules without replacing an existing profile, starts the game through Steam, waits for the title screen, loads the verified backend, and confirms the engine hooks.
5. In the headset, open the Far Far VR settings panel with both thumbsticks or **Insert**. Use **Calibration** to align height and position; adjust comfort and HUD controls as needed.
6. On the **Input** page, optionally apply the corrected game control bindings. The original game save is backed up before changes.

The launcher and setup flow do not require administrator rights where Windows permits normal same-user process access. If Windows denies injection, close the game and headset software, then start Steam, the headset software and Far Far VR normally under the same user account.

## What the mod preserves

- HMD-based positioning for points of interest, world markers, HUD and crosshair behavior.
- Separation between tracked-hand aiming and game-camera rotation.
- The corrected OpenXR Touch action map and optional game-level controller overrides.
- The Far Far West native stereo/cinematic corrections, including the recovery behavior used by the accepted backend.
- Height, position, comfort and camera calibration controls, with separate recenter and camera-offset reset behavior.
- Game-specific menu, HUD, objective, comfort, mount and render-fix Lua scripts.
- The existing launch sequence: Steam startup, title-screen readiness, backend injection, and explicit engine/stereo hook confirmation.

See [Profile](Profile/), [Engine](Engine/README.md), [build and release instructions](BUILD.md), and [backup/restore](RECOVERY.md) for details and validation limits.

## Development

Build the standalone launcher with the .NET 8 SDK on Windows x64:

```powershell
dotnet publish Launcher\FarFarWestVR.csproj -c Release -r win-x64 --self-contained true
```

For native backend source/build instructions, use the pinned revision and patch in [Engine/README.md](Engine/README.md). A custom backend build is not a distributable runtime unless its license permits redistribution. The package builder requires the exact backend hash and an explicit license acknowledgement; see [BUILD.md](BUILD.md).

Automated profile and launcher checks do not establish headset compatibility. The current record distinguishes source/build checks, desktop game startup, and headset gameplay testing in [Engine/VALIDATION.md](Engine/VALIDATION.md) and [Launcher/VALIDATION.md](Launcher/VALIDATION.md). No headset gameplay acceptance is claimed for the latest rendering/comfort fixes.
