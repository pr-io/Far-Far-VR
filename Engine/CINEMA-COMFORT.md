# Local cinema renderer correction

Current follow-up: [October 28 controls and cinema handoff](../Profile/CONTROLS-HEAD-20261028.md).
It adds a Far Far West-only scene-copy hold across native capture recovery.
The historical hashes and October 26 validation below describe earlier builds.

`cinema-comfort.patch` applies to the pinned UEVR source revision
`963b157573eaef20528b602d1d9790b8105819b3`. It modifies only D3D12 movie eye crops,
OpenXR cinema layer selection and cinema quad opacity for the exact
FarFarWest-Win64-Shipping.exe executable. The normal stereo path is unchanged.
It also scopes the existing IXR camera-update timeout recenter to other games:
Far Far West keeps the wearer's explicitly chosen facing and pre-flattened
rotation basis when camera updates pause for more than two seconds. The failed
run logged that timeout immediately after cinematic/loading gaps. Manual recenter
controls remain available; this change does not recenter automatically on travel.
The source patch and launcher known-hash list are committed-source artifacts;
the locally compiled binary is excluded by the project's existing ignore rules.

The checked-out source also contains the optional focused-menu preview patch.
The cinema build used the pinned original versions of Framework.cpp,
LuaLoader.cpp and LuaLoader.hpp during compilation, and restored the preview
source afterward. The established universal settings UI remains in this binary.
The compile used the existing Build-Engine.ps1 toolchain and dependencies.

Known cinema backend SHA256:
`1D9A89BF3AE6F7517CA6C38ACA0CB424AF290EBD86A5F49DD21E1A355889FAD3`.
The local launcher accepts exactly this hash in addition to the earlier runtime.
It fits the current 16:9 movie eye to the screen target with black letterboxing
instead of stretching it vertically, then submits that one bounded eye as a
single BOTH-eye OpenXR quad. This prevents lower-frame sampling artifacts and
the occasional exposed side-by-side right-eye image. The active normal-launch runtime is `dist/FarFarVR/UE5.8`; the earlier runtime
and profile were backed up in `diagnostics/near-dot-cinema-mount-20261026`.
The original `UE5.8` folder remains available separately.

Validation: successful Release build, 33 native offline checks, active OpenXR
FOCUSED session, actual tutorial camera entry/exit, and a real movie texture of
3840 x 1080 sampled as 1920 x 1080 per eye rather than cached 3648 x 1968.
The headset mirror confirmed a black surround. The diagnostic paused frame was
dark; moving-video distortion, wearer comfort and mounted terrain behavior
still require acceptance. See Profile/POLISH-20261026.md for the full boundary.
