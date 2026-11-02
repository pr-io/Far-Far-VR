# Independent gun crosshair and hit X - October 30, 2026

Crosshair_Top, HitImage (hit X) and HitShield now share their own private 256x256 widget renderer and OpenXR swapchain 0xff01. The clones are NOT children of UI_Player or its information HUD. The original sources stay game-owned for animation/brush updates and are individually hidden through the existing wrappers during VR gameplay. Health and all other HUD widgets retain their native parents and positions.

The compositor positions the private reticle layer directly 275 world centimetres along calibrated right-hand aim, each submitted frame, facing the headset for legibility. Its size/depth are independent of UI_Distance, UI_Size, UI_FollowView, the game's HUD canvas geometry and menu anchoring. This replaces the previous reticle-to-information-HUD projection. Both the crosshair and X use the same target/layer, preserving native animation state and relative image alignment.

The offscreen component has no scene main/depth pass, shadows, ray-tracing visibility, collision or hardware input. It is not a world-space mesh. No camera/locomotion/weapon transforms, bindings, existing menu placement, graphics settings, POIs or fullscreen effects are changed. Native source/owner/texture validation and menu/cinema guards apply to the new layer. Script teardown restores original source ownership and destroys only its private renderer.

Checks: 80 current Lua profile tests (legacy HUD-projection tests replaced with private-layer ownership/lifecycle tests), standalone gun-layer pose/scale checks, 33 upstream native checks, backend and launcher builds. Tests explicitly reject reads of information-HUD settings and assert that crosshair/hit-X clones belong only to the private host. Existing seven other Lua modules, launcher flow, configuration and saved bindings are unchanged. Headset appearance, gun alignment, animation and performance remain pending wearer acceptance.

Backup: H:/UEVR/diagnostics/independent-gun-layer-20261030/.
