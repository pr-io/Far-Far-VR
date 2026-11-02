# Emote and battle cry — October 28

The current game save explicitly unbound the gamepad mapping named `emote`.
That override supersedes the asset default `IA_Emote -> Gamepad_DPad_Left`, so
the previous virtual D-pad event could not open the wheel. `InputBattleCry` was
also unbound; the asset confirms `IA_BattleCry`, with keyboard B and no default
gamepad key. Chat is already unbound on this player's profile.

The launcher repairs only these two existing serialized mapping records:

| Record | Gamepad key |
| --- | --- |
| emote | Gamepad_DPad_Left |
| InputBattleCry | Gamepad_DPad_Right |

The repair validates the save type, rejects missing/ambiguous/unfamiliar
records, proves that reversing the two replacements reproduces every original
byte, and makes a timestamped backup before atomic replacement. It runs only
with the game closed, is idempotent, and runs on Start rather than opening the
launcher. All other input records are preserved.

The exact-game native gesture policy now uses:

- Quick press/release of both sticks: latch the game's emote key so its
  hold-to-show wheel stays open. Choosing/cancelling the real wheel releases
  that virtual key; another quick chord also closes it.
- Both sticks held for 500 ms: one battle-cry press, without waiting for release.
- A long hold does not also emit emote, map, ping or mount; both sticks must be
  released before another chord. Battle-cry pulses survive engine/XInput polling.

Start tap/hold, individual right-stick map/ping, left-stick mount, axes, face
buttons and shoulder combinations retain their previous behavior. No rendering,
camera, AA, menu-pose or HUD module change is included in this batch.

Validation includes both chord orders, hold threshold, repeated polling,
release/rearm, focus loss and menu cancellation. The actual save validates
against the C# migration; synthetic records test exact preservation,
idempotence and refusal. Physical headset/controller acceptance is separate.
An offline desktop check confirmed that a short keyboard emote press only
briefly shows the wheel and releasing closes it; this is why the emote path
latches rather than using a short pulse. The desktop check used the prior
wearer-tested runtime, and does not validate the final physical VR gesture.

Before-change runtime, launchers, input save and profile are in
`diagnostics/social-controls-20261028/`.

Release builds succeeded. All 33 native offline checks, gesture/latch tests,
and C# binding-preservation/idempotence/refusal checks passed. The saved input
repair has been applied; reversing just those two records reproduces the
pre-change save byte-for-byte. The final physical VR gesture is unverified.

Installed backend SHA256: `FE3180423895DBB7D97A3C9DCAA6A531EF9594C95E73D58C7D2CF08E0E994F72`.
