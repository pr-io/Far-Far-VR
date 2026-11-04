-- Far Far VR by PR_VR. Stereo corrections restore on exit.
-- Temporal AA scaling is managed exclusively by farfarvr_comfort.lua.
local api,vr=uevr.api,uevr.params.vr
local health=require('farfarvr_health')
local originals,cached={},{}
local elapsed=3
local fields={
    {'r.RefractionQuality'}, {'r.Distortion'}, {'r.SceneColorFringeQuality'},
    {'r.MotionBlurQuality'}, {'r.MotionBlur.Amount',true},
    {'r.DepthOfFieldQuality'}, {'r.DepthOfField.MaxSize',true},
    {'r.LensFlareQuality'}, {'r.SSR.Quality'}, {'r.FilmGrain',true},
    {'r.Tonemapper.GrainQuantization'}, {'r.DynamicRes.OperationMode'}
}
local function restore()
    for name,old in pairs(originals) do
        local ok=pcall(function()
            local var=cached[name]
            local value=old.float and var:get_float() or var:get_int()
            if value==0 then if old.float then var:set_float(old.value) else var:set_int(old.value) end end
        end)
        if ok then originals[name]=nil end
    end
end
local function active()
    return vr.is_runtime_ready() and vr.is_hmd_active()
        and vr:get_mod_value('VR_NativeStereoFix')=='true'
        and vr:get_mod_value('VR_2DScreenMode')~='true'
end
local function apply()
    local manager=api:get_console_manager()
    if not manager then health.set('Render','Waiting for graphics settings');return end
    local count=0
    for _,field in ipairs(fields) do
        local name,is_float=field[1],field[2]
        local var=cached[name] or manager:find_variable(name)
        if var then
            cached[name]=var
            local value=is_float and var:get_float() or var:get_int()
            if originals[name]==nil then originals[name]={value=value,float=is_float} end
            if value~=0 then if is_float then var:set_float(0) else var:set_int(0) end end
            local actual=is_float and var:get_float() or var:get_int()
            assert(actual==0,'Graphics correction rejected: '..name)
            count=count+1
        end
    end
    health.set('Render',count==#fields and 'Active' or 'Active; some optional graphics settings unavailable')
end
uevr.sdk.callbacks.on_post_engine_tick(function(_,dt)
    local ok,err=pcall(function()
        if not active() then restore();elapsed=3;health.set('Render','Standby');return end
        elapsed=elapsed+(dt or .016)
        if elapsed>=3 then elapsed=0;apply() end
    end)
    if not ok then health.set('Render','Recovering: '..tostring(err));elapsed=2 end
end)
uevr.sdk.callbacks.on_script_reset(restore)
