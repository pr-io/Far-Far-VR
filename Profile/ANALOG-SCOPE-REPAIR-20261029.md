# Narrow analog correction — October 29

This supersedes the input behavior in EMOTE-TRANSITION-20261029.md.
The earlier movement lock, StopMovementImmediately call, direct control-rotation
writes, custom orbit rate/clamp and vertical-axis suppression have been removed.

The established native input path is restored. Only sThumbRY is negated (with
signed-axis bounds) while riding the horse or actually playing an emote, outside
menus and outside the emote selection wheel. The left stick, horizontal look,
buttons, triggers and shoulders are unchanged. Title/loading screens, game menus,
UEVR settings and the emote wheel receive their raw axes without inversion or
the gameplay shortcut filter. Entering a menu clears prior emote/mount state.
The existing on-foot right-stick shortcut threshold resumes after dismount or
emote exit. No controller movement-lock or camera-rotation functions are called.

The native backend, flicker guard, graphics settings, AA, HMD offsets, aiming,
POIs, menu anchoring, saved bindings and gesture controls were not modified.
Only the input Lua module was replaced in source, package and installed profile.
69 profile tests pass, including complete menu/wheel axis preservation, isolated
horse/emote inversion, signed-axis limits and restoration after emote/dismount.
Physical controller/headset confirmation remains pending.

Backup: diagnostics/analog-scope-repair-20261029/farfarvr_input.lua
