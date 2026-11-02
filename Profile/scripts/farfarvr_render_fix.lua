-- Far Far VR Render Fix
-- Disables broken screen-space refraction, distortion, chromatic aberration,
-- motion blur, depth-of-field, and lens flares in Native Stereo (SBS) for optimal VR performance and comfort.
local api = uevr.api
local vr = uevr.params.vr

local history = {}

local function log_status(msg)
    table.insert(history, msg)
    if fs and fs.write then
        fs.write('farfarvr-render-fix-status.txt', table.concat(history, '\n'))
    end
    print('[Far Far VR Render Fix] ' .. msg)
end

local vars_to_disable = {
    'r.RefractionQuality',
    'r.Distortion',
    'r.SceneColorFringeQuality',
    'r.MotionBlurQuality',
    'r.DepthOfFieldQuality',
    'r.LensFlareQuality'
}

local cached_vars = {}
local check_elapsed = 0
local initialized = false

local function apply_fixes()
    local manager = api:get_console_manager()
    if not manager then
        if not initialized then log_status('Waiting for console manager...') end
        return false
    end

    local applied_count = 0
    for _, name in ipairs(vars_to_disable) do
        local var = cached_vars[name]
        if not var then
            var = manager:find_variable(name)
            if var then cached_vars[name] = var end
        end

        if var then
            local current_val = var:get_int()
            if current_val ~= 0 then
                var:set_int(0)
                local new_val = var:get_int()
                log_status(string.format('Applied %s: %d -> %d', name, current_val, new_val))
            end
            applied_count = applied_count + 1
        end
    end

    if not initialized and applied_count > 0 then
        initialized = true
        log_status(string.format('Render fix initialized: %d/%d CVars set to 0', applied_count, #vars_to_disable))
    end
    return applied_count == #vars_to_disable
end

-- Apply immediately on initial script load
apply_fixes()

-- Keep CVars enforced across level transitions and post-process resets (every 3 seconds)
if uevr and uevr.sdk and uevr.sdk.callbacks and uevr.sdk.callbacks.on_post_engine_tick then
    uevr.sdk.callbacks.on_post_engine_tick(function(_, dt)
        check_elapsed = check_elapsed + (dt or 0.016)
        if check_elapsed >= 3.0 or not initialized then
            check_elapsed = 0
            apply_fixes()
        end
    end)
end
