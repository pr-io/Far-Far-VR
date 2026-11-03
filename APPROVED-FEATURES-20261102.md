# Approved-feature recovery - November 2, 2026

Local candidate: 0.4.2. User approved pushing the tested changes after confirming crosshair works and performance seems better. Fresh-install runtime delivery remains unresolved.

## Recovery reference

Antigravity conversation 311c60bd-3330-4841-b2dc-5b243fbf5eb1 contains two explicit user approvals today: step 356 (crosshair and controller/settings-isolation repairs) and step 727 (launch/crosshair result plus requested branding). The corresponding Git checkpoint is 2cdc314; the custom runtime present today was 2F30D9353B02435F0D34A6505D2FF494F4DACD20783F8D72CB408F9AEBE41558. Subsequent 18:29-19:50 HUD/aim/imported-attachment changes are not treated as approvals.

The unsuccessful 573B engine experiment from this chat was removed. This recovery uses the original 2F30 runtime rather than another experimental engine build. Gameplay behavior is retained from the accepted checkpoint. Periodic status writes have been removed from HUD, effects, comfort, objectives and mount modules. Comfort now reuses console-variable handles and refreshes them after failures. The lobby spawn recenter is restored separately; the in-game version is corrected to 0.4.2.

## Full retained feature inventory

| Approved feature | Source/evidence retained | Current acceptance |
|---|---|---|
| Separate VR and flat-game graphics, controller and user-settings stores | SettingsIsolation; 462f6c6; Antigravity bbed1344 user steps 1/278 | Lifecycle and preservation tests pass; no settings cache reset |
| Never restore desktop inputs while the game is running | 17f6b90 process guard and VR-save validation | Guard tests pass |
| Jump/dash/reload and custom VR controls | Bundled authoritative VR input save; SocialBindings; native controls policy | Existing files preserved; physical retest pending |
| Original animated crosshair, X and shield-hit artwork on private gun layer | HUD from 2cdc314, original 2F30 runtime | User confirmed crosshair and X working in recovered Live copy; shield still pending |
| Lobby spawn facing | One-shot recenter on new lobby pawn; no height or firing-ray changes | Regression test passes; wearer retest pending |
| Empty-hand aiming dot | Original independent tracked-right-hand route | Software checks pass |
| Minigun/lasso/non-reloadable weapons | Firing-camera classification does not depend on canBeReloaded or mandatory isDrawn | Added missing-property regression case passes; physical retest pending |
| Head-directed locomotion, separate right-hand aim | Profile VR_MovementOrientation=1 and VR_AimMethod=2 | Source/policy checks; wearer test pending |
| HMD-based POIs and saloon markers | Objectives module unchanged from accepted checkpoint | Projection tests pass; wearer test pending |
| Start tap / hold settings, right-stick map / hold ping | Native shortcut policy retained | Timing/chord checks pass |
| Both-stick emote tap / battle-cry hold, push-to-talk | Native policy, input Lua and social binding records retained | Tests pass; physical test pending |
| Horse and emote-only vertical look handling | Comfort/mount/input modules unchanged | Context checks pass; physical test pending |
| Notice/menu A confirmation and trigger laser selection | Original native pointer/input code retained | Pointer checks pass; physical test pending |
| Far Far VR focused menu, trigger interaction, controls popup/page | Original custom engine, menu Lua, latest controls artwork | Software checks pass; current wearer acceptance pending |
| Calibration and neutral camera offsets | Settings catalog/menu retained; no guessed gun-placement offsets | Software checks pass; wearer calibration remains user-specific |
| Stereo split/warp protection, cinematic surround and transition handling | Original custom engine and comfort module retained | Source/policy checks; wearer test pending |
| POV dash/damage and damage direction | Original effects module/custom engine retained | Coverage checks pass; current wearer test pending |
| Cached reflection helpers, bounded redraws, transparent-effects skips, optimized vtable checks | Today's approved Lua optimizations and original engine retained | Original native optimization retained; redundant Lua lookups/writes removed; latest wearer test: performance seemed better; no measured FPS claim |
| VR-only screen-effect/graphics safeguards | Render-fix Lua and VR settings isolation retained | Tests pass |
| User-requested 100% new-player resolution default | config and settings catalog default 1.0 | Existing user's selected scale is preserved |
| Current requested branding, channel/Discord/Flat2VR links and controls artwork | Latest launcher/assets retained | Source verified |
| Runtime auto-acquisition and verified updates without developer tools | Existing flow retained; manifest/runtime-exclusion checks strengthened | Fresh-install custom-runtime dependency still unresolved |

