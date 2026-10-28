-- Far Far VR Render Fix
-- Disables broken screen-space refraction and distortion in Native Stereo (SBS)
local api = uevr.api
local vr = uevr.params.vr

local manager = api:get_console_manager()
local history = {}

local function log_status(msg)
    table.insert(history, msg)
    fs.write('farfarvr-render-fix-status.txt', table.concat(history, '\n'))
    print('[Far Far VR Render Fix] ' .. msg)
end

if not manager then
    log_status('ERROR: Console manager unavailable')
    return
end

local vars_to_disable = {
    'r.RefractionQuality',
    'r.Distortion',
    'r.SceneColorFringeQuality'
}

local applied_count = 0

for _, name in ipairs(vars_to_disable) do
    local var = manager:find_variable(name)
    if var then
        local old_val = var:get_int()
        var:set_int(0)
        local new_val = var:get_int()
        log_status(string.format('Applied %s: %d -> %d', name, old_val, new_val))
        applied_count = applied_count + 1
    else
        log_status(string.format('CVar %s not found (may not be registered)', name))
    end
end

log_status(string.format('Render fix initialized: %d CVars set to 0', applied_count))
