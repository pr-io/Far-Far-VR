# Gun-following crosshair and hit X — October 30

The crosshair and hit X now use one tracked right-hand aim anchor instead of the
PlayerCameraManager actor's forward direction. The prior HUD implementation was
byte-identical in source, package and installed profile, but its game-camera
reference could not guarantee that the shared reticle followed the gun through
camera ownership changes. No specific camera-state trigger was reproduced live.

The new geometry-free SceneComponent is owned by the HUD module and receives
UEVR's calibrated right-hand pose (hand 1). The existing HMD reference (hand 2)
and the gun-hand reference are updated by the same per-view UObjectHook path.
A point 275 cm along that hand's forward ray is projected into the existing HUD.
Neither the actual weapon nor the game/HMD camera is moved or reconfigured.
Both private reference components are removed on HUD rebuild/reset.

Crosshair_Top, HitImage and HitShield still share one moving aim canvas. Their
original brush/material, visibility, hit opacity and animation transforms are
preserved, including their authored offsets relative to Crosshair_Top. No damage
numbers or counters were added or changed. Health, information HUD and POIs
retain their own existing presentation.

Only farfarvr_hud.lua changed among the seven shipped Lua modules. Controller
bindings, analog handling, mounted/emote behavior, comfort settings, graphics,
AA, cinema handoff, menu placement and backend binaries are unchanged.

Validation: 16 HUD tests and 71 complete profile tests pass. Checks include gun
movement with fixed head/camera, camera movement with fixed gun, HMD projection,
shared hit-X parent/animation state, correct hand assignments, source lifetime
recovery and cleanup without modifying the actual weapon/camera. These are
software checks; live gun/head independence and hit-X alignment still require
wearer confirmation.

Backups and verification: diagnostics/gun-reticle-20261030/.
