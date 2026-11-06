# Accepted early-access baseline: v0.4.16

On November 4, 2026, the user reported: "okay finally happy with its current early access state. make note of everything so we don't keep going back to these things".

This is user acceptance of the current early-access state, not a claim of exhaustive testing or perfection. Treat earlier reports that crosshair and dash remain broken as superseded by this acceptance unless the user reports a new regression. Do not resume old repair experiments automatically.

## Exact baseline

- Release: https://github.com/pr-io/Far-Far-VR/releases/tag/v0.4.16
- Runtime/source baseline commit: 538cb647b9e1ebc43484c0cf6904d180806abe09 (v0.4.16).
- Native backend SHA-256: 1B6AC545B746190F4502C346897A36A3002C4BB61FD994521C70579F21B47A74.
- Runtime delta SHA-256: 6A4E0FF8FC42E94B0FD5F8C1FBC3C41FFDA846C5B621F3318C89AD65FC41C43D.
- Reconstructed native size: 15,811,072 bytes. Launcher pins and delta must remain paired.

## Preserve the accepted rendering paths

- Crosshair, hit X and shield-hit artwork use a private 256x256 compositor texture. They must not be moved back into the information HUD texture: its boundary clips them.
- Drawn weapons use PlayerCameraManager location and forward direction, sampled per stereo view, matching the game's firing camera. Do not substitute a raw hand ray for firearm targeting. Empty hands retain tracked-hand targeting.
- Crosshair target is converted to headset-local metres and submitted in OpenXR view space, independently of information-HUD size, placement and clipping.
- Effects retain their independent 1024x1024 texture and per-eye full-view layers for dash, damage, healing, shooting and shields, with directional damage handling and original-effects fallback.
- Optional widget GPU textures require SDK UTexture render-resource discovery before reading their RHI resources. Keep the bounded initialization/retry repair in both native texture paths.
- Preserve original animated brushes/materials and hit artwork; do not replace animations with approximations.
- Controls artwork must exist at Profile/textures/farfarvr_controls.jpg. A docs-only image does not satisfy the native lookup. Existing-profile synchronization must copy JPG as well as PNG.

## Preserve the cumulative product

The complete feature preservation map is [FEATURE-INVENTORY.md](FEATURE-INVENTORY.md). It includes tracked aiming with head-relative on-foot locomotion; original tap/hold controls; emote/A confirmation/battle cry; horse orbit and emote camera isolation; objectives; cinematic/loading transitions; stereo and AA corrections; level/world-anchored settings panel; calibration and saved preferences; resolution Save/Revert and live rebuild; separate VR/desktop settings; startup cancellation/failure cleanup; shutdown recovery; desktop/headset updates and transfer timeouts; privacy/security protections.

The launcher must verify the actual requested DLL after loading it, whether OpenXR or the backend. Keep the v0.4.15 module-path verification correction.

## History and future changes

- The November 3 change 4871759 removed the independent crosshair layer and profile controls images. The independent layer was restored/repaired in aea87bc; the missing controls asset and JPG synchronization were restored in 538cb64. Do not repeat these removals.
- Old imported attachment files were retired deliberately by exact content hash; Setup preserves unknown/user-edited attachments. Do not restore those files wholesale as an aiming repair.
- Keep privacy-safe binary path handling for both slash styles and valid shader-container digests. Executable shader programs live outside .text; arbitrary binary string replacement can break rendering.
- Start future work from current cumulative main. Make scoped changes, inspect actual code and GitHub history, and preserve unrelated modules, assets, configurations and native policies.
- User forbids local test suites and game/headset launches unless explicitly requested later. Builds and static inspection are permitted, but must not be presented as wearer validation. Existing GitHub automation remains enabled.
- Deliver requested fixes through complete published releases when publication is requested; a local edit or source push alone is not a downloadable update.
- Do not create another release merely to record these notes.

## Accepted performance update: v0.4.18

On November 6, 2026, the player tested the complete local candidate and reported significantly better performance, then authorized a new release. v0.4.18 promotes that candidate without changing its native backend or gameplay behavior. This supplements the cumulative v0.4.16 acceptance above.

- Backend SHA-256: `74A1AF0EAB3215DEF7CC08D6C492E75A7DFE4FA2E41EBAA276818D3C9E8D0493`.
- Runtime delta SHA-256: `BACC64D331D034C58D7243A7E5584464431BDC5F3313F9F250395E1145BC6100`.
- Reconstructed native size: 15,816,192 bytes.
- Retain the exact-game native secondary-family ownership repair and bounded empty-effects warmup retries alongside all prior rendering, controls and settings behavior.
- Diagnostics are optional and disabled by default. Performance timing, detailed frame trace and log detail can be changed in the Far Far VR menu; the off button disables capture and restores normal warnings/errors. Do not enable diagnostics automatically.
- Player-reported improvement is not a guarantee of a particular frame rate or universal resolution of every long-session/audio issue.
