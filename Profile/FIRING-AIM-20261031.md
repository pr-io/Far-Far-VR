# Crosshair aim correction - October 31, 2026

The private crosshair/hit-X layer remains separate from the information HUD. Its position now comes from the real game firing camera after each stereo-view attachment update, restoring the aim reference used by the wearer-accepted v7 implementation. The empty-hand dot uses its own tracked right-hand anchor and does not require a firearm.

The script transports the aim point in headset-local OpenXR coordinates to the native compositor. The native quad reconstructs its stage position from that point. It no longer guesses the gun shot ray from raw controller origin/rotation. Camera offsets, locomotion, controller bindings, horse/emote views, POIs, menus, fullscreen effects, and graphics selections are not written by this change.

Crosshair_Top, HitImage and HitShield still share the same private artwork host and target. The game retains ownership of original material changes and hit-animation timing. No physics traces are used. Menu/cinema suspension and cleanup clear the target; malformed targets are rejected by native composition.

Backend: F9D7BA155970860C90F7EB4CCF5C3C9E584A715DDF70C1A32B33EE2B6CA05DFF. The launcher's backend pin is updated; its manual Start flow is unchanged.

Validation: 85 profile checks, standalone native firing-target/head-motion/parser checks, 33 upstream native checks, backend and launcher builds. These checks do not establish wearer acceptance of gun/head motion. See diagnostics/firing-aim-repair-20261031 for the in-game check result and backup.
