# Far Far VR v0.4.11

## Graphics repair

Corrects a v0.4.10 privacy-cleanup error: two embedded compiled graphics shaders had metadata edited without their container checksums being updated. This release updates those checksums while retaining the sanitized paths.

The correction to the v0.4.10 native binary is limited to the two 16-byte shader checksums. Native executable sections and shader program chunks match the pre-cleanup engine. Gameplay Lua scripts, crosshair and effects implementations, controls, menu layout, live resolution saves, shutdown changes and updater/security improvements are retained.

## Install or update

Use the desktop launcher's **Updates** button, or download and extract **FarFarVR-v0.4.11.zip**. Close Far Far West before installing. The next launch installs the corrected runtime automatically.

The launcher and delivery delta were rebuilt. No local test suite, game launch or headset test was run for this repair.
