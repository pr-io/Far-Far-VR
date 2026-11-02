# Third-party notices and distribution status

## UEVR runtime and source

- Original project: [praydog/UEVR](https://github.com/praydog/UEVR), by praydog and contributors.
- Compatible public base release: [joeyhodge/UEVR `102626_FFR_SWGR_Patch`](https://github.com/joeyhodge/UEVR/releases/tag/102626_FFR_SWGR_Patch).
- Pinned UEVR source revision: `963b157573eaef20528b602d1d9790b8105819b3`.
- Pinned UESDK revision: `811b01498b61e6a0246615b4f2b4fb4f4067fb64`.
- Accepted Far Far VR backend SHA-256: `2F30D9353B02435F0D34A6505D2FF494F4DACD20783F8D72CB408F9AEBE41558`.
- Downloaded base archive SHA-256 from local provenance: `FDF325C62BA43BC1DA98ED5F453633FA106CAEF0DF9CB8121566F7AA138F6D55`.

The UEVR and pinned fork license files identify the work as copyright praydog, all rights reserved. Confirm applicable permissions for this exact fork/build and its dependencies before publishing binaries or modified engine source. The package builder is for local staging and verification.

The repository maintains the Far Far West engine modifications as patches and headers against the pinned revision rather than vendoring the entire UEVR source tree. Build prerequisites and reproducibility steps are documented in [Engine/README.md](Engine/README.md).

## Profile and mod content

The game profile and Touch binding layout were customized specifically for Far Far West to resolve HUD projection, aim decoupling, camera height calibration, and social controls. 

## Other components

- **Far Far West** and its marks belong to their respective rights holders. This is an unofficial, community VR mod. No game binaries, game assets, save files, or extracted game content belong in this repository or release.
- **UE4SS** is not a dependency of the current UEVR-only baseline and is not included.
- Runtime libraries such as OpenXR/OpenVR and Lua are excluded from Git source tracking.
