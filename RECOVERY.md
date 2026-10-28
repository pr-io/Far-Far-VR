# Backup and restore

## Before installing or updating

1. Close Far Far West, SteamVR/PCVR software, and Far Far VR.
2. Back up the entire active profile folder:
   `%APPDATA%\UnrealVRMod\FarFarWest-Win64-Shipping`
3. Back up the game input save if it exists:
   `%LOCALAPPDATA%\FarFarWest\Saved\SaveGames\EnhancedInputUserSettings.sav`
4. Keep a copy of the previous Far Far VR release/runtime package outside the game folder. Never overwrite the only known working backend.

Far Far VR installs its baseline profile only when no profile exists. It preserves an existing profile and makes timestamped backups before updating the Far Far VR Lua modules or settings. The optional **Apply corrected game controls** action makes a separate timestamped backup of the input save and refuses unknown save formats or unexpected mappings.

## Restore the profile

With the game closed, rename the current profile folder and copy the saved profile folder back to:

`%APPDATA%\UnrealVRMod\FarFarWest-Win64-Shipping`

Do not merge profile folders unless you know which settings you want to keep. To roll back a single launcher setting, restore the matching `config.txt.backup-*` file while the game is closed.

## Restore game controls

With Far Far West closed, restore the most recent:

`EnhancedInputUserSettings.sav.before-vr-input-*`

backup over:

`%LOCALAPPDATA%\FarFarWest\Saved\SaveGames\EnhancedInputUserSettings.sav`

The `farfarvr_input.lua` stick-axis filter is separate from the save mapping. To restore the original filter behavior, remove or move that module from the active profile's `scripts` directory.

## Restore the previous runtime

Close the game before switching runtime builds. Restore the complete previous `UE5.8` folder from the backup kept before the update; never combine DLLs from different builds. Start the launcher and confirm it reports the expected backend hash before starting the game.

## Local project recovery snapshot

The project creator keeps a local recovery copy of the previously playable package outside Git history. It is intentionally excluded from repository sync and GitHub releases because it contains UEVR runtime binaries whose redistribution permission is unresolved. A Git clone does not contain this local snapshot.
