-- Keep flat-game horse orbit/tilt separate from UEVR's tracked head looking.
local api=uevr.api
local state,fault,elapsed=nil,nil,0
local function alive(o) return o~=nil and UEVR_UObjectHook.exists(o) end
local function rot(p,y,r)
    local value=StructObject.new(assert(api:find_uobject('ScriptStruct /Script/CoreUObject.Rotator')))
    value.Pitch=p; value.Yaw=y; value.Roll=r
    return value
end
local function restore()
    local s=state; state=nil
    if not s then return end
    if alive(s.arm) then pcall(function()
        for k,v in pairs(s.arm_flags) do s.arm[k]=v end
        s.arm:K2_SetRelativeRotation(rot(s.arm_rotation.Pitch,s.arm_rotation.Yaw,s.arm_rotation.Roll),false,{},true)
    end) end
    if alive(s.camera) then pcall(function()
        s.camera.bUsePawnControlRotation=s.camera_control
        s.camera:K2_SetRelativeRotation(rot(s.camera_rotation.Pitch,s.camera_rotation.Yaw,s.camera_rotation.Roll),false,{},true)
    end) end
end
local function mounted(pc)
    if not pc then return nil end
    local pawn=pc.Pawn
    if pawn and pawn:get_full_name():find('BP_Mount_C ',1,true) then return pawn end
    local target=pc:GetViewTarget()
    if target and target:get_full_name():find('BP_Mount_C ',1,true) then return target end
end
local function update(pc)
    if uevr.params.vr:get_mod_value('VR_2DScreenMode')=='true' then restore(); return end
    local pawn=mounted(pc)
    if not pawn then restore(); return end
    if state and state.pawn~=pawn then restore() end
    if not state then
        local arm,camera=assert(pawn.SpringArm,'Mounted spring arm unavailable'),assert(pawn.Camera,'Mounted camera unavailable')
        local ar,cr=arm.RelativeRotation,camera.RelativeRotation
        state={pawn=pawn,arm=arm,camera=camera,
            arm_flags={bUsePawnControlRotation=arm.bUsePawnControlRotation,bInheritPitch=arm.bInheritPitch,
                bInheritRoll=arm.bInheritRoll,bDoCollisionTest=arm.bDoCollisionTest},
            arm_rotation={Pitch=ar.Pitch,Yaw=ar.Yaw,Roll=ar.Roll},
            camera_control=camera.bUsePawnControlRotation,
            camera_rotation={Pitch=cr.Pitch,Yaw=cr.Yaw,Roll=cr.Roll}}
    end
    local s=state
    -- Comfort selects game-camera aim (0) while mounted, so control rotation
    -- comes from the stick, not the tracked controller. Let the native boom
    -- consume that rotation; flattening it every tick also erased stick look.
    s.arm.bUsePawnControlRotation=true
    s.arm.bInheritPitch=true; s.arm.bInheritRoll=false
    s.arm.bDoCollisionTest=true
    s.camera.bUsePawnControlRotation=false
    -- Do not add HMD/controller rotations, or alter distance, yaw, FOV or sprint.
    s.camera:K2_SetRelativeRotation(rot(0,s.camera_rotation.Yaw,0),false,{},true)
end
local function step(dt)
    if fault then return end
    local ok,err=pcall(function()
        update(api:get_player_controller(0)); elapsed=elapsed+dt
        if elapsed>3 then
            elapsed=0
            fs.write('farfarvr-mount-status.txt',state and 'Mounted: native stick orbit; controller aim disabled by comfort; camera collision enabled; distance/FOV unchanged' or 'Not mounted; original camera properties restored')
        end
    end)
    if not ok then fault=tostring(err); restore(); fs.write('farfarvr-mount-status.txt','ERROR: '..fault) end
end
uevr.sdk.callbacks.on_pre_engine_tick(function(_,dt) step(dt) end)
uevr.sdk.callbacks.on_post_engine_tick(function() step(0) end)
uevr.sdk.callbacks.on_script_reset(restore)
