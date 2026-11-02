# Far Far VR production UI

The normal backend now includes the Far Far VR menu, as requested. See [launcher/menu behavior and validation](../Profile/UNIFIED-MENU-20261030.md). The previous preview-only promotion restrictions below describe history; the current user request authorizes this local replacement. No public release has been performed.

Reproduce the current production source with the pinned UEVR revision, `cinema-comfort.patch` in the UEVR root and `farfar-console.patch` in its pinned UESDK checkout. The cinema patch now includes the focused menu and cumulative accepted repairs. Do not apply focused-menu.patch on top; it is a UI-only reference subset. Build with scripts/Build-FarFarVR.ps1 or scripts/Build-Engine.ps1; the old diagnostics build wrapper intentionally removes the menu and must not be used for this version.

Keep the runtime hash in Launcher/Setup.cs and scripts/Build-Package.ps1 synchronized with the compiled backend. The original UI is available through the explicit recovery marker described in the profile note. No compatibility or graphics settings are changed merely by opening the new menu.

---

Historical development notes follow.

# Local focused-menu development

The checked-out source in `../vendor/UEVR` is pinned to the accepted runtime's UEVR revision `963b157573eaef20528b602d1d9790b8105819b3`. It is excluded from the main project's Git tracking. `focused-menu.patch` has been compiled and desktop-tested as a local menu preview. It has not been approved for redistribution or accepted in a headset.

Run `../scripts/Prepare-Engine.ps1` to reproduce the checkout and patch. It checks the revision and patch applicability and leaves other source changes alone. The patch changes three UI files only: Framework.cpp, LuaLoader.cpp and LuaLoader.hpp. It does not change the stereo renderer, engine hooks, projection, textures, controller conversion or game executable.

The custom route requires BOTH the exact FarFarWest-Win64-Shipping.exe process and a `farfarvr-menu.enabled` file in its persistent profile. The marker is now installed locally for preview testing. The original menu remains available with the marker absent; remove it and restart for recovery. The normal accepted backend ignores this marker.

The route uses the original menu visibility, Insert/thumbstick handling, input capture, window positioning, overlay projection and close-state persistence. Only the menu contents and window title change. It invokes the `Far Far VR` Lua panel directly, bypassing unrelated sidebar entries. It shows headset runtime status and an explicit retry action using upstream initialization. Startup waits for Lua initialization before reporting a missing module. Missing modules and Lua failures display an error with a Return to game button.

## Try the local preview

Open the desktop shortcut **Far Far VR Menu Preview**, then Start Game in VR. The shortcut selects `Experimental/MenuPreview/UE5.8` explicitly and uses separate `preview-settings.json` preferences. The normal launcher still selects the accepted runtime; its settings and DLLs were not replaced. Close the game fully before switching between builds.

Preview backend SHA256: `DCDFF1DAB571EF183DE7D5697A60F450A8D50308F0E67B15785382919261B019`. The launcher allows this exact hash only with an explicit `--runtime` override. Local provenance is in `Experimental/MenuPreview/UE5.8/BUILD-SOURCE.txt`.

The same panel can be tested with the accepted unmodified engine: run `../scripts/Install-Menu.ps1` while the game is closed, launch, open the menu, enable Show Advanced Options, and choose **LuaLoader > Far Far VR**. This temporary development path still exposes the universal UEVR menu.

## Build and promotion boundary

Visual Studio 2022 Build Tools are now installed at `C:\BuildTools\VS2022` with the x64 C++ compiler, Windows 11 SDK and CMake. `../scripts/Verify-Toolchain.ps1` successfully compiled, linked and ran a C++23 Windows executable using filesystem, format and mutex support. The installer completed with exit code 0.

UESDK access is now resolved through the user's Epic-linked GitHub account. The pinned source's `COMPILING.md` requires this access because UESDK is an Unreal Engine fork. This source-access requirement is separate from permission to distribute a customized UEVR release.

Initialize submodules at the recorded revisions, then run `../scripts/Build-Engine.ps1`. It checks both pinned revisions, sets up the installed x64 developer environment and builds into `vendor/UEVR-build`, outside the accepted runtime. Tracy uses its exact pinned source archive with a recorded checksum to avoid downloading the entire Git history. Shader compile checks write into the build directory; the original checked-in shader bytecode remains unchanged. `../scripts/Test-Engine.ps1` builds and runs the upstream offline checks. A rebuilt DLL receives a new hash and must be reviewed before adding it to the preview allowlist.

Before promoting a custom DLL: verify a reproducible clean build, test macro open/close and focus, live edits/revert/save/restart, missing-script recovery, controller input return, title screen/gameplay/level changes and headset frame timing. Capture the new DLL hash and provenance and explicitly add the tested build to the launcher's allowed runtime policy. The launcher deliberately still rejects unvalidated custom binaries.

No automatic engine code removal is part of this milestone. Permission requests/public releases remain a later user decision.

## Module behavior tests

The October 28 regression repair also applies `farfar-console.patch` inside
the pinned `dependencies/submodules/UESDK` checkout. It adds the exact Far Far
West 5.8.1 console ABI witness; `cinema-comfort.patch` applies in the UEVR root.
The standalone `scripts/Test-FarFarRuntime.ps1` checks that witness and the
cinematic handoff policy. Both patches are required to reproduce the repaired
runtime. See `Profile/REGRESSION-REPAIR-20261028.md` for evidence and limits.

With Python 3.13: `python -m pip install --target tools/lua-test lupa`, then `python tests/test_menu.py` from the project root. Twelve tests execute the actual menu script in Lua 5.4 with a simulated UEVR/ImGui API. They verify edit boundaries, height-origin calibration, headset gating, camera-only reset, errors and startup diagnostics; they are not desktop or headset acceptance.

The 0.3 menu/launcher design uses the shared `Profile/VRSettings.json` catalog and generated `Profile/scripts/farfarvr_settings.lua`. Regenerate with `scripts/Generate-Settings.py` after catalog edits. The launcher deploys both named Lua modules, backing up replacements, and leaves controller mapping files untouched. Calibration actions use the original UEVR pose/origin API; no new camera or tracking solver was added. ImGui styling is scoped to the focused menu and restored afterward.
