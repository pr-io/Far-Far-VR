# Menu input correction - October 31, 2026

Head-relative locomotion was also rotating menu navigation while the menu camera used game orientation. Native input now bypasses that rotation on title/notice, cursor selection, emote selection, the Far Far VR menu, and cinematic screens. Gameplay HMD-relative locomotion remains enabled. The existing horse/emote-only right-stick vertical inversion is unchanged.

A now switches laser selection to native gamepad confirmation, stopping pointer mouse movement until a trigger reactivates the laser or a new page opens. A remains mapped to the game A button; saved controller bindings are unchanged.

The launcher also ignores exited game processes instead of reporting them as a game running from a different folder. Launch remains manual through START IN VR.

All eight gameplay Lua modules are byte-identical to the pre-change snapshot, including the separate gun crosshair, hit-X layer, and empty-hand pointer. No saved settings or bindings were changed.

Validation: backend and launcher builds passed; 85 profile tests; 33 native tests; pointer and shortcut policy tests passed. Physical controller acceptance is pending; do not describe these checks as a headset test.
