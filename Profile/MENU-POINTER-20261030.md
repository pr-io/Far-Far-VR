# Controller pointer on cursor-selection screens — October 30

The Horizon/OpenXR profile now has a game-menu pointer in addition to the
existing UEVR settings pointer. New cursor pages begin in laser mode, using the
last selected hand (right hand initially). A visible ring marks the ray hit.

- Aim the selected controller to move the game's mouse cursor.
- Press either trigger to select that hand as the pointer and perform a mouse
  selection where it points. Pressing outside the menu cannot click on re-entry
  while the trigger remains held.
- Move either analog stick past the navigation threshold to hand control back
  to native gamepad navigation. Trigger press reclaims pointer mode. A held stick
  does not repeatedly steal it back until recentered and moved again.
- Existing A confirmation remains available. Existing Start, both-stick gestures,
  Y voice hold, controller aiming and horse/emote axis behavior are preserved.
- Page changes release old mouse presses. Entering a screen with a trigger
  already held cannot accidentally select. Leaving a cursor screen releases its
  mouse button and quarantines a held selection trigger until fully released,
  so it cannot turn into a weapon shot.

Context comes from the game's bShowMouseCursor, title pawn or actual visible
emote wheel. Visible top-level widget/switcher changes identify new pages; the
inventory is sampled every 150 ms. Persistent player HUD/hit/POI subtrees are
excluded. Other cursor screens, including card selection, use this same cursor
path without changes to their game widgets. Gameplay, movies, non-cursor screens,
VR settings, headset inactivity and loss of game-window focus disengage it.

Both hand rays intersect the actual OpenXR game-UI plane, including correct
stage/view-space conversion. The software pointer uses the existing framework
texture on the exact menu pose/size. No extra render target is allocated and no
world, weapon, headset, game HUD or menu anchor is moved. The mouse is positioned
on the game thread, and down/up messages are addressed to the game window only
while it is foreground. This is a pointer dot/cursor, without a world-space beam.

All seven existing Lua modules are byte-identical to the pre-feature copies,
including the latest gun crosshair/hit-X and narrow analog fixes. Native changes
are isolated to pointer state/context, read-only menu-ray measurement, selection
trigger masking and copying/submitting its cursor layer when needed. Existing
scene copies, AA, cinematic handoff, POIs and game bindings are unchanged.

Validation includes the native pointer policy and real projection helper:
page defaults, either-hand clicks, analog takeover/hysteresis, held-trigger entry,
page/focus release, stale/outside hits, trigger quarantine (including partial
release), signed controls and moved/turned head vs fixed-stage menu geometry.
The existing 71 profile checks are also retained. Build/native/startup evidence
and backups are in diagnostics/menu-pointer-20261030/. These checks do not prove
physical pointing, native hover/trigger selection or every game page in a
headset; those remain wearer acceptance items.

Release backend SHA256: 8DCA464433CA0BBF62498B28F31CED5E35CAFD05AD53FBDABEE236B6234758B7.
All 33 native compatibility checks, 71 existing profile tests and pointer policy/projection checks passed.
Live startup/API inspection was blocked: GetCursorPos failed with Access is denied (0x80070005), repeated on one retry. No game was launched and no physical VR result is claimed.
