# Combat view duplication correction - 2026-10-26

The wearer reproduced the whole-headset side-by-side view on bullet impacts.
The fresh reproduction retained VR-sized targets and VR_2DScreenMode=false.
The cinema reallocations in older reports were not proof of the combat cause.

Live comparisons in the same run:

- Crosshair_Top-only HUD still split (wearer confirmed before this investigation).
- r.PostProcessing.DisableMaterials=1 still split. The Unreal log confirmed the
  Console-priority setting; materials were restored to 0 after the comparison.
- VR_EnableGUI=false still split; the wearer confirmed the HUD was hidden.
  The GUI was restored to true.
- r.RefractionQuality was already 0, confirmed through Unreal's console query.
- Synchronized Sequential stereo stopped the split but caused severe warping.
  It was reverted to Native Stereo.
- Native Stereo with VR_NativeStereoFix=true: wearer confirmed "No split or
  warping, depth normal". The backend logged an exact eye-pair validation and
  "active (validated right-eye resource submitted)".

The accepted setting is VR_RenderingMethod=0, VR_NativeStereoFix=true,
VR_Compatibility_SplitScreen=false. Existing same-pass/secondary-pass settings
remain unchanged. This isolates a native stereo rendering-path problem; it
does not identify the precise game shader or render pass responsible.

farfarvr_comfort.lua temporarily disables the correction for cinema, retaining
the original native cinema crop, black surround and BOTH-eye submission. It
restores the recorded gameplay preference afterward, on reset and after a
saved cinema override. It also suspends the correction while the headset is
inactive: an actual OpenXR IDLE transition otherwise caused rapid capture
creation/destruction. Live status confirmed Context: idle and that churn stopped.

The HUD and saloon objective scripts were not changed. Temporary comparison
scripts were removed from all active script directories and unloaded with the
normal LuaLoader reset. No backend DLL or launcher binary was changed.

Validation: 14 comfort, 10 HUD and 5 objectives offline checks passed. Combat
stereo/depth was confirmed by the wearer. After final cleanup and guard reload,
the wearer resumed the headset, repeated combat and checked cutscene borders:
"Combat and cutscene borders both good". Live logs also confirmed restoration
of the validated right-eye resource after resuming.

Evidence and prior copies: H:/UEVR/diagnostics/combat-split-before/.
