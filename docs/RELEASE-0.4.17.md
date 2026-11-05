# Far Far VR v0.4.17

Removes normal HUD/effects status-file reporting added during recent repairs. Crosshair stereo-view callbacks no longer build status messages or write status files. Effects callbacks no longer poll their render target for diagnostic reporting. Error recovery and in-menu health information remain.

Retains the complete v0.4.16 native backend and runtime delta, including the earlier render-thread memory-validation optimization. Crosshair targeting, private textures, original animated effects, controls, settings, calibration and all other gameplay modules remain unchanged.

The temporary hitch-capture setting enabled during investigation was restored to its original disabled state before any game launch. No additional monitoring is included.

No local test suites or game/headset sessions were run. Static source and package inspection establish removal of this reporting work; they do not establish that the reported progressive slowdown or audio stutter is fully resolved.

Fresh install: extract the complete **FarFarVR-v0.4.17.zip** and open **Far Far VR.exe** with the game closed. Existing users can use Updates.
