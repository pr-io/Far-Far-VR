# Third-Party Notices, Licensing, and Attribution

**Far Far VR** is developed and published by **PR_VR** ([YouTube: @PR_VR](https://www.youtube.com/@PR_VR)).

---

## UEVR Framework Attribution & Runtime Separation

- **Original Project:** [praydog/UEVR](https://github.com/praydog/UEVR), created by **praydog** and contributors.
- **Attribution:** Powered by UEVR created by praydog. We extend sincere credit and gratitude to praydog and the Flatscreen to VR (Flat2VR) modding community for developing this universal VR framework.
- **Distribution Separation:** **The 0.4.2 candidate builder excludes third-party runtime binaries (`UEVRBackend.dll`, `LuaVR.dll`, `openxr_loader.dll`, `openvr_api.dll`)**. Historical release contents require separate inspection.
- **Auto-Fetch Mechanism:** On first setup, the Far Far VR launcher downloads the verified, compatible public UEVR runtime archive directly from GitHub into the user's local directory:
  - Compatible public release: [joeyhodge/UEVR `102626_FFR_SWGR_Patch`](https://github.com/joeyhodge/UEVR/releases/tag/102626_FFR_SWGR_Patch)
  - Public archive SHA-256: `FDF325C62BA43BC1DA98ED5F453633FA106CAEF0DF9CB8121566F7AA138F6D55`
  - Base backend SHA-256: `FEFF6417359F6E36ABAC6577598A86B96D485A023A850F8614043AA0A508C8CB`
  - Current Far Far VR backend SHA-256: `2F30D9353B02435F0D34A6505D2FF494F4DACD20783F8D72CB408F9AEBE41558`
  - Compatible LuaVR.dll SHA-256: `0431946710BD006C221C89863C1D49ADF80F8847817EFCE198D549F5B1B64B0A`
  - Compatible openvr_api.dll SHA-256: `845D4B947CB5DD2DE2964F6C71536D03366939E4798E574FDD20838D318B540D`
  - Compatible openxr_loader.dll SHA-256: `3BB4EB190024F92EECF4A0BF33669B5D0B27571DDF4C36A33C58DE199EE536C1`
  - Pinned UEVR source revision: `963b157573eaef20528b602d1d9790b8105819b3`
  - Pinned UESDK revision: `811b01498b61e6a0246615b4f2b4fb4f4067fb64`

The pinned public base engine is not equivalent to the customized local backend: it lacks the native reticle/effects/menu-session extensions. A verified distribution path for those extensions remains a release blocker. The existing uploaded v0.4.1 asset was inspected and contains runtime DLLs despite its published separation notice. This candidate package excludes them; no blanket license or TOS certification is claimed.

The pinned upstream engine license says "All rights reserved." See the [actual engine license](https://github.com/joeyhodge/UEVR/blob/963b157573eaef20528b602d1d9790b8105819b3/LICENSE); source visibility and attribution do not themselves establish redistribution rights. The game developer [encourages modding](https://steamcommunity.com/app/3124540/discussions/0/837249163025472585/); that statement is not permission to redistribute third-party engine binaries.

Any source changes maintained by Far Far VR are kept as patches against the pinned revisions (see [Engine/README.md](Engine/README.md)).

---

## Original Works by PR_VR

All launcher code, scripts, configurations, documentation, and design assets authored specifically for Far Far VR belong to **PR_VR**:

1. **Launcher & Injector Application (`Far Far VR.exe`):**
   - Game detection, automatic setup, session isolation, and native thread injection.
   - Live UEVR session link, auto-fetch runtime installer, and updater.
2. **Game-Specific Lua Modules (`Profile/scripts/`):**
   - Independent gun-following compositor layer & weapon reticle (`farfarvr_hud.lua`).
   - Cinematic letterboxing, camera stabilization, and mount comfort transitions (`farfarvr_comfort.lua`, `farfarvr_mount.lua`).
   - In-game VR settings UI, calibration, and live updates panel (`farfarvr_menu.lua`, `farfarvr_settings.lua`).
   - Objective markers, POV damage feedback, and cursor interaction (`farfarvr_objectives.lua`, `farfarvr_effects.lua`).
   - CVar rendering fixes and temporal stabilization (`farfarvr_render_fix.lua`).
3. **VR Profile & Input Layout:**
   - Far Far West customized Touch controller bindings (`Profile/config.txt`, `Profile/_interaction_profiles_oculus_touch_controller.json`).

---

## Intellectual Property & Game Disclaimers

- **Far Far West & Developers:** Far Far VR is an unofficial community mod. Far Far VR has no affiliation with, and is not endorsed by or connected to, the developers of Far Far West, Epic Games, or Unreal Engine.
- **No Copyrighted Game Assets:** No game binaries, game assets, audio, textures, or player-progress saves are included in this distribution. Players must purchase and install the original game on Steam.
