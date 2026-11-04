-- Far Far VR by PR_VR (https://www.youtube.com/@PR_VR)
-- Powered by UEVR created by praydog
-- Preserve the established gameplay shortcut filter (melee at 95% up, utility
-- at 95% down). During active emote playback, disable right-stick action inputs
-- from reaching Enhanced Input while smoothly rotating camera pitch. Horizontal
-- camera rotation, movement, and other controls remain unchanged.
local api,vr=uevr.api,uevr.params.vr
local engage,release=0.95,0.85
local direction=0
local mounted,playing,wheel_open=false,false,false
local passthrough=true
local orbit_y,input_age=0,1
local health=require('farfarvr_health')
local refresh_retry=0

local function refresh(pc)
    pc=pc or api:get_player_controller(0)
    local pawn=pc and pc.Pawn
    local target=pc and pc:GetViewTarget()
    local name=pawn and pawn:get_full_name() or ''
    passthrough=not pc or not pawn or pc.bShowMouseCursor==true
        or name:find('BP_TitleScreen_C ',1,true)~=nil
        or name:find('BP_LoadingScreen_C ',1,true)~=nil
        or vr:get_mod_value('VR_2DScreenMode')=='true'
    if passthrough then
        mounted,playing,wheel_open=false,false,false
        orbit_y=0
        return
    end
    playing=pawn~=nil and pawn:get_class():find_property('isPlayingEmote')~=nil and pawn.isPlayingEmote==true
    local wheel=pawn and pawn:get_class():find_property('ui_Emote') and pawn.ui_Emote
    wheel_open=wheel~=nil and wheel~=false and wheel:IsInViewport() and wheel:IsVisible()
    mounted=name:find('BP_Mount_C ',1,true)~=nil
        or (target~=nil and target:get_full_name():find('BP_Mount_C ',1,true)~=nil)
    if not playing or wheel_open then orbit_y=0 end
end

local function safe_refresh(pc)
    if refresh_retry>0 then return false end
    local ok,err=pcall(refresh,pc)
    if not ok then
        passthrough=true;mounted,playing,wheel_open=false,false,false
        direction=0;orbit_y=0;input_age=1;refresh_retry=1
        health.set('Input','Recovering: '..tostring(err))
    else health.set('Input',passthrough and 'Standby' or 'Active') end
    return ok
end

uevr.sdk.callbacks.on_pre_engine_tick(function(_,dt)
    refresh_retry=math.max(0,refresh_retry-(dt or .016))
    local pc=api:get_player_controller(0)
    if not safe_refresh(pc) then return end
    input_age=input_age+(dt or 0.01)
    if playing and not wheel_open and not mounted and not passthrough
        and not uevr.params.functions.is_drawing_ui() and vr.is_using_controllers() and input_age<0.1 and pc then
        local y=math.abs(orbit_y)>0.2 and orbit_y or 0
        if y~=0 then
            local rot=pc:GetControlRotation()
            if rot then
                local pitch=(rot.Pitch+180)%360-180
                rot.Pitch=math.max(-89,math.min(89,pitch-y*90*math.min(dt or 0.01,0.05)))
                pc:SetControlRotation(rot)
            end
        end
    end
end)

uevr.sdk.callbacks.on_post_engine_tick(function() safe_refresh(api:get_player_controller(0)) end)

uevr.sdk.callbacks.on_script_reset(function()
    mounted,playing,wheel_open=false,false,false
    passthrough=true; direction=0
    orbit_y,input_age=0,1
end)

uevr.sdk.callbacks.on_xinput_get_state(function(retval,user_index,state)
    if user_index~=vr.get_lowest_xinput_index() or not vr.is_using_controllers()
        or passthrough or wheel_open or uevr.params.functions.is_drawing_ui() then
        direction=0
        orbit_y=0
        input_age=1
        return
    end
    local pad=state.Gamepad
    if playing and not wheel_open then
        direction=0
        orbit_y=pad.sThumbRY/32767
        input_age=0
        pad.sThumbRY=0
        return
    end
    orbit_y=0
    if mounted then
        direction=0
        pad.sThumbRY=math.max(-32768,math.min(32767,-pad.sThumbRY))
        return
    end
    local y=pad.sThumbRY/32767
    if direction==1 and y<release then direction=0 end
    if direction==-1 and y>-release then direction=0 end
    if direction==0 then
        if y>=engage then direction=1
        elseif y<=-engage then direction=-1 end
    end
    pad.sThumbRY=direction*32767
end)

print('[Far Far VR Input] Emote pitch isolation (no melee/utility triggers); native 95% shortcuts restored on exit.')
