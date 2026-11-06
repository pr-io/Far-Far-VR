# Far Far VR v0.4.18 — Performance fixes and optional diagnostics

Fixes two sources of unnecessary rendering work while preserving the complete cumulative mod. Player testing of the local candidate reported significantly better performance.

- **Stereo upscaler ownership:** prepares an independent secondary view family for the second eye using the supported game build's native copy/destruction routines. This removes the repeated DLSS temporal-upscaler ownership collision seen with the previous shared-family submission. Runtime guards retain the existing path if the game image/layout is unsupported.
- **Blank effects redraws:** limits initialization retries for an empty effects texture when native acknowledgement is unavailable. Visible effects keep their normal updates, and original effects remain available until the independent layer is ready.
- **Optional Diagnostics page:** adds performance/hitch capture, detailed frame tracing, live log detail and a **Turn off diagnostics** button to the Far Far VR menu. Capture options default off. Detailed tracing enables the information logging it needs; diagnostic files remain local. Turn captures off after troubleshooting.

Keeps the earlier render-thread memory-validation optimization, independent crosshair and effects layers, original artwork/animations, controls, calibration, comfort, settings separation, updater and shutdown behavior. Existing player graphics settings and bindings are preserved.

Validation: native and launcher builds; 112 automated profile checks; 29 runtime delivery checks; memory-probe regression checks; unchanged executable GPU shader programs; package integrity/privacy checks; live local menu/lobby checks and subsequent player performance testing. Improvement is player-reported on the tested setup; no fixed frame-rate or universal long-session/audio guarantee is made.

Fresh install: extract the complete **FarFarVR-v0.4.18.zip** and open **Far Far VR.exe** with the game closed. Existing users can use **Updates**. The separate runtime delta is a verified setup component; it is not the complete playable package.
