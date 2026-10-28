# Far Far VR direction

## 1. Portable foundation — implemented locally

Retain the accepted launch sequence, detect Steam libraries, allow manual paths, validate the pinned runtime, seed a missing profile, preserve existing player bindings, and provide a small set of relevant options. Keep the source project separate from runtime binaries and prepare a minimal local package.

Before a public release: resolve component redistribution permissions and profile authorship; test on a second machine without the current AppData; add a clean Windows build workflow; decide a license for original project code. The source project is prepared for a private repository; no installable release is published.

## 2. Focused in-game menu — compiled preview, desktop checks passed

Use a maintained UEVR source fork pinned to the compatibility revision, retaining game-required engine fixes. Start by tracing controller/Insert menu handling and settings persistence. Replace the user-facing universal menu with Far Far VR comfort, turning, aim, controller alignment, HUD and performance controls. Share the same settings and validation rules with the launcher. Keep recovery logs available without exposing unrelated game compatibility flags.

Implemented locally: a compiled UI-only patch routes the existing menu directly to the Far Far VR panel. Launcher and menu share a catalog of 22 controls, live value verification, session calibration, save requests and revert. Both interfaces now use a dark, warm-accent design with grouped settings. Desktop testing verified menu open/close, calibration editing/reset/save, all four menu pages, missing-headset gating and preservation of corrected Touch bindings. Thirty-three upstream offline checks and twelve Lua 5.4 behavior tests pass. The rendering and hook source and original shader bytecode remain unchanged. The accepted backend is preserved; a separate preview shortcut/runtime is available. Controller macro, physical height, headset projection, gameplay and frame-time acceptance remain pending. See `Engine/README.md`.

Do not remove engine code merely because its UI option is hidden. Establish reproducible builds and compare behavior/performance before trimming shared code. Keep the accepted runtime available for rollback.

Acceptance: menu opens and closes with the existing macro; settings survive restart; no unrelated compatibility settings appear; focus/input returns correctly to gameplay; launcher edits do not conflict with live menu saves.

## 3. Native-feeling gameplay — incremental headset-tested work

Prioritize stable stereo and frame timing, weapon/hand alignment, aiming and recoil, roomscale collision, seated/standing calibration, locomotion comfort, readable HUD, menus/cutscenes, controller prompts and haptics. Handle each as a separate change with headset checks and rollback.

Add UE4SS only if a concrete gameplay feature needs it and verify it against the installed game build. The linked Nexus framework targets UE5.7 and has not been validated against the current local build.

## Release acceptance

Test fresh install, moved game library, missing runtime/profile, existing customized profile, start from SteamVR/desktop, already running game, cancellation, game updates, restart, and incorrect runtime. Record game build/runtime hashes/headset/OpenXR runtime. Perform wearer checks for title screen, gameplay, weapons, interaction, menu macro, pause/death, level changes and frame timing. Build success and engine hook confirmation alone are not headset acceptance.
