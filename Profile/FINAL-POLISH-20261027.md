# Final polish candidate — October 27, 2026

The wearer reported that the October 26 build was nearly ready. This batch
targets only the remaining transition, TAA, lobby guide, loading train and horse
look issues. Headset acceptance of this batch is still required.

## Changes

- Far Far West's D3D12/OpenXR double-wide path retains the last released scene
  image while native stereo has no validated right-eye packet after travel or a
  camera cut. Cinema no longer overwrites that hidden scene image with its movie
  texture. This avoids submitting the raw partially rendered scene during capture
  recreation. Completed native eye pairs and the accepted movie crop, black
  surround and single BOTH-eye quad remain as before. The hold may briefly keep
  the prior scene visible during recovery; logs identify it as `[FarFarVR][Transition]`.
- Candidate TAA correction: when TAA (method 2) and the native stereo correction
  are active, `r.TemporalAA.Upsampling` is temporarily 0. TAA remains selected;
  spatial output scaling avoids the temporal upscaling path suspected of retaining
  packed viewport history. This hypothesis has not been reproduced or confirmed
  remotely. AA-off, FXAA and TSR selections are preserved. Render resolution and
  combat materials are unchanged. The original CVar restores outside that context
  or on reset. A CVar failure is reported separately and cannot disable camera
  comfort handling. A process exit discards this session-only CVar override.
- The existing HMD projection now also adopts `UI_Guide`, found on the lobby's
  `BP_LobbyGuide_C.Widget`. This is the level guide omitted from the accepted POI
  class list. Its original widget, text and animation are retained; the existing
  POI projection math and other classes are unchanged. The separate controller
  interaction targeting and pickup widget are unchanged.
- `BP_LoadingScreen_C` is explicitly recognized before menu/gameplay handling.
  Both map generation and its countdown use game-camera aim and a fixed cinema
  screen. Only this loading context disables decoupled pitch, retaining the
  authored `CamMain` train composition instead of flattening its camera angle.
  Prior aim, screen, roomscale, pitch, UI and native stereo settings restore on
  exit, reset or next startup recovery. No train/camera transform is invented.
- While mounted, the original spring arm again consumes native control rotation
  and pitch, and the input filter passes analog right-stick Y through. Mounted
  comfort still selects game-camera aim (0), so tracked right-hand aiming does
  not steer the view. Camera-local pitch stays neutral, roll inheritance stays
  disabled, and boom collision stays enabled. Distance, FOV, sprint effects,
  on-foot input filtering, buttons and bindings retain their prior behavior.

## Validation

Release backend and launcher builds succeeded. All 56 profile behavior tests and
33 native offline checks pass. These cover loading/cinematic/menu restoration,
mounted analog input and camera flags, TAA/FXAA/TSR selection isolation, graphics
write failure isolation, existing crosshair/POI geometry and controller bindings.

The initial remote launch reached the engine/stereo hooks and allowed native
reflection of `BP_LoadingScreen_C`, `CamMain`, `UI_Interact` and the map generator.
The desktop image was black with `Present failed: 87a0001`; desktop input was
also denied. This is not a rendered gameplay or headset validation. No wearer
acceptance is claimed for any new visual or comfort behavior.

After installation, a fresh normal launch loaded the exact new backend from
`dist/FarFarVR/UE5.8` and completed both engine/stereo hook markers. The updated
comfort module reported idle context without a script error. A temporary native
probe found the AA CVars and completed its write/restore assertions before
stopping at a nil `CamMain` reference on the Blueprint class default object;
camera components need a constructed pawn for that measurement. Consequently no
native train transform/framing measurement is claimed. The probe was removed,
the diagnostic game was closed and the pre-test configuration/journal restored.
Installed scripts match the source/package. The accepted crosshair module and
Touch binding file are byte-for-byte unchanged. The previous build can be
restored with `diagnostics/final-polish-20261027/Restore-Previous-Runtime.ps1`.

Backend SHA256:
`240C5ACCB5A1E02862101A5809B10C22D9403A1B5ED1A3095DDF4CB29BA5EC73`.
The normal launcher selects this exact corrected backend. The optional focused
menu preview is excluded from this build, as in the accepted normal runtime.

## Evening headset checks

1. Enter a lobby and exit a cinematic: check the right edge in both eyes.
2. Select TAA, fire and hit nearby enemies: check for upward compression or a
   black lower edge. Compare FXAA only if needed; TSR testing is not required.
3. Look around the lobby with the controller still, then raise/move the hand
   toward the train guide. Check guide placement and all existing POIs.
4. Start a mission: move the right controller independently during generation
   and countdown. Check the fixed screen, train framing and direction.
5. Ride, look with both stick axes, move the hand independently, then dismount.
   Check camera comfort/collision and restoration of normal gun aim.

Pre-change runtime, launchers, package and profile are under
`[local UEVR workspace]/diagnostics/final-polish-20261027/`. Close the game before rollback.

