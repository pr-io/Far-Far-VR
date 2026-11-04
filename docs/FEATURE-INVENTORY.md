# Cumulative feature inventory

This is a preservation inventory for changes across chats. Presence in source or in a package does not establish live headset behavior.

| Area | Features to retain | Owning implementation |
| --- | --- | --- |
| Aiming and crosshair | Tracked-hand weapon aim; firearm/minigun and empty-hand handling; original animated crosshair, hit X and shield-hit artwork on an independent compositor texture; per-view firing-ray targeting | `Profile/scripts/farfarvr_hud.lua`, native reticle and pointer policies |
| Effects | Independent face-covering dash, shooting, healing, shield and damage effects; directional damage relative to head orientation; original-effects fallback | `Profile/scripts/farfarvr_effects.lua`, native effects compositor |
| Objectives | Head-projected world markers, ownerless lobby markers and stale-marker cleanup | `Profile/scripts/farfarvr_objectives.lua` |
| Controls | Start tap/game menu and hold/VR menu; map/ping tap/hold; weapon switch/push-to-talk tap/hold; two-stick emote/battle-cry actions; A confirmation; cursor pointing/trigger selection and stick navigation takeover | `Engine/FarFarWestControls.hpp`, native input/pointer handling, `Launcher/SocialBindings.cs`, profile action/binding files |
| Riding and emotes | Horse orbit and camera collision; mounted camera transitions; emote pitch isolation without accidental melee/utility inputs; restoration on exit/dismount | `Profile/scripts/farfarvr_mount.lua`, `farfarvr_input.lua`, comfort module |
| Cutscenes and transitions | Authored cinematic/loading cameras, loading-train behavior, stereo correction restoration, facing preservation across recovery | Comfort/HUD modules and native cinema/rendering policies |
| Comfort and performance | Saved preferences separate from temporary menu/cinematic/riding overrides; idle-headset handling; AA scaling correction; scoped stereo/post-processing fixes; retained native memory-probe and render-thread work | Comfort/render-fix modules, native policies and cumulative engine patch |
| Menu and calibration | Level headset panel with opening direction retained; controls guide; explicit recenter; height/position calibration; camera/world/HUD adjustments; saved preferences | Menu/comfort modules and native menu handling |
| Display and resolution | Resolution staged until Save; Revert discards unsaved scale; live texture/swapchain rebuild and matching stereo-pair handling; mirror and frame controls | Menu module, native resolution policies and D3D12 handling |
| Settings separation | Independent VR and flat-game graphics/input/settings files; original desktop snapshots; crash/interruption recovery; no progress-save replacement | `Launcher/SettingsIsolation.cs`, profile settings templates |
| Launch and shutdown | Verified upstream reconstruction and current backend pins; no automatic game start from Steam's `--start`; startup readiness; cancellation/failure exit monitoring; desktop restoration; scoped shutdown helpers | Launcher setup/runtime/session/exit code and native shutdown hook |
| Updates and distribution | Desktop and headset update controls; cancel and transfer timeouts; stable-release selection; repository-scoped download/authentication; integrity manifests; transactional install/rollback recovery; private-file exclusion | Launcher updater/apply helper, menu bridge, package builder |
| Privacy and security | Sanitized diagnostic roots with valid graphics containers; credential protections; private vulnerability reporting; dependency/security scans; published history cleanup | Runtime preparation tools, security policy, repository settings |

Player delivery must include every gameplay module, current bindings, settings templates, controls artwork, launcher, matching delta and integrity manifest. A narrow update must not discard another row of this inventory.
