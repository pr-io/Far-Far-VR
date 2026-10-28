# Far Far VR controller mappings

The bundled profile includes the corrected OpenXR Touch action map. Existing
camera, rendering, left stick, horizontal turning, grip and trigger conversion
are preserved.

| Control | Action |
| --- | --- |
| Left menu / Start tap | Game menu (on release) |
| Left menu / Start hold > 0.5 seconds | Map via UEVR's native Back/View output |
| Right stick click | Ping |
| Right stick down, 95% travel | Utility (`InputGrenade`) |
| Right stick up, 95% travel | Melee |
| Left trigger | Aim |
| Left grip / right grip | Original LB / RB, including both held together |

The vertical filter in `scripts/farfarvr_input.lua` engages at 95% and stays
engaged until the stick retreats below 85%. This prevents chatter near the edge.
It bypasses physical gamepads, other controller slots and the UEVR settings UI.
It also affects vertical navigation in the game's own menus. It never changes
the right stick's horizontal axis, the left stick, triggers or any button bits.
The game handles its existing LB+RB third-spell chord; no artificial extra
spell or delayed shoulder-button input is added.

The launcher Input page can optionally apply four game-level Enhanced Input
overrides. It refuses unknown save formats or unexpected values and backs up
the original beside the save before writing. Melee is already bound to stick
up. The input save is user-specific and is never copied into the baseline
profile or bundled with distributions. `scripts/Set-ControllerBindings.py`
remains a development/test utility; players do not need Python.

With the game closed, restore the `EnhancedInputUserSettings.sav.before-vr-input-*`
backup over `EnhancedInputUserSettings.sav` in
`%LOCALAPPDATA%/FarFarWest/Saved/SaveGames/`, and move `farfarvr_input.lua`
out of `%APPDATA%/UnrealVRMod/FarFarWest-Win64-Shipping/scripts/` to restore
the original controls.

Automated verification exercises the Lua filter through simulated XInput
states, including unchanged LB+RB and triggers. Desktop verification recorded
the intended game Controls page assignments and successful Lua initialization.
Physical controller range, game spell dispatch and in-headset acceptance remain
unverified.
