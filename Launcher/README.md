# Far Far VR launcher source

See [the project README](../README.md) for setup, packaging and current limitations.

Build: `dotnet publish FarFarWestVR.csproj -c Release -r win-x64 -o publish`.

The supported local package builder is `../scripts/Build-Package.ps1`. On a new installation the launcher needs the compatible runtime folder and baseline `Profile` beside its executable; publishing the executable alone does not create a complete package.

`Program.cs` contains the existing launch and native loading sequence. `Setup.cs` contains Steam discovery, preferences, first-use profile installation and the focused launcher options screen. The universal UEVR frontend is not launched. The backend's in-game menu is still upstream UEVR.

`Updater.cs` performs release checks and package staging off the render thread. Public releases require no token; private preview releases may use `%LOCALAPPDATA%\FarFarWestVRLauncher\github_token.txt`. The staged archive is validated against its SHA-256 manifest and pinned backend hashes. `Apply-Update.ps1` is launched on the next launcher start, waits for the launcher and game to exit, then replaces package files with rollback backups.

Historical October 23 validation is preserved in `VALIDATION.md`; subsequent checks are appended by date.
