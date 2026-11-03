# Third-party notices

Far Far VR is maintained by **PR_VR** ([YouTube](https://www.youtube.com/@PR_VR)). It is an unofficial community mod, with no affiliation with or endorsement by the Far Far West developers, Epic Games, praydog or joeyhodge.

## UEVR and runtime patch

- **UEVR:** created by [praydog and contributors](https://github.com/praydog/UEVR).
- **Upstream compatibility build:** [joeyhodge/UEVR — 102626_FFR_SWGR_Patch](https://github.com/joeyhodge/UEVR/releases/tag/102626_FFR_SWGR_Patch).
- **Upstream download:** [uevr.zip](https://github.com/joeyhodge/UEVR/releases/download/102626_FFR_SWGR_Patch/uevr.zip), fetched directly from its publisher by the launcher.
- **Far Far VR customization delta:** [backend.ffvrdelta](https://github.com/pr-io/Far-Far-VR/releases/download/v0.4.3/backend.ffvrdelta), applied locally using Windows MSDelta.
- **Maintained source changes:** [engine patch](https://github.com/pr-io/Far-Far-VR/blob/main/Engine/cinema-comfort.patch) and [console compatibility patch](https://github.com/pr-io/Far-Far-VR/blob/main/Engine/farfar-console.patch).

The player ZIP excludes complete `UEVRBackend.dll`, `LuaVR.dll`, `openxr_loader.dll` and `openvr_api.dll` binaries. The installer checks the upstream archive, customization delta, reconstructed engine and companion DLLs before completing setup.

| Verified component | SHA-256 |
| --- | --- |
| Upstream archive | `FDF325C62BA43BC1DA98ED5F453633FA106CAEF0DF9CB8121566F7AA138F6D55` |
| Customization delta | `EC35A59458BBD782DC625031D38B333ECA1CBA157DAD4251A1CEFDC36D7B7716` |
| Installed backend | `2F30D9353B02435F0D34A6505D2FF494F4DACD20783F8D72CB408F9AEBE41558` |

The upstream engine [license](https://github.com/joeyhodge/UEVR/blob/963b157573eaef20528b602d1d9790b8105819b3/LICENSE) states "All rights reserved." Upstream retains its rights; attribution, public source access and distributing a delta do not grant additional engine rights or establish endorsement. No blanket legal certification or commercial-use permission is claimed.

## Game and community

Players must own [Far Far West on Steam](https://store.steampowered.com/app/3124540/Far_Far_West/). The mod download contains no game executables, game audio, game textures or player-progress saves. Settings templates are supplied for VR/desktop configuration and controller bindings.

Thanks to the [Flat2VR](https://flat2vr.com/) community. All third-party names and trademarks belong to their respective owners. See [DISCLAIMER.txt](DISCLAIMER.txt) for the warranty disclaimer.
