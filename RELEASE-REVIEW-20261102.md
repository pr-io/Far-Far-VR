# Superseded failed candidate

The 573B engine candidate described below failed the user's headset test. It was removed. See APPROVED-FEATURES-20261102.md for the recovery using today's accepted 2F30 engine/HUD checkpoint.

# Release review - November 2, 2026

Status: local test build installed; public first-install runtime dependency unresolved. Headset acceptance pending the user's test.

## Local test copy

`H:\FarFarVR Live\Far Far VR.exe`

Backend SHA-256: `573B521A3B7AEF818B07D6FF8E3558B1E0F116339DBFB988865CA8C32ED1E862`.
Original LuaVR/OpenXR/OpenVR companion binaries retained. Source, Live and installed Lua scripts match by SHA-256. No temporary profiler/probe scripts are installed and no build/monitor process remains running.

## Repairs

- Crosshair/X and face-effects textures use the existing validated UE5.8 owned-resource path instead of process-global legacy UTexture resource offsets. Two bounded caches revalidate the exact owner, render-thread and native-resource chain each frame; discovery retries are capped. No new per-frame logging.
- Periodic HUD/effects diagnostic status-file writes removed. Existing transparent-effects work avoidance and unchanged-reticle-layout optimization retained.
- Reverted the November 2 19:50 commit's imported October 23 gun attachment/0.54 scale overrides. The four changed aim/roomscale/startup defaults now match that commit's parent. No guessed gun-placement offsets remain. Synchronization and framerate defaults remain today's earlier values (1 and false).
- Game-folder OpenXR loader cleanup now removes only an unchanged DLL deployed by this launcher, using an ownership marker. Existing game files are preserved.
- Engine patch converted from UTF-16 to UTF-8; preparation now copies required product headers and applies the previously omitted console patch. Pinned revisions retained.
- Player package builder restored to the documented upstream-download route. Builder and updater reject bundled UEVR runtime DLLs; update manifests must cover required files and cannot contain duplicate entries.

## Validation

Backend and launcher Release builds passed. 102 profile checks, 33 upstream native checks, eight mod-specific native suites and four launcher suites passed. Source patch reverse-application checks and whitespace checks passed. The player ZIP passes manifest validation and contains zero third-party runtime DLLs.
These are software/build checks. They do not establish headset appearance, gameplay performance, comfort, or resolution of the historical November 1 crash.

## Release blocker: fresh-install engine acquisition

The existing uploaded GitHub v0.4.1 ZIP was inspected directly. It contains UEVRBackend.dll, LuaVR.dll, openxr_loader.dll and openvr_api.dll, contrary to its release notes. The locally rebuilt player archive excludes them. Nothing was uploaded or published in this review.

The configured upstream archive was downloaded and its pinned SHA-256 verified. Its backend is FEFF6417359F6E36ABAC6577598A86B96D485A023A850F8614043AA0A508C8CB and contains none of the reticle, effects or menu-session bridges required by this product. The latest upstream release (110326StupidAvowed) and current upstream branch were checked too; those product bridges are absent. No matching custom-runtime upstream download was identified.

The local test copy includes the custom engine and is suitable for the user's headset test. A runtime-separated public ZIP using the current automatic-download URL is not a complete functioning installation. Resolve that dependency before publishing; do not describe it as release-ready.

Sources: repository history through 0950d64; actual GitHub release asset; configured and latest upstream binaries; chats “Fix final VR gameplay issues” and “Investigate game crash”.

## Terms and attribution

The existing runtime-download distribution method is retained. No third-party runtime binary is included in the player ZIP. This is not a blanket legal/TOS certification. The actual UEVR LICENSE reserves rights; the official game developer encourages modding. No claim is made that attribution alone grants redistribution rights.
- https://raw.githubusercontent.com/praydog/UEVR/master/LICENSE
- https://steamcommunity.com/app/3124540/discussions/0/837249163025472585/

Backups and check output: `H:\FarFarVR\diagnostics\release-before-final` and `release-*.txt`.
