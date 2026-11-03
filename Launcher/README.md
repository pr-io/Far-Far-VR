# Launcher source

The self-contained Windows x64 launcher detects the Steam game, installs and verifies the runtime/profile, launches and injects the game, and provides desktop VR settings. Players use the complete release ZIP; publishing the executable alone is insufficient.

- `Program.cs`: startup and injection sequence.
- `Setup.cs` / `RuntimePatch.cs`: game detection, preferences, profile installation and verified runtime reconstruction.
- `SettingsIsolation.cs` / `SocialBindings.cs`: independent VR/desktop settings and controller mappings.
- `SessionLink.cs`: live game readiness and menu requests.
- `Updater.cs` / `Apply-Update.ps1`: release checks, manifest validation, staging and replacement with rollback.
- `SettingsForm.cs` / `Theme.cs`: launcher settings and appearance.

See [BUILD.md](../BUILD.md) for build/test commands and [README.md](../README.md) for player instructions.
