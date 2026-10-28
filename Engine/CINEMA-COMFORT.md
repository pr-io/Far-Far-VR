# Far Far West native stereo corrections

`cinema-comfort.patch` applies to UEVR revision `963b157573eaef20528b602d1d9790b8105819b3`. It modifies only four native files. The patch corrects the game's D3D12/OpenXR cinema eye crop, bounded BOTH-eye layer and opacity, keeps the selected camera basis through engine camera-update pauses, and holds the last completed game-scene image while native eye capture recovers.

The accepted current backend was built with the normal universal UEVR settings menu; the optional focused-menu preview changes are not part of that backend. The backend hash and dependencies are recorded in `THIRD_PARTY.md` and `Profile/FINAL-POLISH-20261027.md`.

Desktop/native evidence includes successful engine builds and offline checks, OpenXR native hook/reflection observations, and known scene/present behavior. It does not establish headset display output, physical comfort, correct behavior in every level, or frame timing. See `Engine/VALIDATION.md`.
