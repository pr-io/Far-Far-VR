# Local menu and riding comfort pass - 2026-10-25

**2026-10-26 update:** `farfarvr_comfort.lua` now handles menus and mounted
contexts separately. Menus use stationary stage-space UI with game-camera aim;
mounts use game-camera aim without changing the accepted gameplay HUD placement.
Prior settings restore when leaving those contexts, including through a recovery
journal after a saved override or crash. See [FINAL-TOUCHES.md](FINAL-TOUCHES.md).
These changes still require runtime/headset acceptance; the settings-only pass
described below did not fix the reported menu or sprint camera movement.

The cutscene follow-up also presents detected cinematics on a stationary cinema
screen, disabling aim and roomscale-driven scene movement for their duration.
Prior VR settings return afterward. Cutscene playback and comfort still need
headset acceptance; see FINAL-TOUCHES.md for detection and test boundaries.

Two active UEVR settings changed:

| Setting | Before | After |
| --- | --- | --- |
| UI_Distance | 2.0 m | 2.75 m |
| UI_FollowView | false | true |

The UI retains its 2 m physical height and zero X/Y offsets. Greater distance
reduces its angular size. In OpenXR, follow-view uses view space, bypassing the
game-camera rotation and stage origin that could move the UI with controller
aim or pitch it above the user at transitions. This is a shared game-UI setting:
it also affects HUD and map placement, and follows head turns. It does not lock
the panel in world space or distinguish the notice/menu/results screens.

In the game's Options > Controls, changed Control Mount with Camera from Yes
to No. The game persisted `mountCameraControl` = `0` in userSettings.sav.
This removes camera-driven mount steering while preserving UEVR controller aim.
It does not change the aiming system specifically while mounted.

Verification: the launched UEVR Runtime page displayed UI Distance = 2.750 and
UI Follows View checked; the game Controls page displayed mount control = No;
the game settings save contains the new value. The Enhanced Input binding save
remained byte-for-byte unchanged, preserving the previous controller work.
The headset was unavailable (XR_ERROR_FORM_FACTOR_UNAVAILABLE), so notice,
main menu, pause menu, start/results screens, readability, controller pointing
and actual mounted travel need headset acceptance. No session was started.

Backups: `[local UEVR workspace]/diagnostics/menu-comfort-20261025-145732/` contains the prior
UEVR config, Touch bindings, game user settings and game input settings.
To revert UI only, with the game closed, set UI_Distance=2.000000 and
UI_FollowView=false in the active config. To revert mount steering only, set
Control Mount with Camera to Yes in the game's Controls menu.
