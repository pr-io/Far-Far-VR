# Final touches - 2026-10-26

Latest wearer-feedback corrections are in [POLISH-20261026.md](POLISH-20261026.md):
full cinema surround, current movie dimensions, near unarmed dot, and mounted
camera orbit/collision constraints. The black-surround style is retained.

## Cutscene follow-up

The comfort module now detects the local player's `isInCinematic` flag, the
controller's `bCinematicMode` and camera-actor view targets used by Sequencer cuts.
During a detected cutscene it enables UEVR's existing 2D screen mode, disables
controller/head aim and roomscale-driven pawn movement, and uses stage-space
screen placement without decoupled-pitch UI adjustment. Head movement tracks the
wearer around the stationary screen instead of steering the movie camera.
Authored cutscene camera motion continues inside the screen. This intentionally
presents cinematics on a screen rather than as an immersive moving environment.

Checks run before and after the engine tick to catch camera changes before the
next render. On exit, prior screen, movement, aiming and UI settings restore.
The existing recovery journal also covers screen mode and roomscale movement.
Separate crosshair and world-marker projection stay hidden while screen mode is
active. Normal controller mappings, HUD placement and on-foot aim are unchanged.

43 simulated behavior/geometry/input/menu checks pass, including cinematic flags,
camera cuts without a pawn, pause/end/mount transitions, reset/crash recovery and
crosshair/marker suppression. The updated module loaded natively at the title
screen with no script error. The attempted tutorial validation was blocked by a
black remote-session display, repeated graphics `Present failed: 87a0001` errors
and denied desktop input. No live tutorial intro, new-game intro or outro has
passed acceptance. OpenXR and headset movement/rendering remain unverified.

## Earlier HUD, menu and mount pass

Changes:

- Separate original animated crosshair at 2 m from the firing camera; the other
  HUD elements retain the accepted 2.75 m view-following compositor placement.
- World-marker projection no longer depends on the mission HUD. Additional
  saloon marker classes and unowned level components are supported, with duplicate
  widget recreation prevented and full restoration on script reset.
- Menus temporarily use game-camera aim (0), stage-space UI and no decoupled-pitch
  UI adjustment. The right controller can still point; hand/head aim cannot rotate
  the game camera and the panel no longer follows head turns.
- Mounted pawns and mount view targets temporarily use game-camera aim (0),
  retaining normal headset tracking and stick look. The original aiming mode
  returns on foot. The existing mount-steering game setting is unchanged.
- A small recovery journal preserves the pre-override settings if UEVR saves
  while a menu is open or a mount is active. On script reset they restore
  immediately; after a crash they restore when the script loads next launch.
- The launcher updates all six named Far Far VR modules while the game is closed,
  making backups. Existing user configuration and binding files are preserved.

The crosshair uses a 1024-square world WidgetComponent for a 512-logical-pixel
canvas. Only the crosshair uses this surface; health and corner information stay
in the compositor. This is a new rendering path, not a literal restoration of the
original UEVR compositor crosshair. It shares the original animated materials
and updates its position in the stereo-view callback without physics queries.
Headset sharpness, visibility near geometry, shot alignment and frame time still
need wearer acceptance. The previous v7 compositor version is backed up.

Validation completed: 36 simulated Lua behavior/geometry/input/menu tests and a
Release launcher publish. These checks establish transition logic, original brush
and animation copying, shot-ray placement, HMD projection, marker adoption and
recovery behavior. They do not establish native UMG rendering or headset comfort.

The inspection launch reached the title notice and loaded the runtime, but OpenXR
initialization failed (-51). Windows game input was intermittently denied through
the desktop-control helper during the remote session. After a fresh launch,
desktop input worked. The new modules loaded in the real offline saloon:

- The title and pause contexts reported aim 0 and UI_FollowView false.
- Resuming in the saloon reported aim 2 and UI_FollowView true.
- The marker module adopted 15 saloon widgets, including world icons and NPC
  indicators, without script errors. Native inspection confirmed their original
  screen components no longer held duplicate widgets.
- The crosshair's 1024-square WidgetComponent and original animated brush copies
  built and synchronized without a script error. It remained hidden because the
  VR runtime was unavailable; the original desktop crosshair remained visible.
- Mount activation in the starting saloon area was refused by the game because
  mounting is only allowed in the nearby fields. Actual mounted camera behavior
  therefore remains untested.

These checks do not establish headset rendering or comfort. The offline run was
closed; the exact pre-test config was restored to avoid incidental engine-save
changes. Controller bindings remain byte-for-byte unchanged. Native status
files and the runtime inventory are retained only in the local recovery copy.

Required runtime checks: notice/title/options/pause/menu transitions with a moving
head and right hand; saloon POIs with a stationary head and moving controller;
mission POIs; horse walk/sprint/turn/dismount; separate crosshair visibility,
sharpness, hit feedback and shot alignment with head/hand movement independently.
Do not interpret the code checks as these checks passing.
