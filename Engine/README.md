# Engine customizations

Player installation is automatic; these files are for maintaining the customized runtime.

- Base UEVR revision: `963b157573eaef20528b602d1d9790b8105819b3`.
- UESDK revision: `811b01498b61e6a0246615b4f2b4fb4f4067fb64`.
- `cinema-comfort.patch`: cumulative Far Far VR menu, input, pointer, compositor, camera and performance changes against the UEVR checkout.
- `farfar-console.patch`: console-interface compatibility changes against its UESDK checkout.
- `FarFarWest*.hpp`: game-specific policies and helpers used by those patches and native regression tests.

Apply both patches using `scripts/Prepare-Engine.ps1`; the product headers must also be present. Do not substitute an untested upstream or newly compiled DLL for the pinned runtime just because it builds.

The player launcher downloads the pinned upstream archive and applies the packaged Windows MSDelta patch during setup, then verifies the complete result. It does not compile source or run a background patch service.

| Component | SHA-256 |
| --- | --- |
| Upstream ZIP | `FDF325C62BA43BC1DA98ED5F453633FA106CAEF0DF9CB8121566F7AA138F6D55` |
| Upstream backend | `FEFF6417359F6E36ABAC6577598A86B96D485A023A850F8614043AA0A508C8CB` |
| Far Far VR delta | `6A4E0FF8FC42E94B0FD5F8C1FBC3C41FFDA846C5B621F3318C89AD65FC41C43D` |
| Installed backend | `1B6AC545B746190F4502C346897A36A3002C4BB61FD994521C70579F21B47A74` |

See [BUILD.md](../BUILD.md) for reproduction and regression checks, and [THIRD_PARTY.md](../THIRD_PARTY.md) for attribution and upstream terms.
