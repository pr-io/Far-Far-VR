-- Only filter the VR right-stick vertical axis. All buttons, triggers,
-- shoulders (including LB+RB), horizontal turning and left stick pass through.
-- Start tap/hold is already handled by UEVR (Start / Back at 500 ms).
local vr = uevr.params.vr
local engage = 0.95
local release = 0.85
local direction = 0
local mounted = false
uevr.sdk.callbacks.on_pre_engine_tick(function()
    local pc=uevr.api:get_player_controller(0)
    local pawn=pc and pc.Pawn
    local target=pc and pc:GetViewTarget()
    mounted=(pawn~=nil and pawn:get_full_name():find('BP_Mount_C ',1,true)~=nil)
        or (target~=nil and target:get_full_name():find('BP_Mount_C ',1,true)~=nil)
end)
uevr.sdk.callbacks.on_script_reset(function() mounted=false; direction=0 end)

uevr.sdk.callbacks.on_xinput_get_state(function(retval, user_index, state)
    if user_index ~= vr.get_lowest_xinput_index() or not vr.is_using_controllers() then
        direction = 0
        return
    end
    if mounted or uevr.params.functions.is_drawing_ui() then
        direction = 0
        return
    end
    local pad = state.Gamepad
    local y = pad.sThumbRY / 32767
    if direction == 1 and y < release then direction = 0 end
    if direction == -1 and y > -release then direction = 0 end
    if direction == 0 then
        if y >= engage then direction = 1
        elseif y <= -engage then direction = -1 end
    end
    pad.sThumbRY = direction * 32767
end)

print('[Far Far VR Input] Right-stick up/down: engage 95%, release 85%; other controls preserved.')
