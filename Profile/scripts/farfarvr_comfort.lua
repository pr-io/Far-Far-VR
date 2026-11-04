-- Far Far VR by PR_VR (https://www.youtube.com/@PR_VR)
-- Powered by UEVR created by praydog
-- Context-specific comfort, with recovery if UEVR saves during an override.
local api,vr=uevr.api,uevr.params.vr
local health=require('farfarvr_health')
local retry=0
local saved,mode,fault=nil,'gameplay',nil
local journal='farfarvr-comfort-restore.txt'
local allowed={VR_MovementOrientation=true,VR_AimMethod=true,UI_FollowView=true,VR_DecoupledPitchUIAdjust=true,VR_2DScreenMode=true,VR_RoomscaleMovement=true,VR_NativeStereoFix=true,VR_DecoupledPitch=true}
local overridden={VR_MovementOrientation='0',VR_AimMethod='0',UI_FollowView='false',VR_DecoupledPitchUIAdjust='false',VR_2DScreenMode='true',VR_RoomscaleMovement='false',VR_NativeStereoFix='false',VR_DecoupledPitch='false'}
local baseline={}
local function recover()
    vr.set_mod_value('VR_Compatibility_SplitScreen','false')
    local ok,data=pcall(fs.read,journal)
    if ok and type(data)=='string' then
        for key,value in data:gmatch('([%w_]+)=([^\r\n]+)') do
            if allowed[key] and (value=='true' or value=='false' or value:match('^[0-5]$')) then
                baseline[key]=value
                if vr:get_mod_value(key)==overridden[key] then vr.set_mod_value(key,value) end
            end
        end
        if (baseline.VR_AimMethod or baseline.VR_RoomscaleMovement) and baseline.VR_2DScreenMode==nil then
            vr.set_mod_value('VR_2DScreenMode','false')
        end
    end
end
recover()
local function snapshot_baseline()
    local changed=false
    for key in pairs(allowed) do
        local value=vr:get_mod_value(key)
        if baseline[key]~=value then baseline[key]=value; changed=true end
    end
    return changed
end
snapshot_baseline()
local function checkpoint()
    local lines={}
    for key,value in pairs(baseline) do lines[#lines+1]=key..'='..value end
    fs.write(journal,table.concat(lines,'\n'))
end
local function restore()
    if not saved then return end
    for key,value in pairs(saved) do vr.set_mod_value(key,value) end
    saved=nil; mode='gameplay'
end
_G.farfarvr_preferences={
    handles=function(key) return allowed[key]==true end,
    get=function(key) if saved and saved[key]~=nil then return saved[key] end; return vr:get_mod_value(key) end,
    set=function(key,value)
        assert(allowed[key],'Unknown preference')
        baseline[key]=value
        if saved and saved[key]~=nil then saved[key]=value else vr.set_mod_value(key,value) end
        checkpoint()
    end,
    save=function()
        -- UEVR serializes on a later frame. The native save boundary applies
        -- this validated journal without changing effective camera values.
        checkpoint()
        vr.save_config()
    end
}
local camera_class
local function flag(obj,key)
    return obj~=nil and obj:get_class():find_property(key)~=nil and obj[key]==true
end
local function cinematic(pc,pawn,target)
    if flag(pc,'bCinematicMode') or flag(pawn,'isInCinematic') then return true end
    camera_class=camera_class or api:find_uobject('Class /Script/Engine.CameraActor')
    if not (target~=nil and target~=pawn and camera_class~=nil and target:is_a(camera_class)) then return false end
    local name=target:get_full_name()
    if name:find('CameraShake',1,true) or name:find('Shake',1,true) or name:find('BP_Item',1,true) or name:find('/Game/Player/',1,true) then
        return false
    end
    return true
end
local function context(pc)
    if not vr.is_hmd_active() then return 'idle' end
    if not pc then return 'loading' end
    local pawn=pc.Pawn
    local name=pawn and pawn:get_full_name() or ''
    local target=pc:GetViewTarget()
    if cinematic(pc,pawn,target) then return 'cinematic' end
    if name:find('BP_LoadingScreen_C ',1,true) or (target and target:get_full_name():find('BP_LoadingScreen_C ',1,true)) then return 'map_loading' end
    if pc.bShowMouseCursor or not pawn or name:find('BP_TitleScreen_C ',1,true) then return 'menu' end
    if name:find('BP_Mount_C ',1,true) or (target and target:get_full_name():find('BP_Mount_C ',1,true)) then return 'riding' end
    if flag(pawn,'isPlayingEmote') then return 'emote' end
    return 'gameplay'
end
local function update(pc)
    local next_mode=context(pc)
    if next_mode==mode then
        if mode=='gameplay' and snapshot_baseline() then checkpoint() end
        health.set('Comfort',mode)
        return
    end
    restore()
    snapshot_baseline()
    mode=next_mode
    health.set('Comfort',mode)
    if mode~='gameplay' then
        saved={VR_AimMethod=vr:get_mod_value('VR_AimMethod')}
        if mode=='riding' then saved.VR_MovementOrientation=vr:get_mod_value('VR_MovementOrientation') end
        if mode~='riding' and mode~='emote' then
            saved.UI_FollowView=vr:get_mod_value('UI_FollowView')
            saved.VR_DecoupledPitchUIAdjust=vr:get_mod_value('VR_DecoupledPitchUIAdjust')
        end
        if mode=='cinematic' or mode=='map_loading' then
            saved.VR_2DScreenMode=vr:get_mod_value('VR_2DScreenMode')
            saved.VR_RoomscaleMovement=vr:get_mod_value('VR_RoomscaleMovement')
        end
        if mode=='map_loading' then saved.VR_DecoupledPitch=vr:get_mod_value('VR_DecoupledPitch') end
        if mode=='cinematic' or mode=='map_loading' or mode=='idle' then
            saved.VR_NativeStereoFix=vr:get_mod_value('VR_NativeStereoFix')
        end
        checkpoint()
        vr.set_mod_value('VR_AimMethod','0')
        if mode=='riding' then vr.set_mod_value('VR_MovementOrientation','0') end
        if mode~='riding' and mode~='emote' then
            vr.set_mod_value('UI_FollowView','false')
            vr.set_mod_value('VR_DecoupledPitchUIAdjust','false')
        end
        if mode=='cinematic' or mode=='map_loading' or mode=='idle' then
            -- Keep the accepted mono crop/letterbox path exactly as before.
            -- The independent-eye correction is needed for combat, not movies.
            -- Suspending it while OpenXR is idle also avoids capture churn.
            vr.set_mod_value('VR_NativeStereoFix','false')
        end
        if mode=='map_loading' then
            -- CamMain owns the train composition, including its authored pitch.
            -- Flattening that pitch puts the train above the viewer.
            vr.set_mod_value('VR_DecoupledPitch','false')
        end
        if mode=='cinematic' or mode=='map_loading' then
            vr.set_mod_value('VR_2DScreenMode','true')
            vr.set_mod_value('VR_RoomscaleMovement','false')
        end
    end
end
-- Keep TAA's temporal filter, but use spatial output scaling for the two
-- independently rendered eyes. Temporal upsampling can retain the packed
-- viewport history; this is a candidate guard for the reported combat crop.
-- Do not change AA choice,
-- render resolution, TSR, FXAA, or post-process materials.
local taa_override,taa_status,taa_retry=nil,'Waiting for console variables',0
local function restore_taa()
    if taa_override then
        local previous=taa_override
        taa_override=nil
        if previous.variable:get_int()==0 then previous.variable:set_int(previous.original) end
    end
end
local taa_method,taa_upscale
local function update_taa()
    -- No graphics CVar access is needed while the headset/runtime is idle.
    if mode=='idle' then restore_taa(); taa_status='Original AA scaling'; return end
    if not taa_method or not taa_upscale then
        local manager=api:get_console_manager()
        taa_method=manager and manager:find_variable('r.AntiAliasingMethod')
        taa_upscale=manager and manager:find_variable('r.TemporalAA.Upsampling')
    end
    local method,upscale=taa_method,taa_upscale
    if not method or not upscale then taa_status='TAA scaling guard unavailable'; return end
    local active=method:get_int()==2 and vr:get_mod_value('VR_NativeStereoFix')=='true'
        and mode~='idle' and mode~='cinematic' and mode~='map_loading'
    if active then
        if not taa_override then taa_override={variable=upscale,original=upscale:get_int()} end
        if upscale:get_int()~=0 then upscale:set_int(0) end
        assert(upscale:get_int()==0,'TAA spatial output scaling did not apply')
        taa_status='TAA retained; spatial output scaling'
    else restore_taa(); taa_status='Original AA scaling' end
end
uevr.sdk.callbacks.on_pre_engine_tick(function(_,dt)
    retry=math.max(0,retry-(dt or .016)); if retry>0 then return end
    fault=nil
    local ok,err=pcall(function()
        update(api:get_player_controller(0))
        -- A rejected graphics CVar must not disable camera/context handling.
        taa_retry=math.max(0,taa_retry-(dt or .016))
        if taa_retry==0 then
            local taa_ok,taa_error=pcall(update_taa)
            if not taa_ok then
                taa_retry=1
                taa_method,taa_upscale=nil,nil
                pcall(restore_taa)
                taa_status='ERROR in TAA scaling guard: '..tostring(taa_error)
                fs.write('farfarvr-taa-status.txt',taa_status)
            end
        end

    end)
    if not ok then fault=tostring(err); pcall(restore); pcall(restore_taa); retry=1; health.set('Comfort','Recovering: '..fault); pcall(fs.write,'farfarvr-comfort-status.txt','ERROR: '..fault) end
end)
uevr.sdk.callbacks.on_post_engine_tick(function()
    if retry>0 then return end
    local ok,err=pcall(function()
        update(api:get_player_controller(0))
        -- Game settings/camera effects can rewrite CVars during Tick. Recheck
        -- before rendering so that rewrite cannot bypass the TAA guard for a frame.
        if taa_retry==0 then
            local taa_ok,taa_error=pcall(update_taa)
            if not taa_ok then
                taa_retry=1
                taa_method,taa_upscale=nil,nil
                pcall(restore_taa)
                taa_status='ERROR in TAA scaling guard: '..tostring(taa_error)
                fs.write('farfarvr-taa-status.txt',taa_status)
            end
        end
    end)
    if not ok then fault=tostring(err); pcall(restore); pcall(restore_taa); retry=1; health.set('Comfort','Recovering: '..fault); pcall(fs.write,'farfarvr-comfort-status.txt','ERROR after tick: '..fault) end
end)
uevr.sdk.callbacks.on_script_reset(function()
    restore()
    restore_taa()
    for key,value in pairs(baseline) do vr.set_mod_value(key,value) end
    checkpoint()
    _G.farfarvr_preferences=nil
end)
