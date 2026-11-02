# Far Far VR

Far Far VR is an unofficial **PCVR mod for Far Far West**, distributed with its launcher and injector. It runs with the Windows PC version of the game and a supported PCVR headset; it is not a native standalone-headset app. This repository contains the launcher, player profile, game-specific Lua modules, OpenXR Touch bindings, engine patches, headers, and build/package scripts. It is designed exclusively for Far Far West and does not require generic UEVR tools or separate configuration.

> **Early access:** Far Far VR is still in active development. Compatibility and comfort can vary by PC, headset, OpenXR runtime, and game update; back up your profile and settings before installing. This is a Windows PCVR mod, not a standalone-headset app.

> **Current Backend:** The verified Far Far VR runtime backend has SHA-256 `2F30D9353B02435F0D34A6505D2FF494F4DACD20783F8D72CB408F9AEBE41558`. The launcher rejects stock or mismatched backends to ensure game-specific cinematic, controls, and comfort fixes are preserved. See [third-party notices](THIRD_PARTY.md).

## Player Installation & Launch

Requirements: 64-bit Windows 10/11, Far Far West installed through Steam, a compatible PCVR headset, and an active OpenXR runtime.

1. Extract the Far Far VR package.
2. Run **Far Far VR.exe**. In **Settings & options**, confirm the detected Steam installation folder for Far Far West.
3. Connect your PCVR headset (OpenXR runtime active).
4. Click **START IN VR**. Far Far VR automatically:
   - Sets up or updates the Far Far West profile and Lua modules in `%APPDATA%\UnrealVRMod\FarFarWest-Win64-Shipping`.
   - Launches the game through Steam and waits for the title screen.
   - Injects the verified compatibility backend and confirms stereo/engine hooks.
5. In your headset:
   - Hold **Start** or press **Insert** on keyboard to toggle the Far Far VR menu.
   - Use **Calibration** to adjust standing/seated height and position.
   - Configure comfort options, HUD layout, and controller preferences live.

### In-Menu Updates

The **Updates** tab checks the `pr-io/Far-Far-VR` GitHub release list in the background. Public releases need no token. While early-access releases remain private, save a fine-grained GitHub token limited to this repository with **Contents: read-only** access at `%LOCALAPPDATA%\FarFarWestVRLauncher\github_token.txt`. The launcher never writes the token into the profile or release files. Downloaded packages are staged under `%LOCALAPPDATA%\FarFarWestVRLauncher\pending_update`; the next launcher start applies them only after the launcher and game have exited.

## Community

- YouTube: [https://www.youtube.com/@PR_VR](https://www.youtube.com/@PR_VR)
- Discord: [https://discord.gg/PcsYNa6MS9](https://discord.gg/PcsYNa6MS9) (VR Buddies Discord)

## Controls & In-Game Features

![Far Far VR controller controls](docs/farfarvr-controls.jpg)

- **VR Menu:** Hold **Start** or press **Insert**. (A quick Start press opens the native game menu).
- **Map & Ping:** Click the right stick to toggle the map; hold right stick for 0.5s to ping.
- **Weapons & Voice:** Tap **Y** to switch weapons; hold **Y** for 0.5s to push-to-talk.
- **Emotes & Battle Cry:** Click both thumbsticks together to open emotes (**A** confirms). Hold both thumbsticks for 0.5s to trigger battle cry without opening the emote wheel.
- **Cursor Screens & Pointer:** On menu screens with a cursor, controller pointing is enabled by default. Point with either hand and pull the trigger to click. Moving a thumbstick switches back to gamepad navigation.
- **Camera & Stereo Comfort:** Native stereo rendering with automatic letterboxing/screen stabilization during cutscenes, conversation cameras, and mount transitions.
- **Independent Gun Aiming:** Tracked-hand weapon aiming is decoupled from headset camera rotation.
- **HUD & Reticle:** Custom in-world floating HUD with weapon-following reticle and hit markers.

## Calibration & Height

1. Put on your headset and stand or sit in your neutral play position.
2. Open the Far Far VR menu and choose **Calibration**.
3. Select **Calibrate current height** to align headset height with the game's camera origin.
4. **Recenter position** aligns your horizontal origin.
5. Fine adjustment sliders for height, forward/back, and horizontal offsets are available and can be reset with **Reset height & camera**.

## Development & Build

### Launcher (.NET 8 SDK, Windows x64)
```powershell
dotnet publish Launcher\FarFarWestVR.csproj -c Release -r win-x64 -o Launcher\publish
```

### Automated Tests
Run unit tests for Lua behaviors (requires Python 3.13):
```powershell
python -m unittest discover -s tests -v
```

Run C# launcher and binding tests:
```powershell
dotnet run --project tests\SessionLinkTests\SessionLinkTests.csproj
dotnet run --project tests\SettingsIsolationTests\SettingsIsolationTests.csproj
dotnet run --project tests\SocialBindingsTests\SocialBindingsTests.csproj
dotnet run --project tests\UpdaterTests\UpdaterTests.csproj
```

### Building the Release Package
To stage a complete release package in `dist\FarFarVR`:
```powershell
pwsh -File scripts\Build-Package.ps1
```

See [BUILD.md](BUILD.md) and [Engine/README.md](Engine/README.md) for native engine compilation and patch reproduction.
