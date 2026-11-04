# Far Far VR v0.4.7

## Changes

- Resolution scale applies live when you press **Save settings**. Moving the slider stages the change; Revert discards it.
- Rebuild resolution-dependent stereo textures safely when changing scale, including packed stereo sources.
- Fit the Far Far VR settings panel within the headset view, with compact tabs and visible Display save controls.
- Preserve facing direction when the HUD recovers after a settings change; recovery no longer recenters the same player again.
- Release the OpenXR session before Windows tears down runtime worker threads, and restore desktop settings before the launcher closes.
- Preserve saved comfort preferences independently of temporary menu, riding and cutscene overrides.
- Improve update command handling, stable-release selection, download validation and recovery from interrupted installation. Keep desktop and headset update controls.
- Recover temporary feature failures, prune stale objectives, improve firing-endpoint reticle projection and remove unused rendering code and duplicate controls assets.

## Install or update

Download **FarFarVR-v0.4.7.zip**, extract the entire archive, and run **Far Far VR.exe**. Keep all included folders together. Existing players can also use the launcher or headset Updates page. Close the game and launcher before applying an update.

The launcher downloads the pinned upstream runtime when needed and applies the bundled customization delta locally. The separate `backend.ffvrdelta` asset is for rebuilding the package; players should download the complete ZIP.

## Validation and known issue

Live resolution Save and menu fit were checked with a Quest 3 using Meta OpenXR, and the user tested the local update before authorizing publication. Automated Lua, launcher, updater and runtime reconstruction checks also passed.

**Steam can still report Far Far West as running after the game exits and refuse a relaunch.** This was also reproduced with the VR backend, Lua and OpenXR completely unloaded, using both window close and the game's Quit command. Restarting Steam clears the retained session. This release does not claim to resolve that separate game/Steam session issue.
