# Native backend source and reproducible build

Far Far VR keeps its engine changes as focused patches; the upstream UEVR source is not copied into this repository.

- UEVR source: `https://github.com/joeyhodge/UEVR`
- Required revision: `963b157573eaef20528b602d1d9790b8105819b3`
- Required UESDK revision: `811b01498b61e6a0246615b4f2b4fb4f4067fb64`
- Required Far Far West backend SHA-256: `240C5ACCB5A1E02862101A5809B10C22D9403A1B5ED1A3095DDF4CB29BA5EC73`
- Required patch: `cinema-comfort.patch`

Run `scripts/Prepare-Engine.ps1` to clone/check out the pinned fork revision and apply the game-specific patch. It verifies the pinned base before changing the ignored `vendor\UEVR` checkout and detects an already-applied patch. `scripts/Build-Engine.ps1` verifies the source and UESDK revisions, uses a detected Visual Studio 2022 x64 toolchain, and builds into `vendor\UEVR-build`; it never promotes or overwrites a runtime. `scripts\Test-Engine.ps1` runs the upstream offline checks.

The patch changes the native D3D12/OpenXR movie and stereo paths, protects the chosen camera basis during pauses, and retains the last complete game scene while native eye capture recovers. It is restricted to Far Far West where possible. The game-specific Lua scripts separately implement the HMD-based world-marker/POI placement, tracked-hand aiming independent of camera rotation, HUD/crosshair, objectives, comfort, mount and render behavior.

UESDK may require access through the builder's Epic-linked GitHub account. Visual Studio Build Tools, CMake, the Windows SDK, and native dependencies are needed only to rebuild the backend, not to install/play the mod. Exact build prerequisites and runtime hashes are recorded in `scripts\Build-Engine.ps1`, `scripts\Test-Engine.ps1`, and `THIRD_PARTY.md`.

The modifications and build recipe do not grant rights to distribute UEVR binaries, source excerpts, or dependencies. The pinned project license is recorded as all rights reserved. Do not publish a modified runtime until the applicable permission is obtained. The focused-menu preview patch is an optional development path and is not part of the accepted release backend.

The accepted desktop/runtime checks and outstanding headset acceptance items are recorded in [VALIDATION.md](VALIDATION.md). Building and reaching engine hooks are not proof of headset rendering or comfort.
