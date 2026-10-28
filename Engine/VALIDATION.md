# Native backend validation

The recorded final-polish candidate was built from UEVR revision `963b157573eaef20528b602d1d9790b8105819b3` and the matching `Engine/cinema-comfort.patch`. Its backend hash is `240C5ACCB5A1E02862101A5809B10C22D9403A1B5ED1A3095DDF4CB29BA5EC73`. The previous known-good runtime and original profile were preserved in a local recovery snapshot outside GitHub.

The source history records a successful Release build, 33 native offline checks and 56 profile behavior tests. Native hook and reflection checks reached the engine/stereo hooks; the desktop run also recorded a failed display-present path. These checks do not establish rendered gameplay on a headset.

Outstanding real-device checks include both-eye lobby/POI placement, crosshair and HUD visibility, cinema/loading transitions, TAA/FXAA output, mounted camera and analog stick behavior, physical Touch controls, calibration, level transitions, comfort and frame timing. The current final-polish notes list these acceptance checks in `Profile/FINAL-POLISH-20261027.md`. No headset acceptance is claimed.
