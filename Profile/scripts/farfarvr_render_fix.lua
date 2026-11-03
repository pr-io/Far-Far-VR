-- Far Far VR by PR_VR (https://www.youtube.com/@PR_VR)
-- Powered by UEVR created by praydog
-- Far Far VR Render Fix
-- Disables broken screen-space refraction, distortion, chromatic aberration,
-- motion blur, depth-of-field, lens flares, screen-space reflections, film grain,
-- and dynamic resolution in Native Stereo for optimal VR comfort and stereo visual integrity.
local api = uevr.api
local vr = uevr.params.vr

local waiting_reported = false

local function log_status(msg)
    print('[Far Far VR Render Fix] ' .. msg)
end

local vars_to_disable = {
    'r.RefractionQuality',           -- Screen-space refraction (causes stereo left/right eye split)
    'r.Distortion',                  -- Screen-space distortion (causes mismatched eye warp)
    'r.SceneColorFringeQuality',      -- Chromatic aberration (conflicts with VR headset lens optics)
    'r.MotionBlurQuality',           -- Motion blur quality (causes severe VR simulator sickness)
    'r.MotionBlur.Amount',           -- Motion blur intensity
    'r.DepthOfFieldQuality',         -- 2D depth of field (blurs stereoscopic vision unnaturally)
    'r.DepthOfField.MaxSize',        -- Depth of field maximum blur size
    'r.LensFlareQuality',            -- 2D lens flares (float unnaturally in stereo space)
    'r.SSR.Quality',                 -- Screen-space reflections (causes left/right eye shimmering)
    'r.FilmGrain',                   -- Film grain noise (causes static dancing noise on HMD displays)
    'r.Tonemapper.GrainQuantization', -- Tonemapper grain quantization
    'r.DynamicRes.OperationMode',    -- Dynamic resolution (causes projection jitter and compositor hitches)
    'r.TemporalAA.Upsampling'        -- Temporal AA upsampling (causes viewport history artifacts)
}

local cached_vars = {}
local check_elapsed = 0
local initialized = false

local function apply_fixes()
    local manager = api:get_console_manager()
    if not manager then
        if not waiting_reported then log_status('Waiting for console manager...'); waiting_reported = true end
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
        log_status(string.format('Render fix initialized: %d VR-breaking CVars enforced to 0', applied_count))
    end
    return applied_count == #vars_to_disable
end

-- Apply immediately on initial script load
apply_fixes()

-- Keep CVars enforced across level transitions and post-process resets (every 3 seconds)
if uevr and uevr.sdk and uevr.sdk.callbacks and uevr.sdk.callbacks.on_post_engine_tick then
    uevr.sdk.callbacks.on_post_engine_tick(function(_, dt)
        check_elapsed = check_elapsed + (dt or 0.016)
        if check_elapsed >= 3.0 then
            check_elapsed = 0
            apply_fixes()
        end
    end)
end
