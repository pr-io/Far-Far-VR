# Original animated gun crosshair

**2026-10-26 update:** the v8 implementation supersedes the v7 description below.
The crosshair now has a separate 2 m world aim surface with 2x raster sampling;
health/corners remain on the accepted distant compositor HUD. Marker hosting no
longer depends on UI_Player and includes saloon marker classes. See
[FINAL-TOUCHES.md](FINAL-TOUCHES.md) for the current implementation, validation
limits and required headset checks. The historical v7 wearer acceptance below
does not establish v8 rendering, sharpness, performance or alignment.

## Historical v7 implementation and evidence

`scripts/farfarvr_hud.lua` draws a live copy of the game's original Crosshair_Top, HitImage and HitShield on an independent aim canvas in the sharp compositor HUD. The target sits 275 cm along the camera actor's forward direction from its location, matching the exact camera properties used by F_TraceBullet before spread. Its position is projected from the tracked HMD onto UEVR's actual UI quad; moving the gun moves this aim canvas independently of health and corner information.

The original widgets remain in UI_Player's tree with their original canvas layout and animation bindings. Full-size wrappers hide their viewport paint while the world copy is active. The copy shares the original brush resources, including the weapon's dynamic material, and mirrors color, opacity, transform, visibility, size and animated slot displacement at up to 60 Hz.

Health, compass, corner information and messages retain the accepted sharp head-following HUD. The title background camera keeps its existing 60 cm elevation adjustment. Menus and controller bindings are handled separately.

There is no crosshair world widget, extra render target, custom crosshair geometry or shared material change. Aim now updates in on_pre_calculate_stereo_view_offset, after UObjectHook has updated its HMD attachment for that view. It reads the camera actor properties directly without invoking F_TraceBullet or a physics query. This replaces the previous 20 Hz aim updates against a previous-view HMD transform, which drifted during movement. Original visual state is copied at up to 60 Hz.

`scripts/farfarvr_objectives.lua` takes the original UI_PingObjective widgets out of the controller-projected screen widget components and places them on a separate canvas using their original world anchors and the tracked HMD. This adds no render targets. Reset restores the original widgets to their components. The wearer confirmed the objective icons now work correctly.

Validation: eight crosshair tests and three HMD projection tests pass, including fresh-view HMD rotation and per-view camera updates without physics queries. The earlier native animation check confirmed shared weapon material and exact copied Hitmarker opacity (diagnostics/hud-stock-crosshair-animation-v4.txt). The v7 callback runs in the live game. The wearer confirmed v7 looks correct when testing gun movement with a still head and head movement with a still gun. The wearer also confirmed the objective icons track correctly. These checks do not establish frame-time cost. Both layers require the accepted view-following flat UI quad; icons or targets beyond that quad's visible bounds are outside its drawable area.
