-- Far Far VR settings UI. Rendering, poses and bindings remain owned by UEVR.
local startup_ok, startup_error = pcall(function()
local vr = uevr.params.vr
local fields = require('farfarvr_settings')
local groups = {'Comfort', 'Calibration', 'Input', 'HUD'}
local page, baseline, was_open, failed = 'Comfort', nil, false, nil
local status = 'Changes apply live. Save to keep them for your next session.'
local by_key = {}
for _, field in ipairs(fields) do by_key[field.key] = field end
local function finite(v) return type(v)=='number' and v==v and v~=math.huge and v~=-math.huge end
local function read(field)
    local raw = vr:get_mod_value(field.key)
    if field.kind=='bool' then
        assert(raw=='true' or raw=='false', 'Unavailable setting: '..field.label)
        return raw=='true'
    end
    local value=tonumber(raw)
    assert(finite(value), 'Unavailable setting: '..field.label)
    return value
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
    vr.set_mod_value(field.key,tostring(value))
    local actual=read(field)
    assert(actual==value or (finite(actual) and math.abs(actual-value)<.0001), 'Runtime did not apply '..field.label)
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
uevr.sdk.callbacks.on_frame(function()
    if failed then return end
    local open=uevr.params.functions.is_drawing_ui()
    if open and not was_open then safe(function() baseline=snapshot() end) end
    was_open=open
end)
local function packed(n) return n>=0x80000000 and n-0x100000000 or n end
local function draw()
    safe(function()
        if not baseline then baseline=snapshot() end
        for i,name in ipairs(groups) do
            if i>1 then imgui.same_line() end
            imgui.push_style_color(21,packed(page==name and 0xFF75BAEE or 0xFF302B25))
            imgui.push_style_color(0,packed(page==name and 0xFF171B20 or 0xFFE9E5DF))
            if imgui.button(name,{174,38}) then page=name end
            imgui.pop_style_color(2)
        end
        imgui.spacing()
        if failed then imgui.text('Settings unavailable: '..failed); return end
        if page=='Calibration' then
            imgui.text('Sit or stand naturally and look forward before calibrating.')
            if imgui.button('Calibrate current height',{230,36}) then calibrate(true) end
            imgui.same_line()
            if imgui.button('Recenter position',{210,36}) then calibrate(false) end
            if imgui.button('Reset height & camera',{230,36}) then
                for _,key in ipairs({'VR_CameraUpOffset','VR_CameraForwardOffset','VR_CameraRightOffset'}) do write(by_key[key],0) end
                status='Manual camera adjustments reset. Controller bindings are preserved.'
            end
            imgui.text('Height calibration lasts this session. Fine adjustments can be saved.')
            imgui.spacing()
        elseif page=='Input' then
            imgui.text('Your corrected Touch button mapping is preserved.')
            imgui.spacing()
        elseif page=='Comfort' then
            imgui.text('Choose how turning and movement feel.')
            imgui.spacing()
        else
            imgui.text('Place the game interface and settings menu comfortably in VR.')
            imgui.spacing()
        end
        for _,field in ipairs(fields) do
            if field.group==page then
                local current=read(field)
                local changed,value
                imgui.set_next_item_width(460)
                if field.kind=='bool' then changed,value=imgui.checkbox(field.label,current)
                elseif field.kind=='choice' then
                    local choices={}; for i,label in ipairs(field.choices) do choices[i-1]=label end
                    changed,value=imgui.combo(field.label,current,choices)
                elseif field.kind=='int' then changed,value=imgui.slider_int(field.label,current,field.min,field.max)
                else changed,value=imgui.slider_float(field.label,current,field.min,field.max,'%.2f'..(field.unit and (' '..field.unit) or '')) end
                if changed then write(field,value); status='Applied. Save to keep your changes.' end
                imgui.spacing()
            end
        end
        imgui.spacing(); imgui.separator(); imgui.spacing()
        if imgui.button('Recenter view',{180,36}) and headset_ready() then vr.recenter_view(); status='Facing direction reset. Height is unchanged.' end
        imgui.same_line()
        if imgui.button('Save settings',{180,36}) then vr.save_config(); baseline=snapshot(); status='Save requested through the VR engine.' end
        imgui.same_line()
        if imgui.button("Revert this session's edits",{280,36}) then
            for _,field in ipairs(fields) do if read(field)~=baseline[field.key] then write(field,baseline[field.key],true) end end
            status='Restored settings from when you opened this menu or last saved.'
        end
        imgui.text(status)
    end)
    if failed then imgui.text('VR settings could not be accessed:'); imgui.text(failed) end
end
uevr.lua.add_script_panel('Far Far VR',draw)
safe(function() snapshot() end)
print('[FarFarVR] Menu module loaded (0.3.0).')
end)
if not startup_ok then
    local message=('[FarFarVR] Menu startup error: '..tostring(startup_error)):gsub('%%','%%%%')
    pcall(function() uevr.params.functions.log_error(message) end)
    pcall(function() fs.write('farfarvr-menu-startup-error.txt',tostring(startup_error)) end)
end
