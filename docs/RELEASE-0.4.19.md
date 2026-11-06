# Far Far VR v0.4.19 - First-launch profile setup fix

Fixes [issue #4](https://github.com/pr-io/Far-Far-VR/issues/4), where an update check could create an updater-only profile folder and prevent **START IN VR** from completing first-time setup.

- Update checks now wait until the VR profile is installed before writing headset update-status files.
- First-time setup recognizes folders containing only the launcher's update-status files, preserves a backup, and installs the complete bundled profile. Unknown files and existing player profiles retain their protection.
- If installation fails after moving the old folder, setup attempts to restore it automatically.

Includes the same native runtime and gameplay features as v0.4.18, including its performance improvements and optional diagnostics. Existing player settings and bindings are preserved.

Fresh install: extract the complete **FarFarVR-v0.4.19.zip** and open **Far Far VR.exe** with the game closed. Existing users can use **Updates** to download the release, then close the game and reopen the launcher to install it. No manual profile deletion is required for the updater-only folder problem.

Validation: launcher and regression-project builds, package integrity and privacy inspection, and GitHub automated checks. Local test suites and game/headset sessions were not run for this release; in-app update and first-launch acceptance remain for player testing.
