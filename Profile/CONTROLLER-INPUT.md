# Controller adjustments

Installed locally on 2026-10-25. Existing camera, rendering, OpenXR bindings,
left stick, horizontal turning, grip and trigger conversion are preserved.

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

The active Enhanced Input save has four overrides changed with
`scripts/Set-ControllerBindings.py --apply`; that script refuses unexpected
bindings and backs up the original beside the save before writing. Melee is
already bound to stick up. The input save is user-specific and is not copied
into the baseline profile or bundled with distributions.

Recovery with the game closed: restore the `EnhancedInputUserSettings.sav.before-vr-input-*`
backup over `EnhancedInputUserSettings.sav` in
`%LOCALAPPDATA%/FarFarWest/Saved/SaveGames/`, and move `farfarvr_input.lua`
out of `%APPDATA%/UnrealVRMod/FarFarWest-Win64-Shipping/scripts/`.

Automated verification: `python tests/test_input.py` exercises the real Lua
filter through simulated XInput states, including unchanged LB+RB and triggers.
It does not prove the physical controller's range or the game's spell dispatch.
Physical acceptance: tap Start opens menu; hold opens map without opening menu;
stick click pings; slight/partial up/down does nothing; full up/down activates
melee/utility; left trigger aims; both grips cast the third spell. Check both
grips together and with a slight stagger in either order.

Desktop verification on 2026-10-25: launched through Far Far VR; UEVR logged
loading `farfarvr_input.lua` and completing Lua initialization without a script
error. The game's actual Controls page showed Utility = right stick down,
Melee = right stick up, Aim = LT, Map = Back/View, Ping = right stick click,
Spell 1 = LB, Spell 2 = RB, Spell 3 = LB+RB. Closed the verification run.
The headset was unavailable (`XR_ERROR_FORM_FACTOR_UNAVAILABLE`), so physical
input and in-headset acceptance remain pending. The verification launcher used
the configured dist runtime, which dropped eight unknown OpenXR settings on
save; restored the exact pre-test config after closing. Final config and Touch
mapping hashes match their backups; all four new game overrides remain saved.
