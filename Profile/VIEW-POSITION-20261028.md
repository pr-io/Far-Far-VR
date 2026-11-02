# First-person position handoff — October 28

**Reverted during regression repair.** The automatic origin adjustment described
below has been removed. This document records the previous candidate, not the
currently installed behavior. See `REGRESSION-REPAIR-20261028.md`.

The wearer reports that the viewpoint feels forward and off-center after the
last updates. Comparing the October 28 batch against its saved October 27
baseline found no change to the gameplay camera offsets, scale, projection,
or weapon camera code. The cause of the wearer's specific observation has not
been reproduced in a headset.

A concrete position lifecycle gap was found in the fixed-screen comfort path:
cinematic/loading views disable roomscale movement and omit HMD translation,
then restore gameplay without consuming the headset movement made during that
screen. The outstanding horizontal movement can therefore become an offset in
the restored view, or be applied to the pawn by the roomscale renderer.

`farfarvr_comfort.lua` now samples the raw HMD position and standing origin on
entry to a fixed screen. Before restoring gameplay, it adds only the horizontal
movement during that screen to the origin. This preserves the pre-screen lean
and calibrated height. Consecutive map/countdown/cinematic cameras share one
handoff; an interim missing pawn does not finish it prematurely.

The correction never runs during ordinary gameplay, firing, aiming, game menus
or riding. Runtime inactivity drops the pending delta. Invalid poses and a changed
origin (manual calibration or a runtime reference-space reset) skip it. Pose
coordinates are transient and are never stored in the recovery journal.

No camera offset, field of view, world scale, facing rotation, control binding,
POI script, mount script, launcher or native DLL was changed. This is a targeted
candidate correction; wearer confirmation is still needed for the reported
viewpoint position.

Validation: all 63 Lua/profile tests pass, including six added handoff regression
checks. Static/simulated validation does not establish headset visual acceptance.
The normal launcher loaded the script and completed both engine/stereo hooks;
the comfort status updated without a script error. This desktop run remained
in the idle-headset context with graphics backbuffer errors, so it did not
exercise the physical transition or establish visual acceptance. The diagnostic
game was closed and its pre-test settings/recovery journal restored.
Pre-change script and runtime settings are backed up in
`diagnostics/view-position-20261028/runtime-before/`.
