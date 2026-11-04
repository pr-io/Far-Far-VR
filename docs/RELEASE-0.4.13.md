# Far Far VR v0.4.13

## Crosshair correction

This release corrects the crosshair aiming change introduced in v0.4.12 using the earlier wearer-accepted implementation as the reference.

- Drawn weapons use the game firing camera location and forward direction, sampled per stereo view, matching the camera properties used by F_TraceBullet before spread.
- Restore selection of the local player viewport HUD instead of accepting nested or ownerless copies.
- Retain tracked-hand aiming for empty hands, original animated crosshair and hit artwork, and the cumulative engine, controls, comfort, launcher, settings, shutdown and security updates.

## Known issue

Dash and other full-view effects remain unresolved. This release does not claim to fix the effects compositor handoff.

No local test suites or game/headset sessions were run by the agent. Packaging and file comparisons establish delivery contents only; headset behavior remains unconfirmed.

## Install or update

Close Far Far West and its launcher. For a fresh install, extract the complete **FarFarVR-v0.4.13.zip** and launch **Far Far VR.exe**. Existing users can also use the launcher's Updates button.
