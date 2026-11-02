# Validation on October 23, 2026

- .NET 8 Release publish completed without warnings or errors.
- Visually checked the native launcher and used its Start Game in VR button.
- Fresh launch began with no game process running.
- Launcher log confirmed Administrator=False.
- Steam started the installed original game as PID 8628.
- Launcher waited through opening credits until the title-screen marker appeared in the current game log.
- Backend load was requested at 23:13:06; the actual loaded module was confirmed at H:\UEVR\UE5.8\UEVRBackend.dll.
- Fresh UEVR log confirmed Hooked UGameEngine::Tick at 23:13:15 and Finished hooking FFakeStereoRendering at 23:13:19.
- The launcher displayed completion and exited automatically. The game remained running.
- Opening a second launcher while startup was in progress exited cleanly, with one game and one active launcher remaining.
- Starting the launcher again after VR loaded reused the same game and detected the already loaded backend; no second injection was requested.
- The saved Touch X/B binding file was unchanged.
- The headset was idle during this launcher workflow test. This validates automatic launch and injection; it does not constitute a new headset gameplay acceptance test. This same maintained UEVR build was previously confirmed playable in the headset by the user.

The executable uses an asInvoker manifest. No UAC setting, scheduled task, service, Steam elevation setting, or game executable was changed.

# Far Far VR foundation — October 24, 2026

- Release publish and local package staging completed without warnings/errors.
- Opened the executable from `dist/FarFarVR` and visually inspected the main screen and full setup dialog.
- Steam library discovery resolved `H:\SteamLibrary\steamapps\common\FarFarWest` without a hardcoded game path.
- Setup resolved the runtime beside the packaged executable. Saving paths without VR changes preserved active config SHA256 `68AF8ED58BF338DDFE03D4539EE5D3137A88A36BA7CBCEEAB45CEB911A205564` and Touch binding SHA256 `1602DF8B4CD4D70A23D0942CFE39292F3C6BEB0E8CB4DC282CD4ECD211F524CD`.
- Clicked Start Game in VR with no game running. Steam started PID 25480 at 13:36:07 local time. Launcher waited for title-screen readiness, requested VR at 13:36:48, confirmed the packaged backend module and engine hooks at 13:36:56, then exited automatically.
- The game was left running after this test. No fresh headset gameplay, custom in-game menu, first-install profile deployment on another account, or changed comfort-setting wearer acceptance is claimed.
- After backend startup, UEVR itself rewrote `config.txt`, omitting eight `OpenXR_*` entries. All remaining baseline lines matched and the Touch binding file hash remained unchanged. The earlier unchanged-config hash refers to launcher setup/save before injection, not the backend's subsequent profile save.
- Source/runtime provenance and public-release requirements are recorded in `../THIRD_PARTY.md`. UE4SS is not installed or required by this baseline.
