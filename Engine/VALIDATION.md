# Menu milestone — October 24, 2026

- Accepted original and packaged backend hashes both remain `FEFF6417359F6E36ABAC6577598A86B96D485A023A850F8614043AA0A508C8CB`.
- Pinned source checked out locally, focused-menu patch prepared and `git diff --check` passed. Patch reproduction recognizes the already applied patch without changing other files. Only Framework.cpp, LuaLoader.cpp and LuaLoader.hpp change; no rendering or game hook file is modified.
- Actual module loaded from the game's persistent scripts folder at 13:49:09. The unchanged packaged backend was confirmed loaded by the launcher; engine hooks completed at 13:49:10.
- Visually inspected Far Far VR under LuaLoader in the actual desktop game menu. All four profile values were read successfully through the real UEVR API.
- Clicked Snap turning; the toggle changed to enabled and the panel reported Applied after verifying the engine's live value. Clicked Revert and observed it return to disabled. Requested Save, then read config.txt: snap turn false, turn angle 45, world scale 1, HUD distance 2. No comfort changes were left applied.
- Touch binding SHA256 remained `1602DF8B4CD4D70A23D0942CFE39292F3C6BEB0E8CB4DC282CD4ECD211F524CD`.
- Eight Lua 5.4 behavior tests pass with a simulated API: read-only open, live edit/revert, saved revert point, unavailable value, range violation, rejected engine write, reopen/external edits and explicit recenter action.
- OpenXR reported XR_ERROR_FORM_FACTOR_UNAVAILABLE during this desktop run. This is not headset rendering, macro/controller, gameplay or frame-time acceptance. The controller macro has not been exercised in this milestone.
- At this initial module milestone, the focused-menu source patch was not yet compiled. The subsequent compiled preview results are recorded below.

## Build tools installation and continued build preparation

- Downloaded the Visual Studio 2022 Build Tools bootstrapper from Microsoft's official endpoint and verified its Authenticode signature as valid, signed by Microsoft Corporation.
- Installed Build Tools to `C:\BuildTools\VS2022`, selecting VC.Tools.x86.x64, VC.CMake.Project and Windows11SDK.26100 with the C++ tools workload. Installer exit code 0.
- CMake reports 3.31.6-msvc6. The actual C++23/x64 smoke executable compiled, linked against Windows SDK APIs and ran successfully with MSVC 1944. This verifies installation, not the UEVR backend.
- Public glm, imgui and spdlog submodules were initialized at pinned revisions. Required UESDK download failed with Repository not found.
- Initially authenticated local Git through Git Credential Manager as `pr-io`; requests to EpicGames/UnrealEngine, praydog/UESDK and joeyhodge/UESDK returned Repository not found. Access was subsequently resolved by the user, enabling the pinned UESDK download.
- Added reproducible build and toolchain-check scripts. Build preflight refuses to proceed without the exact pinned UESDK source. The accepted runtime hash remains unchanged.

## Compiled focused menu preview

- UEVR and UESDK checkouts match the recorded working revisions. Final tracked source changes are only Framework.cpp, LuaLoader.cpp and LuaLoader.hpp. Original renderer/hook source and the checked-in shader bytecode have no diff. Shader compilation validation writes to the build directory.
- Release backend and Lua API builds succeeded with upstream compiler warnings. The final backend SHA256 is `622A9218D197EE9214F76580829CDF593786F0C365BD6C81626763628B05F45C`; LuaVR SHA256 is `7729CDA355E31178B53142FAA66E54699992C405D9B6C5CEA943A337431F9897`.
- All 33 upstream offline checks passed. Nine Lua behavior tests passed, including startup-error logging. These do not substitute for game/headset checks.
- Staged a separate runtime at `Experimental/MenuPreview/UE5.8`. Its launcher explicitly selects this runtime with `--runtime` and uses separate preview preferences. The preview hash is accepted only in that mode. The normal launcher preferences and accepted runtime were not replaced.
- In the running preview, visually confirmed the Far Far VR window replaces the universal sidebar. No LuaLoader navigation or duplicate universal settings appear. Verified live snap-turn enable, revert to false, Save settings, Return to game and Insert reopening the focused menu.
- Startup initially displayed a missing-module message before the script's first initialization. Added a UI-only initialization guard and startup diagnostics. A fresh launch confirmed the engine hooks and displayed the complete focused menu.
- Saved original values remain snap turn false, angle 45, world scale 1 and HUD distance 2. Touch binding hash remains `1602DF8B4CD4D70A23D0942CFE39292F3C6BEB0E8CB4DC282CD4ECD211F524CD`.
- OpenXR reported XR_ERROR_FORM_FACTOR_UNAVAILABLE during desktop tests. Headset/controller macro, stereo projection, gameplay, calibration, level transitions and frame timing remain unverified. The new menu accurately shows the connection issue and offers an explicit upstream retry action.
- The game was left running with the final preview menu visible. A Far Far VR Menu Preview shortcut is on the desktop. Close the game before switching to the normal runtime.

## Height correction, expanded settings and modern design

- Source inspection confirmed VR::recenter_view changes rotation offset only. It does not reset camera translation or standing origin.
- Backed up the prior profile under `diagnostics/design-calibration-before`, then reset camera up offset 4.447802 to zero. Forward/right offsets remain zero and world scale remains 1. The bundled baseline profile is neutral too. No claim is made that this alone establishes the wearer's final physical eye height.
- Introduced a shared 22-control catalog for Comfort, Calibration, Input and HUD; a generated Lua module keeps the launcher and in-game labels, ranges and choices aligned.
- Added headset-gated session height calibration (vertical origin only), full position recenter, and a camera-offset-only reset. Facing recenter remains separate. No mapping reset is exposed.
- Rebuilt the UI-only native patch with scoped dark colors, rounded controls, warm accents, larger padding and a wider readable menu. The normal launcher and preview launcher now use the same modern theme, dark title bar and grouped settings navigation.
- Actual launcher UI testing saved a temporary height value of 5, then used Reset height & camera and Save changes to restore zero. Both operations preserved the Touch binding hash. Original saved comfort/aim values remain unchanged.
- Actual Start in VR button launched Steam, waited for the title screen and loaded the current preview. Engine hooks were confirmed. Visually checked all four in-game pages and verified the no-headset calibration guard reports the connection requirement.
- Current preview backend SHA256: `DCDFF1DAB571EF183DE7D5697A60F450A8D50308F0E67B15785382919261B019`. Accepted original backend remains `FEFF6417359F6E36ABAC6577598A86B96D485A023A850F8614043AA0A508C8CB`.
- All 33 upstream checks passed again; all 12 Lua tests passed, including height-only origin changes, missing-headset refusal and camera reset preserving aim/mapping. Build succeeded without launcher warnings/errors; unchanged native dependency warnings remain recorded in its build log.
- Final physical height, controller macro, headset menu readability/projection and gameplay remain wearer acceptance checks. OpenXR still reports no available headset in this desktop run.
