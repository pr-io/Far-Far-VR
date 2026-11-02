# Gun reticle repair - October 30, 2026

Only the gun crosshair/hit indicator projection is changed. Crosshair_Top, HitImage (hit X), and HitShield retain the original game-owned brushes, opacity, visibility and animation timing on one shared reticle canvas.

The old HUD module refused gun-following placement when UI_FollowView was false, restoring the original HUD-bound images. That created a dependency on information-HUD/menu projection state. The new read-only native VR_FarFarGunAim value projects the calibrated right-controller aim onto the actual compositor HUD quad. It supports head-following and fixed/stage-space surfaces without changing either surface. It uses the same controller rotation/position offsets as the existing tracked gun anchor. The HUD module consumes normalized coordinates for all three images together. A temporarily missing/stale surface or off-panel aim hides the aiming copies and originals rather than displaying an incorrect centered gameplay reticle. Game/VR menus and script cleanup retain normal original-widget restoration.

The getter cannot write settings, controls, the camera or gun. OverlayComponent only records its existing final quad pose/size/space for the getter; existing projection and layer contents are unchanged. The existing launcher/menu, cinematic, AA, effects, POI, mount, control and settings modules remain intact. No game configuration or saved bindings are rewritten by this repair.

Checks: actual Lua menu return with a fixed HUD, gun-source independence from old scene anchors, pending/off-panel suppression without centered fallback, shared crosshair/hit-X parent and native animation state, pure native calibrated-ray projection for following and fixed/moved/rotated HUDs. Full profile suite: 86 tests. Build and software checks do not constitute a wearer test. Live headset/combat confirmation remains pending.

Backup: [local UEVR workspace]/diagnostics/gun-reticle-repair-20261030/.