## Remaining public-release blocker

The configured official upstream download and the latest upstream build were downloaded/checked. Neither supplies this product's native reticle, effects or menu-session extensions. No matching upstream release was identified. The local copy has the custom engine; the runtime-separated player ZIP does not acquire it. Do not publish the candidate as download-and-go until this dependency is resolved.

The user explicitly selected the upstream-download distribution method; the player archive excludes runtime DLLs. The existing uploaded v0.4.1 asset was independently inspected and unexpectedly contains all four DLLs. No GitHub asset was changed in this recovery. No blanket license/TOS certification is claimed.

## Validation boundary

104 profile checks and all four launcher suites pass for the recovery source. The existing eight mod-native suites and 33 upstream policy checks passed; those are not runtime/headset proof. The earlier recovery test had working crosshair/X but poor performance and wrong spawn facing. After the isolated recenter correction and Lua cleanup, the latest user test confirmed the crosshair works and performance seems better. Spawn-facing and other unreported checklist items are not independently marked accepted. A further failure must stay recorded as a failure, not be overwritten by green software tests.

## Handoff and publication sequence

1. Test the uniquely versioned 0.4.2 local candidate from `H:\FarFarVR Live`. Keep the confirmed original custom runtime (2F30). No temporary probes, profiler, capture helper or background monitor is installed.
2. Record wearer results using `FINAL-TEST-20261102.md`. Latest wearer feedback reports improved performance; software checks do not override it.
3. User approved pushing the tested source. Publish the matching no-runtime-DLL asset only after resolving custom-runtime delivery and updating GitHub instructions/notes. Make the public download available without a token. Do not present the current upstream base engine as feature-complete.
4. With game and launcher closed, restore the flat game's original settings first. Inventory and remove the installed mod copies, mod-owned active profile/runtime/session files and launcher/runtime caches so the next launch cannot reuse them. Preserve the development repository, unrelated game data and player progress. Do this only after approval/publication; nothing has been deleted for the fresh-install test yet.
5. Download the actual published archive into a new folder. Verify extraction, automatic acquisition, Steam detection, launch, all custom features and VR/flat settings isolation without development tools or manual file copying.
6. Test the published update from an older compatible installation: check/stage in VR, exit/reopen, confirm the displayed version and all modules updated, retained settings and retired-attachment migration. Use a subsequent real release if the clean install already equals the newest release; do not fake update success by comparing identical versions.

## Current performance evidence

The user's latest gameplay log contains 1,487 DLSS "Another plugin already set FSceneViewFamily::SetTemporalUpscalerInterface()" errors. The same error is present in morning history, so it is not established as the new regression's cause. Native stereo renders eyes through separate transactions; a safe family/upscaler redesign has not been validated. No untested DLL change, AA change, graphics reduction or log suppression is included to claim a fix. The original optimized custom DLL remains installed. The Lua cleanup reduces avoidable work but has no measured FPS claim.

## Latest wearer acceptance

User: "i tested and the crosshair worked. and performance seemed better. push updates to github, then we'll delete everything locally and test fresh install". This authorizes the source push. It does not establish fresh-install functionality or authorize shipping an unverified replacement runtime.

## Fresh-install repair design

See [Engine/RUNTIME-DELIVERY.md](Engine/RUNTIME-DELIVERY.md) for the exact matching-upstream and independent-plugin routes, dependencies and release gates. Neither route is represented as completed. The tested local runtime is preserved.

## 0.4.3 automatic setup implementation

The technical runtime acquisition gap is now repaired locally with exact Windows MSDelta reconstruction of the tested 2F30 backend, not a gameplay rewrite or reduced-feature fallback. 29 runtime setup checks pass, including a real upstream network download into an empty folder, failure preservation, cancellation and retry. The existing Live copy remains unchanged. The local player ZIP includes the 1.5 MB delta and no complete runtime DLLs. Public distribution of that delta has not been license-cleared; no binary patch or 0.4.3 release asset has been pushed. See Engine/RUNTIME-DELIVERY.md.

## Publication authorization

After discussing unresolved binary-patch rights, the user explicitly accepted proceeding and requested upstream patch references and direct patch links on the GitHub page. Publish the verified 0.4.3 package and its delta with attribution, hashes and source links, without claiming a legal certification. No engine rewrite or gameplay change is included. Fresh-install and live-update wearer tests remain pending.
