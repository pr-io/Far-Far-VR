# Launcher validation record

- The existing launcher sequence starts Far Far West through Steam when needed, waits for the title-screen readiness marker, injects only the accepted backend path, verifies that module is loaded, and waits for both engine and stereo hook markers.
- Launch is same-user and requests no administrator elevation. Windows access-denied failures are reported with a same-user/non-elevated recovery step.
- Steam library detection reads the local Steam library list and game manifest rather than relying on a developer's game folder.
- The launcher creates a profile only when none exists, preserves existing settings and controller bindings, backs up changed modules/settings, and recovers recorded comfort overrides before the next injection.
- The launcher now installs the render-fix Lua module and exposes the optional corrected game-control mappings in Setup. Those mappings are applied by the self-contained launcher, not by requiring Python on the player's PC.
- Backend acceptance is checked by SHA-256. The recorded required backend is `240C5ACCB5A1E02862101A5809B10C22D9403A1B5ED1A3095DDF4CB29BA5EC73`; generic or stale builds fail with an explicit error.
- Setup checks whether Windows has a valid active OpenXR runtime path. This registry check does not prove that a headset is connected or that rendering works.

Historical desktop testing confirmed Steam startup, title readiness, backend module load and both hook markers on the development installation. The existing reports do not establish clean-machine installation, current headset rendering, physical controller acceptance, game-playability, or frame timing. No headset success is claimed.
