# Far Far VR v0.4.8

## Changes

- Keep the Far Far VR settings panel level while preserving the horizontal direction it opened toward.
- Ignore Steam's `--start` argument. Opening the launcher no longer starts Far Far West automatically; start the game with the launcher button.
- Release Meta/OpenXR before game-process teardown. If runtime teardown blocks for five seconds after the game requests exit, a watchdog ends that exact game process.
- Keep the launcher Updates button and verify the installed native backend against this release.

## Install or update

Download **FarFarVR-v0.4.8.zip**, extract the entire archive, and run **Far Far VR.exe**. Keep all included folders together. Existing players can use the launcher or headset Updates page. Close the game and launcher before applying an update.

The launcher downloads the pinned upstream runtime when needed and applies the bundled customization delta locally. The separate `backend.ffvrdelta` asset is for package builders; players should download the complete ZIP.

## Validation and known issue

The native backend and launcher were built. The user confirmed the in-game menu is level. No game or headset test was run for this release.

The user reports Steam can still show Far Far West as running after closing it. The last local Steam process log recorded the game AppID removed from its running list after normal game shutdown; this release does not claim to resolve a stale Steam UI status.
