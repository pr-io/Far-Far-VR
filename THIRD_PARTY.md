# Third-party notices and distribution status

## UEVR runtime and source

- Original project: [praydog/UEVR](https://github.com/praydog/UEVR), by praydog and contributors.
- Compatible public base release: [joeyhodge/UEVR `102626_FFR_SWGR_Patch`](https://github.com/joeyhodge/UEVR/releases/tag/102626_FFR_SWGR_Patch).
- Pinned UEVR source revision: `963b157573eaef20528b602d1d9790b8105819b3`.
- Pinned UESDK revision: `811b01498b61e6a0246615b4f2b4fb4f4067fb64`.
- Accepted Far Far VR backend SHA-256: `240C5ACCB5A1E02862101A5809B10C22D9403A1B5ED1A3095DDF4CB29BA5EC73`.
- The public release's standard backend has SHA-256 `FEFF6417359F6E36ABAC6577598A86B96D485A023A850F8614043AA0A508C8CB`; its `DIBRUEVRBackend.dll` asset has SHA-256 `172C3B83BB9EAED46AC3FE76A71B540772005FA55FB5F68BD852D1E2CBACC3CF`. Neither matches the accepted Far Far VR backend.
- The downloaded `uevr.zip` archive was recorded with SHA-256 `FDF325C62BA43BC1DA98ED5F453633FA106CAEF0DF9CB8121566F7AA138F6D55`.

The UEVR and pinned fork license files identify the work as copyright praydog, all rights reserved. No written grant to redistribute the customized backend or runtime dependencies is recorded here. A private GitHub repository or release is still a hosted copy; do not upload or redistribute these binaries until the applicable rights holder grants permission. The required custom backend is not part of the public release assets listed above. Setup must reject those assets rather than silently install a stock backend.

The repository keeps the Far Far West engine modifications as patches against the pinned revision rather than vendoring the UEVR source tree. Build prerequisites, UESDK source-access requirements, and reproducibility limits are documented in [Engine/README.md](Engine/README.md). A successful local build or source access is not permission to redistribute the result.

## Profile and mod content

The game profile and Touch binding layout were recovered from a local working Far Far VR installation. Original profile authorship and redistribution terms were not established in the available project records. Obtain permission and add any required credit before distributing the profile or its binding data.

## Other components

- **Far Far West** and its marks belong to their respective rights holders. This is an unofficial, community VR mod. No game binaries, game assets, save files, or extracted game content belong in this repository or release.
- **UE4SS** is not a dependency of the current UEVR-only baseline and is not included.
- Runtime libraries such as OpenXR/OpenVR and Lua are not copied into the source repository. If a rights-cleared release later bundles them, include the exact license notices shipped with that runtime.

This notice records the information available during project preparation; it is not legal advice or a redistribution grant.
