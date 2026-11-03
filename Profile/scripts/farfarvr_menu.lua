-- Far Far VR by PR_VR (https://www.youtube.com/@PR_VR)
-- Powered by UEVR created by praydog
-- Far Far VR settings UI. Rendering, poses and bindings remain owned by UEVR.
local startup_ok, startup_error = pcall(function()
local vr = uevr.params.vr
local fields = require('farfarvr_settings')
local groups = {'Welcome', 'Comfort', 'Calibration', 'Input', 'HUD', 'Display', 'Updates'}
local labels={HUD='Interface', Input='Controls'}
local page, baseline, was_open, failed = 'Welcome', nil, false, nil
-- Show the controls diagram only when requested from the Controls tab.
local show_controls_popup, popup_closed = false, true
local status = 'Changes apply live. Save to keep them for your next session.'
local current_version = 'v0.4.5'
local update_status, update_version, update_notes = 'Not checked yet.', current_version, ''
local update_state, update_poll_elapsed = 'idle', 0
local by_key = {}
for _, field in ipairs(fields) do by_key[field.key] = field end
local function finite(v) return type(v)=='number' and v==v and v~=math.huge and v~=-math.huge end
local session_cache = {}
pcall(function()
    local content
    if fs and fs.read then content = fs.read('config.txt') end
    if (not content or content == '') and io and io.open then
        local f = io.open('config.txt', 'r')
        if f then content = f:read('*a'); f:close() end
    end
    if content and type(content) == 'string' then
        for line in content:gmatch('[^\r\n]+') do
            local k, v = line:match('^([^=]+)=(.*)$')
            if k and v and by_key[k] then
                local f = by_key[k]
                if f.kind == 'bool' then
                    if v == 'true' then session_cache[k] = true
                    elseif v == 'false' then session_cache[k] = false end
                else
                    local num = tonumber(v)
                    if finite(num) then session_cache[k] = num end
                end
            end
        end
    end
end)
local function is_runtime_setting(field)
    return field.key:sub(1, 7) == 'OpenXR_' or field.runtime == true
end
local function read(field)
    local raw = vr:get_mod_value(field.key)
    if field.kind == 'bool' then
        if raw == 'true' then return true end
        if raw == 'false' then return false end
        if is_runtime_setting(field) then
            if session_cache[field.key] ~= nil then return session_cache[field.key] end
            if field.default ~= nil then
                local def = (field.default == true or field.default == 'true')
                session_cache[field.key] = def
                return def
            end
        end
        assert(false, 'Unavailable setting: '..field.label)
    end
    local value = tonumber(raw)
    if finite(value) then return value end
    if is_runtime_setting(field) then
        if session_cache[field.key] ~= nil then return session_cache[field.key] end
        local def = tonumber(field.default)
        if finite(def) then
            session_cache[field.key] = def
            return def
        end
    end
    assert(false, 'Unavailable setting: '..field.label)
end
local function write(field, value, restore)
    if field.kind=='bool' then assert(type(value)=='boolean', 'Invalid toggle value')
    else
        assert(finite(value), 'Invalid numeric value')
        if not restore then
            local min,max=field.min,field.max
            if field.kind=='choice' then min,max=0,#field.choices-1 end
            assert(value>=min and value<=max, 'Setting is outside its supported range')
        end
        if field.kind=='int' or field.kind=='choice' then value=math.floor(value+.5) end
    end
    if is_runtime_setting(field) then session_cache[field.key] = value end
    pcall(function() vr.set_mod_value(field.key, tostring(value)) end)
    local actual = read(field)
    if not (actual==value or (finite(actual) and math.abs(actual-value)<.0001)) then
        if is_runtime_setting(field) then session_cache[field.key] = actual end
        assert(false, 'Runtime did not apply '..field.label)
    end
end
local function snapshot()
    local result={}
    for _,field in ipairs(fields) do result[field.key]=read(field) end
    return result
end
local function safe(fn)
    local ok,err=pcall(fn)
    if not ok then failed=tostring(err); print('[FarFarVR] Menu error: '..failed) end
end
local function headset_ready()
    if vr.is_runtime_ready() and vr.is_hmd_active() then return true end
    status='Connect your headset to PCVR before calibrating.'
    return false
end
local function calibrate(height_only)
    if not headset_ready() then return end
    local position,rotation,origin=UEVR_Vector3f.new(),UEVR_Quaternionf.new(),UEVR_Vector3f.new()
    vr.get_pose(vr.get_hmd_index(),position,rotation)
    vr.get_standing_origin(origin)
    assert(finite(position.x) and finite(position.y) and finite(position.z), 'Headset pose is unavailable')
    if height_only then origin.y=position.y else origin.x=position.x; origin.y=position.y; origin.z=position.z end
    vr.set_standing_origin(origin)
    status=height_only and 'Current height calibrated for this session.' or 'Current position calibrated for this session.'
end
local function poll_update_status()
    if not fs or not fs.read then return end
    local ok, content = pcall(fs.read, 'farfarvr-updates.txt')
    if not ok or type(content) ~= 'string' or content == '' then return end
    update_state = content:match('state=([^\r\n]+)') or update_state
    update_version = content:match('version=([^\r\n]+)') or update_version
    local notes = content:match('notes:\n(.*)')
    if notes then update_notes = notes:gsub('\r', '') end
    local messages = {
        checking='Checking for updates...',
        downloading='Downloading update package...',
        available='Update Available: '..update_version,
        ['up-to-date']='Up to date',
        prepared='Update prepared. Exit the game and reopen the launcher to install it.',
        applied='Update installed.',
        error='Update check failed: '..update_notes
    }
    update_status = messages[update_state] or update_status
end
local function request_update(action)
    if not fs or not fs.write then
        update_status = 'Launcher update service is unavailable.'
        return
    end
    local ok, err = pcall(fs.write, 'farfarvr-update-command.txt', action)
    if ok then
        update_state = action == 'check' and 'checking' or 'downloading'
        update_status = action == 'check' and 'Checking for updates...' or 'Downloading update package...'
    else
        update_status = 'Could not contact the launcher: '..tostring(err)
    end
end
uevr.sdk.callbacks.on_frame(function()
    if failed then return end
    update_poll_elapsed = update_poll_elapsed + 1
    if update_poll_elapsed >= 60 then
        update_poll_elapsed = 0
        poll_update_status()
    end
    local open=uevr.params.functions.is_drawing_ui()
    if open and not was_open then safe(function() baseline=snapshot() end) end
    was_open=open
end)
local function packed(n) return n>=0x80000000 and n-0x100000000 or n end
-- Wrap copy inside the available panel instead of clipping at its right edge.
local function text(value)
    if imgui.text_wrapped then imgui.text_wrapped(value) else imgui.text(value) end
end
local function draw()
    safe(function()
        if not baseline then baseline=snapshot() end
        for i,name in ipairs(groups) do
            if i%3~=1 then imgui.same_line() end
            imgui.push_style_color(21,packed(page==name and 0xFF75BAEE or 0xFF302B25))
            imgui.push_style_color(0,packed(page==name and 0xFF171B20 or 0xFFE9E5DF))
            if imgui.button(labels[name] or name,{160,34}) then page=name end
            imgui.pop_style_color(2)
        end
        imgui.spacing()
        if failed then text('Settings unavailable: '..failed); return end
        if page=='Welcome' then
            text('Your next frontier.')
            imgui.spacing()
            text(vr.is_runtime_ready() and vr.is_hmd_active() and 'Far Far VR is ready. Return to game whenever you are ready.' or 'Your VR settings are available. Connect your headset to play.')
            text('Hold Start to show or hide this panel.')
            text('Quick Start keeps the normal in-game menu.')
            imgui.spacing();imgui.separator();imgui.spacing()
            text('MAKE VR FEEL RIGHT')
            text('Comfort: turning and movement. Calibration: height and scale.')
            text('Input: tracked aiming. Interface: HUD and this menu.')
            text('Display: frame rate and desktop mirror.')
            imgui.spacing()
            text('YOUR CONTROLS')
            text('Right stick click: map. Hold right stick: ping.')
            text('Both stick clicks: emote wheel. Hold both: battle cry.')
            text('A: confirm an emote. Y: switch weapon. Hold Y: talk.')
            text('On cursor screens: point and trigger to select. A stick switches to navigation.')
            imgui.spacing()
        elseif page=='Calibration' then
            text('Sit or stand naturally and look forward before calibrating.')
            if imgui.button('Calibrate current height',{230,36}) then calibrate(true) end
            imgui.same_line()
            if imgui.button('Recenter position',{210,36}) then calibrate(false) end
            if imgui.button('Reset height & camera',{230,36}) then
                for _,key in ipairs({'VR_CameraUpOffset','VR_CameraForwardOffset','VR_CameraRightOffset'}) do write(by_key[key],0) end
                status='Manual camera adjustments reset. Controller bindings are preserved.'
            end
            text('Height calibration lasts this session. Fine adjustments can be saved.')
            imgui.spacing()
        elseif page=='Input' then
            text('Your corrected Touch button mapping is preserved.')
            if imgui.button('Show Controls Popup', {180, 30}) then
                show_controls_popup = true
                popup_closed = false
            end
            imgui.spacing()
            if imgui.draw_controls_image then
                imgui.draw_controls_image(480, 270)
                imgui.spacing()
            end
        elseif page=='Comfort' then
            text('Choose how turning and movement feel.')
            imgui.spacing()
        elseif page=='Display' then
            text('Adjust VR resolution scale and spectator mirror.')
            text('Game quality, antialiasing and headset refresh rate stay in their own settings.')
            imgui.spacing()
        elseif page=='Updates' then
            poll_update_status()
            text('Current Version: '..current_version)
            text(update_status)
            imgui.spacing()
            if imgui.button('Check for Updates',{230,36}) then request_update('check') end
            if update_state=='available' then
                imgui.same_line()
                if imgui.button('Download & Prepare Update',{270,36}) then request_update('stage') end
            end
            imgui.spacing()
            text('Release Notes')
            if imgui.begin_child and imgui.end_child then
                local child_ok, child_visible = pcall(imgui.begin_child, 'FarFarVRReleaseNotes', {0, 170}, true)
                if child_ok then
                    if child_visible ~= false then
                        if imgui.text_wrapped then imgui.text_wrapped(update_notes ~= '' and update_notes or 'Check for updates to view release notes.')
                        else text(update_notes ~= '' and update_notes or 'Check for updates to view release notes.') end
                    end
                    imgui.end_child()
                else
                    text(update_notes ~= '' and update_notes or 'Check for updates to view release notes.')
                end
            else
                text(update_notes ~= '' and update_notes or 'Check for updates to view release notes.')
            end
            imgui.spacing()
        else
            text('Place the game interface and settings menu comfortably in VR.')
            imgui.spacing()
        end
        for _,field in ipairs(fields) do
            if field.group==page then
                local current=read(field)
                local changed,value
                imgui.set_next_item_width(220)
                if field.kind=='bool' then changed,value=imgui.checkbox(field.label,current)
                elseif field.kind=='choice' then
                    local choices={}; for i,label in ipairs(field.choices) do choices[i-1]=label end
                    changed,value=imgui.combo(field.label,current,choices)
                elseif field.kind=='int' then changed,value=imgui.slider_int(field.label,current,field.min,field.max)
                else changed,value=imgui.slider_float(field.label,current,field.min,field.max,'%.2f'..(field.unit and (' '..field.unit) or '')) end
                if field.description and imgui.is_item_hovered() then imgui.set_tooltip(field.description) end
                if changed then write(field,value); status='Applied. Save to keep your changes.' end
                imgui.spacing()
            end
        end
        imgui.spacing(); imgui.separator(); imgui.spacing()
        if imgui.button('Recenter view',{160,34}) and headset_ready() then vr.recenter_view(); status='Facing direction reset. Height is unchanged.' end
        imgui.same_line()
        if imgui.button('Save settings',{160,34}) then vr.save_config(); baseline=snapshot(); status='Save requested through the VR engine.' end
        if imgui.button("Revert this session's edits",{240,34}) then
            for _,field in ipairs(fields) do if read(field)~=baseline[field.key] then write(field,baseline[field.key],true) end end
            status='Restored settings from when you opened this menu or last saved.'
        end
        text(status)
        imgui.spacing()
        imgui.separator()
        imgui.spacing()
        text('Support me by subscribing to my channel: https://www.youtube.com/@PR_VR')
        text('For questions and support for this mod, join the VR Buddies Discord: https://discord.gg/PcsYNa6MS9')
        if show_controls_popup and not popup_closed and vr.is_runtime_ready() and vr.is_hmd_active() and imgui.begin_window and imgui.end_window then
            local p_w, p_h = 600, 460
            if imgui.set_next_window_size then imgui.set_next_window_size({p_w, p_h}, 1) end
            local keep_open, win_open = imgui.begin_window('Far Far VR - Controls & Bindings', true, 0)
            if win_open and keep_open ~= false then
                text('FAR FAR VR CONTROLS & BINDINGS')
                imgui.same_line()
                text('(You can also review this anytime on the Controls tab)')
                imgui.spacing()
                if imgui.draw_controls_image then
                    imgui.draw_controls_image(570, 321)
                else
                    text('Controls mapping diagram:')
                end
                imgui.spacing()
                imgui.separator()
                imgui.spacing()
                if imgui.button('Close Controls Diagram', {240, 36}) then
                    show_controls_popup = false
                    popup_closed = true
                end
                imgui.same_line()
                text('Press Trigger or A to close, or click [X] above.')
                imgui.end_window()
            else
                show_controls_popup = false
                popup_closed = true
                imgui.end_window()
            end
        end
    end)
    if failed then text('VR settings could not be accessed:'); text(failed) end
end
uevr.lua.add_script_panel('Far Far VR',draw)
safe(function() snapshot() end)
print('[FarFarVR] Menu module loaded (0.4.5).')
end)
if not startup_ok then
    local message=('[FarFarVR] Menu startup error: '..tostring(startup_error)):gsub('%%','%%%%')
    pcall(function() uevr.params.functions.log_error(message) end)
    pcall(function() fs.write('farfarvr-menu-startup-error.txt',tostring(startup_error)) end)
end
