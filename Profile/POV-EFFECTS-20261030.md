# POV event effects - October 30, 2026

Dash, shot, heal and shield flashes from UI_ScreenFlashes, UI_Player.HealDamageScreen, and UI_DamageScreen incoming-hit direction artwork now have an independent POV effects layer. Native artwork, materials and animation objects remain owned by the game. Health/info HUD, gun crosshair, outgoing hit X, POIs, pointer and all controls retain their existing modules and settings.

A private transparent 1024x1024 offscreen widget renders only the selected effect images. It has no scene main/depth pass, shadow, ray-tracing visibility, collision or hardware input. The compositor fits a head-relative quad separately to each eye's actual field of view, including asymmetric eyes and a 4% margin. Effects cover the scene; the existing information HUD stays above them. No camera, world scale, HUD distance, AA selection or graphics settings are changed.

Incoming damage direction retains the original widget proportions and animation. Read-only post-damage notifications capture the native camera heading at creation; the mirrored indicator corrects that heading against tracked HMD yaw, independent of subsequent gun/controller aim.

Original painting is suppressed only while this exact private renderer has a fresh successful native acknowledgement. Missing APIs, unsupported runtime, inactive headset, menus, cinema, or copy/submission failures restore original effects. Cleanup restores the original image parents/layout. The bridge is runtime-only and does not add saved settings.

Validation: production backend build; 33 upstream native checks; 82 Lua profile tests; separate asymmetric-FOV, coverage, distance-scaling and invalid-input checks. These are software checks, not live engine or wearer acceptance. In-headset appearance, damage direction, transition behavior and GPU cost remain unverified. The offscreen effect render/copy is capped at 60 Hz and skips inactive copies after warmup (materials with internal alpha can still incur active rendering cost).

Installed module: scripts/farfarvr_effects.lua. Runtime status: farfarvr-effects-status.txt in the installed game profile. Source/rollback evidence: H:/UEVR/diagnostics/pov-effects-20261030/. Close the game before running Restore-Previous-Runtime.ps1 there to restore the previous runtime and launchers and disable this new module.
