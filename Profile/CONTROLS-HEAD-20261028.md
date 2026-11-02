# Controls, head-directed movement and saloon aim — October 28

This batch follows the wearer's positive gameplay report on the October 27
build, with the remaining issues and control changes requested overnight.
The normal `H:/UEVR/Far Far West VR.exe` launcher is the entry point.

| Gesture | Action |
| --- | --- |
| Quick Start press | Default game menu |
| Hold Start for 0.5 seconds | Toggle UEVR settings, without releasing |
| Quick right-stick click | Toggle map |
| Hold right-stick click for 0.5 seconds | Ping, without releasing |
| Click both sticks together | The game's emote action |

The later social-controls repair supersedes this row: quick both-stick click
opens/closes the emote wheel; a 0.5-second both-stick hold triggers battle cry.
See `SOCIAL-CONTROLS-20261028.md` for the saved-binding repair and validation.
| Insert | UEVR settings, as before |

Long holds do not also trigger short taps and require release before another
activation. A 150 ms chord window prevents a near-simultaneous two-stick click
from also mounting or pinging. The original left-stick click/mount behavior is
retained outside that brief window. Emote holds retain the game's normal button
semantics. Axes, triggers, face buttons and shoulder combinations retain their
existing handling. The mounted analog-look exception is retained.

These shortcuts are scoped in the native backend to the exact Far Far West
executable, ahead of the upstream Start-to-Back conversion. Timing uses a
monotonic clock and also runs during engine ticks, so opening a menu does not
depend on a release or on another gameplay input poll. The old two-stick UEVR
menu and headlocked-aim shortcuts are disabled for this game. The control policy
has standalone native tests for tap, hold, rearming, menu cancellation, focus
loss and chord isolation.
Live native inspection confirmed `IA_Map -> Gamepad_Special_Left` and
`IA_Emote -> Gamepad_DPad_Left`. The native chord resolves that emote mapping
from the game asset; it does not assign an assumed controller convention.

The saloon dot no longer requires a drawn, reloadable firearm before the
controller-aimed original crosshair can be shown. Its original brush, animation,
visibility and per-view aim projection are retained. Movement direction is set
separately to Headset (1), while normal aim remains Right hand (2). This preserves
controller aiming while pushing forward moves toward the headset's facing.

The game menu's quad captures the current HMD position and horizontal facing
when it opens. It stays there while open. This avoids placing it relative to an
old stage calibration origin after walking around the saloon; it does not
recenter the game world, move the HMD or change POI projection.

At cutscene exit, the native OpenXR path retains the last movie quad and its
pixels/pose until the corrected right-eye packet is ready. Native scene capture
can resume underneath while the wearer still sees the movie. This targets the
remaining flash of the pre-cutscene game image. Cached movie layers are dropped
on swapchain replacement/reset, and opening VR settings permits recovery. The
accepted movie crop, letterbox, opaque surround and BOTH-eye presentation remain.

The headset must still verify the new physical gestures, menu placement,
saloon dot, movement direction and cutscene handoff. A build, policy test or
native hook marker is not a wearer acceptance result.

## Validation and installed build

- Release backend and launcher builds succeeded. All 57 Lua/profile tests,
  33 native offline checks and the standalone native gesture-policy suite pass.
  The tests include first input after resting the controllers, cancellation of
  held chords across menu transitions, both chord orders and one-shot hold
  activation without release.
- The normal canonical launcher loaded the previous build from
  `dist/FarFarVR/UE5.8`, and both engine/stereo hook markers completed. Native
  asset inspection confirmed the actual map and emote mappings.
- The POI and mount scripts, Touch binding file and game input save are
  byte-for-byte unchanged. Installed modules match the source and package.
  The native patch passes reverse-application checking against the checkout.
- OpenXR initialization failed (-51), with a black remote display and graphics
  presentation errors. Consequently no new physical button, saloon aim,
  head-directed walking, menu placement or cinematic visual pass is claimed.
  The diagnostic game was closed and saved menu/idle overrides were removed by
  restoring the pre-test config/journal with only the two requested configuration
  changes: movement orientation 1 and the old two-stick menu toggle disabled.

Backend SHA256 for this control batch (before the later regression repair):
`11B89B0E8D6035A0B234041E223F92B69A4ABF8A5A0813B9A1FCD39782382AB7`.

Pre-change source, runtime, launchers, input save and active profile:
`H:/UEVR/diagnostics/controls-head-20261028/`.
Close the game before using `Restore-Previous-Runtime.ps1` there to restore
the October 27 runtime, active profile and bundled profile. The updated native
and launcher development sources are retained.

The right-stick actions were swapped at the user request: tap toggles the map and hold pings. Headset verification remains pending.
