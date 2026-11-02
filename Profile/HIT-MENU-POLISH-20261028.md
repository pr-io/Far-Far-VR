# Hit marker and settings-menu polish — October 28

Baseline: wearer-tested backend
`79430B98FC2ED29DA30E355920DAE5500ABA6ECF5B86440382CE7AC1246AD006`.
The wearer reports that most cinematic flicker is gone, FXAA is fine, and
NVIDIA DLSS looks best so far. TAA improved but has occasional distortion.
TSR was not tested. One slight flash was observed from the start menu into play.

Changes in this batch:

- The original `HitImage` and `HitShield` images now share the moving aim canvas
  with `Crosshair_Top`. Their live original widgets remain the animation/game
  callback targets; copies preserve brush, opacity, transform and visibility.
  HUD lifetime recovery remains in place.
- For this game in OpenXR, the framework/UEVR settings quad captures the raw HMD
  position and horizontal facing when opened, then keeps that stage pose.
  Controller aim offsets no longer rotate the menu. Controller-ray pointing
  remains available. Closing/resetting clears the anchor for the next opening.
- The existing TAA-only spatial-output guard also checks after the game tick.
  A game settings/effect write during Tick can no longer leave temporal
  upsampling enabled until the following frame. Original AA selection and
  non-TAA settings are preserved.

The accepted cinematic renderer and 150 ms handoff policy are unchanged. No
speculative startup-camera or tracking-origin adjustment was added for the
single reported menu-to-game flash. That case remains unresolved. The TAA timing
change targets a possible overwrite window; it does not establish that every
reported distortion is eliminated. Physical confirmation of both is pending.

The POI and mounted camera scripts, controller gesture policy, native CVar ABI
repair, head-directed movement and manual launcher Start are preserved.

Validation: 63 Lua/profile tests pass. A new test executes the actual HUD builder
and verifies all three images share one aim parent and retain hit animation
opacity/transforms. Another test simulates a during-Tick CVar overwrite and
verifies the post-Tick guard repairs it without changing FXAA/TSR selection.
Native build/check results are recorded in
`diagnostics/hit-menu-polish-20261028/`. A build or simulated test is not an HMD
visual-acceptance result.

Installed build SHA256: `84F2E23B80DB63959D6619EB876E8CD3C66A59EE624E7CAF289008BC76F3BB20`.
Release engine/launcher builds and all 33 native checks passed.
