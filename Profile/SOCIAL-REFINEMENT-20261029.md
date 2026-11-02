# Emote confirmation, native riding controls and Y voice hold — October 29

Use the normal `H:/UEVR/Far Far West VR.exe` launcher and press START IN VR.

- Quick both-stick click toggles the emote wheel. While hovering an emote,
  press A to select it. A is consumed until released so confirmation cannot
  also jump. Clicking both sticks again cancels the wheel instead of selecting.
- Both-stick hold remains a single battle cry after 0.5 seconds.
- During emote playback, physical right-hand aim no longer rotates the game
  camera. The native sticks control the camera; analog vertical movement also
  bypasses the gameplay grenade/melee axis filter while selecting/playing emotes.
- While mounted, movement orientation temporarily uses Game camera (0), so the
  original horse controls receive the left stick without HMD-relative rotation.
  The existing mounted right-stick camera path is retained. Dismounting restores
  the saved movement direction and controller aiming. Menu, reset and crash
  recovery restore recorded preferences.
- Tap Y to switch weapons; hold Y for 0.5 seconds for the game's push-to-talk
  action until released. A long hold does not switch weapons. In UEVR settings,
  Y keeps its normal menu behavior. Focus loss cancels voice and requires release.

The native policy now distinguishes emote confirmation from cancellation. The
read-only game inventory confirmed UI_EmoteWheel's `canceled` flag and the
InputPushToTalk mapping name. Cancellation marks that flag on the game thread
before releasing the virtual emote key. A confirmation releases the key normally.

The launcher parses the complete custom saved key-override table before adding
one InputPushToTalk -> Gamepad_DPad_Up record, using the known gamepad metadata.
It first verifies the existing InputGrenade -> Gamepad_RightStick_Down override
has freed D-pad Up, and refuses a conflicting or unfamiliar table. The table
count and new record are the only new archive bytes. Reversing the insertion
reproduces the original bytes, and repeated migration is identical. Timestamped
save backup and unchanged-file/process guards remain in place.

The backend changes are limited to input handling. Cinematic rendering, AA,
headset offsets, crosshair/hit markers, POIs, menu anchoring and horse spring-arm
implementation are unchanged from the accepted baseline. Only the context and
input Lua modules changed. Backups are in
`diagnostics/social-refinement-20261029/`.

Offline verification: 66 profile tests, native gesture/hold tests, C# save-table
preservation/idempotence/refusal checks and the native compatibility suite.
Physical A selection, emote/horse camera feel and audible push-to-talk require
wearer testing; desktop startup and offline tests cannot establish those results.

Installed backend SHA256:
`03F3BD6823C1432D39620F3F9209C29DCA9FA239AE22FF9BC7183D8277743F32`.
