# Fresh-install delivery repair

Status: proposed implementation, not a completed installer fix. The accepted local 0.4.2 build remains intact.

## Why the current download cannot reproduce the test

The tested local backend has SHA-256 `2F30D9353B02435F0D34A6505D2FF494F4DACD20783F8D72CB408F9AEBE41558`. The launcher's pinned public upstream archive contains backend `FEFF6417359F6E36ABAC6577598A86B96D485A023A850F8614043AA0A508C8CB`. That base engine lacks the custom reticle/effects bridge and menu/session extensions. Changing hashes to accept it does not supply those features. The launcher now rejects that known-incomplete base as a complete custom runtime.

## Route preserving the exact tested engine

A matching runtime release provided through an authorized upstream distribution channel would allow the launcher to download and hash-check it directly. This is the smallest implementation change and preserves the tested binary. No matching public download has been found. Publishing our customized engine somewhere else is not itself a solution to its distribution rights.

## Independent-plugin route

UEVR explicitly supports loading a game-specific plugin from `%APPDATA%/UnrealVRMod/FarFarWest-Win64-Shipping/plugins/`. Its `include/uevr/API.h` carries its own MIT license, separate from the engine's all-rights-reserved LICENSE. This provides a supported interface for independently authored plugin code; preserve the API's MIT notice. It does not grant a license to copy engine internals into a plugin.

Package independently authored `FarFarVR.dll`, the existing launcher, profile and Lua modules. Continue downloading an unmodified, pinned upstream engine directly from upstream. The launcher installs the plugin automatically before game launch; no player compiler, source checkout or development tools. Avoid a per-frame file bridge, additional background service, binary-delta distribution, or installation-time engine build.

### Required port and acceptance scope

| Current native customization | Required plugin work |
| --- | --- |
| Private gun reticle and hit-X/shield compositor layer | Independent OpenXR layer/swapchain ownership; preserve artwork, depth, firing-camera ray, per-view timing, acknowledgement and cleanup |
| Full-face dash/damage/directional artwork | Independent effects layer; preserve visibility gating, full resolution and fade-out copy suppression |
| Controller taps/holds/chords and cursor pointer | Adapt the existing product policy headers through public XInput and engine/view callbacks; verify callback ordering against upstream controller conversion |
| Focused menu, laser/trigger input and controls artwork | Own UI integration through supported plugin/Lua interfaces; native ImGui modifications are not supplied by the base engine |
| Launcher session/readiness/menu commands | Replace custom core session handling with a plugin-owned handshake; gate launch success on plugin readiness, not only engine injection |
| Calibration, flat/VR settings isolation, game Lua modules | Retain the accepted existing launcher/Lua behavior; change only the native bridge transport as required |
| Engine-level console ABI, cinematic/stereo fixes and memory-query optimization | Determine what the selected upstream runtime actually contains. Public plugin callbacks do not automatically replace these internal patches. Any remaining core dependency must be addressed upstream or independently implemented and revalidated before claiming parity |

Use the plugin custom-event callback for Lua-to-native messages and the Lua state/public API for acknowledgement. Audit availability and thread ordering before implementing the compositor, input or UI adapters. A plugin skeleton or successful build alone is not feature parity.

### Installer, update and release gates

1. Build the independently authored plugin on the publisher's machine/CI. Retain required dependency notices and exclude engine binaries from the player ZIP.
2. Pin the public engine archive and plugin hashes as one tested compatibility pair. Stage downloads and validate every required file before promoting the runtime; preserve an existing valid installation on failure.
3. Install the plugin and all modules through the launcher. Update installation must synchronize the plugin too, including migration from the custom-engine build; current named-Lua synchronization alone is insufficient.
4. Verify engine version, plugin ABI and complete plugin readiness before reporting launch success. Missing reticle/effects/menu support must produce a clear failure rather than silently returning to a HUD fallback.
5. Re-run profile, native policy and launcher checks, then repeat wearer checks for every ported feature and performance. Do not replace the accepted local runtime during development.
6. Publish the matching package only after parity and distribution review. Remove installed copies/caches only after the reproducible public download exists; then test a genuine empty-cache install and an older-version-to-newer-version update.

## Primary references

- [UEVR plugin installation and lifecycle](https://docs.uevr.io/plugins/getting_started.html)
- [MIT-licensed API header at the pinned source revision](https://github.com/joeyhodge/UEVR/blob/963b157573eaef20528b602d1d9790b8105819b3/include/uevr/API.h)
- [Separate engine license](https://github.com/joeyhodge/UEVR/blob/963b157573eaef20528b602d1d9790b8105819b3/LICENSE)

This design avoids bundling the engine. It is not a blanket certification of every third-party term, and it is not represented as implemented or headset-tested.
