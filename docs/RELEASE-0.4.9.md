# Far Far VR v0.4.9

## Changes

- Continue monitoring the game after VR startup is canceled or fails, so desktop graphics and controller settings are restored when the game exits.
- Add a Cancel button for update checks and downloads in the desktop launcher.
- Stop stalled update transfers after 30 seconds without data, limit the complete download to 15 minutes, and remove incomplete downloads so they can be retried.
- Document desktop updates and shutdown-monitor checks.
- Enable automated Windows launcher, settings, updater, shutdown-monitor and Python/Lua checks on GitHub.

## Install or update

Download **FarFarVR-v0.4.9.zip**, extract the entire archive, and run **Far Far VR.exe**. Keep all included folders together.

Existing players can use the desktop launcher **Updates > Check for updates > Download update**. Close Far Far West, then select **Install & restart**. The headset Updates page can also prepare the update; exit the game and reopen the launcher to install it.

The runtime backend and customization delta are unchanged from v0.4.8. Players should download the complete ZIP; the separate delta asset is for package builders.

## Validation

GitHub's automated regression checks passed for the fixes in commit `134fa89`. The release launcher was rebuilt. No local tests or game/headset tests were run for this release.
