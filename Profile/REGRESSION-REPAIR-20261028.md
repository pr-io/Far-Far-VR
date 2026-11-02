# Regression repair — October 28

Evidence was preserved before editing in
`diagnostics/regression-repair-20261028/active-profile-before/`.

## Gun-following crosshair

At 19:32:41 the HUD module logged `Restored original HUD: caught (...) exception`.
Its error handler restored the original centered widget and latched the module
off for the remainder of the session. This explains the reported crosshair
falling back into the ordinary information HUD.

The module now validates widget/root/camera lifetimes, refreshes the current
camera manager, and retries a failed build after one second. It records the
failure stage. Unused weapon inventory scanning and cached weapon diagnostics
were removed: the crosshair already uses the camera ray, and stale weapon
references must not disable it. Original material, animation, 275 cm aim
projection, unarmed dot and the separate HMD POI module are preserved.

## TAA bottom crop

The failed session repeatedly logged a structured exception in the console
variable integer getter. Disassembly of the game's own 5.8.1 interface showed
the heuristic had selected slot 22, which is the resolved-context setter. The
correct integer getter is slot 25. The exception handler returned zero, so the
Lua guard interpreted active TAA as AA method zero and skipped the override.

The SDK now recognizes this exact executable/version using instruction
witnesses for the flags/tag context copy and its setter. It selects the existing
UE5.7+ resolved-context ABI (Set 22, GetInt 25, GetFloat 26). This scope refuses
legacy guessing when witnesses fail. The TAA-specific guard retains its original
behavior: spatial output scaling while method 2/native correction are active,
restoration otherwise. AA selection, FXAA and TSR are not overridden.
Temporary write failures retry after one second instead of disabling the guard
for the rest of the session.

## Cutscene transitions

The previous hold ended on the first ready eye packet. It therefore exposed
gameplay during a short return to the pawn between consecutive movie shots.
The movie pixels and pose now remain held for at least 150 ms and until a valid
pair is ready. A new movie inside that gap resumes cinematic presentation
without exposing gameplay. Settings/runtime/swapchain recovery paths remain.

The last-pass automatic standing-origin compensation has been removed. No
additional camera, world-scale or tracking-origin adjustment is introduced.
Manual launcher Start, the requested controller gestures, head-directed
movement, mounted analog look, and the accepted POI script remain intact.

## Validation boundary

Lua/profile checks and native policy checks cover recovery, stale lifetimes,
controller aim projection, scope validation, mutated/truncated ABI witnesses,
consecutive movie gaps, ready-pair timing and escape/reset behavior. Build and
runtime checks are recorded alongside the evidence. Physical headset acceptance
of combat with TAA and cinematic transitions remains separate.

Installed backend SHA256:
`79430B98FC2ED29DA30E355920DAE5500ABA6ECF5B86440382CE7AC1246AD006`.

- Release engine/launcher builds succeeded; 33 native checks, 61 Lua/profile
  checks, runtime policy and controller policy suites passed.
- The actual launcher Start button launched the repaired backend and completed
  both hook markers. Opening the launcher alone did not launch the game.
- A temporary in-game probe read the current AA method as 1 and verified real
  temporal-upsample reads/writes/restoration (`0 -> 1 -> 0`), preserving AA
  selection. The repaired ABI was validated in the running process; no old
  getter exception was present.
- The local tutorial created the v8 HUD without latching an error. OpenXR
  reported no available headset; status explicitly reported runtime unavailable
  and zero per-view aim updates. This does not verify gun following visually,
  combat TAA cropping, or the movie handoff in an HMD.
- The test game was closed, the temporary probe removed, and pre-test saved
  settings/recovery journal restored. Installed module copies match.
