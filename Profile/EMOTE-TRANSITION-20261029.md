# Emote input isolation, inverted orbit and transition guard — October 29

- The emote wheel keeps the original stick navigation. A balanced, temporary
  controller movement lock prevents the same left-stick input from walking.
  Opening it stops existing character velocity once. Closing or selecting clears
  only this module's lock; a pre-existing ignore-movement state is preserved.
- During emote playback, right-stick Y is removed from the game's Enhanced Input
  stream so its digital Up/Down bindings cannot trigger melee or grenade. Its
  inverted analog value updates the existing control rotation on the game thread
  at up to 90 degrees/second, with pitch bounded to +/-89 degrees. Horizontal
  native orbit is retained. Input older than 100 ms cannot keep rotating a camera.
- On the horse, right-stick Y is inverted with safe signed-axis bounds. The
  existing game-camera horse movement/orbit behavior is retained.
- Closing the wheel or stopping/breaking an emote restores gameplay axis handling
  immediately on the next input poll. Reset releases the movement lock. Controller
  and menu bypass behavior, A selection, both-stick battle cry and Y voice hold
  are preserved. The input module validates widget lifetime during travel.

The old cinematic hold could release after 150 ms with a single ready capture.
The updated policy waits at least 350 ms after screen exit, requires at least two
unique packet serials and a 100 ms continuously ready interval, and resets that
interval on capture disruption. A new movie inside that window remains on the
movie path. Settings, headset idle, missing cached image and the existing 500 ms
native-disabled escape remain available. This is a transition candidate: actual
inter-cutscene flicker still requires wearer confirmation. It does not change
stereo layout, cinema crop, letterbox, projection or render resolution.

The automatic UEVR startup menu is closed once before runtime initialization for
this exact game. Start hold and Insert still open it explicitly; later runtime
retries do not close a menu the user opened. This avoids presenting the automatic
window before tracking and menu placement settle.

The wearer's preferred 120 FPS, medium post processing, FXAA and other low
settings are acknowledged and left as selected. This patch does not change the
game's graphics options, AA choice, HMD offsets, crosshair/hit markers, POIs,
head-directed on-foot movement or native horse spring-arm implementation.

Validation: 68 profile tests plus focused native cinematic and input checks;
full native compatibility suite and installed-startup results are recorded in
`diagnostics/emote-transition-20261029/`. These do not establish physical headset
comfort, flicker removal, or analog direction acceptance. Backups and restoration
instructions are in that same folder.

The final desktop startup check could not be completed: the computer-use helper
returned 'failed to activate captured window' on the initial attempt and the
fresh-window retry. The launcher was verified waiting for START IN VR, with no
game process running. No final live headset startup or gameplay result is claimed.
