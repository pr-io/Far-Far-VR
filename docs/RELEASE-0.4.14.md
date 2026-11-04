# Far Far VR v0.4.14

## Separate crosshair and effects rendering

The v0.4.13 wearer report confirmed that gun tracking worked, but the crosshair disappeared outside the information HUD texture. Moving artwork inside that texture could not remove its clipping boundary.

- Restore a dedicated 256x256 crosshair compositor texture, separate from the information HUD. Crosshair, hit X and shield-hit artwork share this private texture.
- Retain the firing-camera calculation confirmed in v0.4.13. Submit its headset-local target directly in OpenXR view space, without projecting into the information HUD or changing HUD settings.
- Initialize the SDK render-resource lookup before reading private widget GPU textures. The prior crosshair and effects paths could read the lookup before it was initialized; the Far Far scene route can supply native textures directly without establishing this optional UTexture lookup.
- Keep the existing full-view dash/damage/healing/shield compositor and correct its shared texture prerequisite. Resource discovery retries are bounded.
- Retain cumulative controls, objectives, riding, comfort, resolution, launcher, settings separation, shutdown and security changes.

No local test suites or game/headset sessions were run by the agent. Build and package inspections do not establish wearer acceptance. Crosshair visibility beyond the HUD and full-view effects remain unconfirmed in the headset.

## Install or update

Close Far Far West and the launcher. Extract the complete **FarFarVR-v0.4.14.zip** for a fresh install, or use the launcher's Updates button. First setup reconstructs the new matching native backend; copying only the Lua script is insufficient.
