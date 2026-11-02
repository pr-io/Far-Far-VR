# Far Far VR

![Far Far VR launcher](docs/farfarvr-launcher.png)

**Created and published by PR_VR**

## Links
- [Far Far West on Steam](https://store.steampowered.com/app/3124540/Far_Far_West/)
- [UEVR](https://github.com/praydog/UEVR)
- [Flat2VR](https://flat2vr.com/)

Far Far VR is an unofficial **PCVR mod for [Far Far West on Steam](https://store.steampowered.com/app/3124540/Far_Far_West/)**, created and maintained by **PR_VR**. Powered by [UEVR](https://github.com/praydog/UEVR) created by praydog. It runs with the Windows PC version of the game and a supported PCVR headset; it is not a native standalone-headset app. 

This repository contains the launcher, player profile, game-specific Lua modules, OpenXR Touch bindings, engine patches, headers, and package scripts. It is designed exclusively for [Far Far West on Steam](https://store.steampowered.com/app/3124540/Far_Far_West/) to provide a seamless, console-like PCVR experience.

> **Early access:** Far Far VR is in active development. Compatibility and comfort can vary by PC, headset, OpenXR runtime, and game update; back up your profile and settings before installing.

> **Clean Distribution & Runtime Auto-Fetch:** To respect third-party copyright and open-source boundaries, Far Far VR releases do not bundle third-party runtime binaries (`UEVRBackend.dll`, `LuaVR.dll`, `openxr_loader.dll`, `openvr_api.dll`). On first launch, the Far Far VR launcher automatically downloads and verifies the compatible [UEVR](https://github.com/praydog/UEVR) runtime package directly from the public repository. See [third-party notices](THIRD_PARTY.md).

## Player Installation & Launch

Requirements: 64-bit Windows 10/11, [Far Far West on Steam](https://store.steampowered.com/app/3124540/Far_Far_West/) installed through Steam, a compatible PCVR headset, and an active OpenXR runtime.

1. Extract the Far Far VR release package.
2. Run **Far Far VR.exe**. In **Settings**, confirm the detected Steam installation folder for [Far Far West on Steam](https://store.steampowered.com/app/3124540/Far_Far_West/).
3. Connect your PCVR headset (OpenXR runtime active).
4. Click **START IN VR**. Far Far VR automatically:
   - Downloads and verifies the compatible [UEVR](https://github.com/praydog/UEVR) runtime if not already present on your system.
   - Sets up or updates the [Far Far West on Steam](https://store.steampowered.com/app/3124540/Far_Far_West/) profile and Lua modules in `%APPDATA%\UnrealVRMod\FarFarWest-Win64-Shipping`.
   - Launches the game through Steam and waits for the title screen.
   - Injects the verified compatibility backend and confirms stereo/engine hooks.
5. In your headset:
   - Hold **Start** or press **Insert** on keyboard to toggle the Far Far VR menu.
   - Use **Calibration** to adjust standing/seated height and position.
   - Configure comfort options, HUD layout, and controller preferences live.

### Built-in Updater

Far Far VR includes a built-in updater. Use the **Updates** tab in the in-game menu to check for new releases and stage an available update. The launcher installs staged updates automatically the next time it starts after [Far Far West on Steam](https://store.steampowered.com/app/3124540/Far_Far_West/) and the launcher have been closed.

## Credits, Attribution & Disclaimers

- **Mod Author & Maintainer:** [PR_VR](https://www.youtube.com/@PR_VR)
  - YouTube: [PR_VR on YouTube](https://www.youtube.com/@PR_VR)
  - Community Discord: [VR Buddies Discord](https://discord.gg/PcsYNa6MS9)
- **VR Framework:** Powered by [UEVR](https://github.com/praydog/UEVR) created by praydog and contributors from the [Flat2VR](https://flat2vr.com/) community. Immense gratitude to praydog for the groundbreaking universal Unreal Engine VR injector and API.
- **Game & Third-Party Disclaimers:**
  - Far Far VR is an unofficial community mod. Far Far VR has no affiliation with, and is not endorsed by or connected to, the developers of [Far Far West](https://store.steampowered.com/app/3124540/Far_Far_West/), Epic Games, or Unreal Engine.
  - No copyrighted game assets, binaries, audio, textures, or save files are included in this distribution. Players must purchase and install the original game on [Steam](https://store.steampowered.com/app/3124540/Far_Far_West/).

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
To stage a clean release package in `dist\FarFarVR` (without third-party binaries):
```powershell
pwsh -File scripts\Build-Package.ps1
```

See [BUILD.md](BUILD.md) and [Engine/README.md](Engine/README.md) for native engine compilation and patch reproduction.
