# Far Far VR 0.4.2 local test

Launch `H:\FarFarVR Live\Far Far VR.exe`. This is the local custom-runtime candidate, not a proven fresh-download release.

- Start a fresh session: face forward in the lobby; gun sits at its original default; moving head and gun separately keeps the crosshair/X on the gun. Check empty hands, shield hits, minigun and another weapon.
- Walk and fight in a mission: performance, stereo depth, no split/warp; dash/damage effects fill the face rather than the distant HUD; POIs stay correct when turning the head.
- Check jump/dash/reload, weapon switching, map/ping, emote/battle cry, push-to-talk; quick taps and holds each produce only the intended action.
- Open the VR menu: trigger selection, A confirmation, controls popup/page, calibration, save/restart and updates page showing 0.4.2.
- Ride, emote, play a cutscene and transition maps: camera/comfort restore; lobby facing correct on return; remove/resume headset without broken stereo.
- Change a VR graphic/control setting, exit normally, launch flat: flat settings remain independent. Return to VR: VR settings persist.

Report any failures and whether performance is better/same/worse. The prior crosshair/X recovery was accepted, but performance was poor. This candidate removes periodic status writing and repeated TAA lookups; the latest user test reports that the crosshair works and performance seems better.

Public release remains blocked by the custom-runtime acquisition gap described in APPROVED-FEATURES-20261102.md. The user approved pushing the tested source. Local mod cleanup and fresh-install deletion have not occurred.
