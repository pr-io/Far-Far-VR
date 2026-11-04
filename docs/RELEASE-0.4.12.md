# Far Far VR v0.4.12

## Crosshair and effects handoff

The v0.4.11 shader-container correction did not resolve the reported fresh-install symptom: crosshair and dash artwork remained on the ordinary HUD. This release changes the replacement paths themselves.

- Accept attached gameplay HUD widgets without an explicitly assigned player owner, including nested HUD widgets.
- With controller aiming selected, project crosshair and hit artwork from the selected tracked-hand aim anchor instead of the render camera, which can already contain the headset-view override.
- Explicitly enable the private effects component's offscreen rendering and wake deferred rendering with RequestRenderUpdate where supported. Retry offscreen warmup at a bounded interval until the compositor acknowledges it.
- Preserve original effects until the independent compositor acknowledges the replacement.
- Record the actual HUD discovery or effects handoff stage in the existing status files, rather than retaining only a title-camera or headset-inactive message.

The native engine, shader repair, controls, comfort, resolution, settings separation, shutdown and security changes are retained. No local tests or game/headset sessions were run by the agent. Live headset behavior remains to be confirmed.

## Install or update

Use the launcher's Updates button, or extract the complete **FarFarVR-v0.4.12.zip**. Close Far Far West before installing.
