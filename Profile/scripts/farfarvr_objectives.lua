-- Far Far VR by PR_VR (https://www.youtube.com/@PR_VR)
-- Powered by UEVR created by praydog
-- Keep the original objective widgets sharp; project their world anchors using
-- the HMD and the actual UEVR compositor quad, independently of weapon aiming.
local api,vr=uevr.api,uevr.params.vr
local state,last_pawn,fault
local scan_time=0
local obj_cache = {}
local function find(path)
    local obj = obj_cache[path]
    if not obj then
        obj = api:find_uobject(path)
        assert(obj, 'Missing '..path)
        obj_cache[path] = obj
    end
    return obj
end
local function native(path,values)
    local obj=StructObject.new(find('ScriptStruct /Script/'..path))
    for key,value in pairs(values) do obj[key]=value end
    return obj
end
local function xy(x,y) return native('CoreUObject.Vector2D',{X=x,Y=y}) end
local identity=native('CoreUObject.Transform',{Translation={X=0,Y=0,Z=0},Rotation={X=0,Y=0,Z=0,W=1},Scale3D={X=1,Y=1,Z=1}})
local function alive(o) return o~=nil and UEVR_UObjectHook.exists(o) end
local function cleanup()
    local s=state; state=nil
    if not s then return end
    for _,item in ipairs(s.items) do
        if alive(item.widget) then pcall(function()
            item.widget:RemoveFromParent()
            if alive(item.component) then
                item.component.WidgetClass=item.widget_class
                item.component:SetWidget(item.widget)
            end
        end) end
    end
    if alive(s.hud) then pcall(function() s.hud:RemoveFromParent() end) end
    if alive(s.head) then pcall(function()
        UEVR_UObjectHook.remove_motion_controller_state(s.head)
        s.head:K2_DestroyComponent(s.pawn)
    end) end
end
-- UI_Size is quad HEIGHT in metres (OverlayComponent.cpp). Convert a world
-- direction into the quad's logical UMG coordinates, including HMD roll.
local function project(hp,hr,point,distance,height,xoffset,yoffset,logical_height)
    local p,y,r=math.rad(hr.Pitch),math.rad(hr.Yaw),math.rad(hr.Roll)
    local cp,sp,cy,sy,cr,sr=math.cos(p),math.sin(p),math.cos(y),math.sin(y),math.cos(r),math.sin(r)
    local dx,dy,dz=point.X-hp.X,point.Y-hp.Y,point.Z-hp.Z
    local forward=dx*cp*cy+dy*cp*sy+dz*sp
    if forward<=1 or height<=0 then return nil end
    local right=dx*(sr*sp*cy-cr*sy)+dy*(sr*sp*sy+cr*cy)-dz*sr*cp
    local up=-dx*(cr*sp*cy+sr*sy)+dy*(cy*sr-cr*sp*sy)+dz*cr*cp
    return (right/forward*distance-xoffset)/height*logical_height,
        -(up/forward*distance-yoffset)/height*logical_height
end
local function build(pawn,pc)
    local s={pawn=pawn,pc=pc,items={},scan=1,settings=1,updates=0}
    state=s
    local library=find('Class /Script/UMG.WidgetBlueprintLibrary'):get_class_default_object()
    s.hud=assert(library:Create(pc,find('WidgetBlueprintGeneratedClass /Game/Interfaces/UI_LowAmmo.UI_LowAmmo_C'),pc))
    UEVR_UObjectHook.activate()
    s.head=assert(pawn:AddComponentByClass(find('Class /Script/Engine.SceneComponent'),true,identity,false))
    pawn:FinishAddComponent(s.head,true,identity)
    local tracking=assert(UEVR_UObjectHook.get_or_add_motion_controller_state(s.head))
    tracking:set_hand(2); tracking:set_permanent(true)
    s.canvas=assert(api:spawn_object(find('Class /Script/UMG.CanvasPanel'),s.hud.WidgetTree))
    s.hud.WidgetTree.RootWidget=s.canvas
    s.hud:AddToViewport(20)
    s.hud:SetVisibility(4)
    s.canvas:SetVisibility(4)
    s.layout=find('Class /Script/UMG.WidgetLayoutLibrary'):get_class_default_object()
    s.component_class=find('Class /Script/UMG.WidgetComponent')
    s.marker_classes={}
    for _,path in ipairs({'Pings/UI_PingObjective','Pings/UI_Ping','Pings/UI_ObjectPing','UI_WorldIcon','UI_NpcInGame','UI_Guide'}) do
        local short=path:match('([^/]+)$')
        local cls=api:find_uobject('WidgetBlueprintGeneratedClass /Game/Interfaces/'..path..'.'..short..'_C')
        if cls then s.marker_classes[#s.marker_classes+1]=cls end
    end
end
local function is_marker(s,widget)
    for _,cls in ipairs(s.marker_classes) do if widget:is_a(cls) then return true end end
    return false
end
local function adopt(s)
    for _,component in ipairs(s.component_class:get_objects_matching(false)) do
        if component.Space==1 and component:get_full_name():find('.PersistentLevel.',1,true) then
            local widget=component:GetWidget()
            local owner=widget and widget:GetOwningPlayer()
            -- Level-authored saloon WidgetComponents can have no explicit
            -- player owner; their screen-space widget still belongs in this view.
            if widget and is_marker(s,widget) and (owner==nil or owner==s.pc) then
                local known=false
                for _,item in ipairs(s.items) do if item.component==component then known=true; break end end
                if not known then
                    local item={component=component,widget=widget,widget_class=component.WidgetClass}
                    s.items[#s.items+1]=item -- rollback owns it before detaching
                    component.WidgetClass=nil -- prevent InitWidget recreating a controller-projected duplicate
                    component:SetWidget(nil)
                    item.wrapper=assert(api:spawn_object(find('Class /Script/UMG.CanvasPanel'),s.hud.WidgetTree))
                    local slot=s.canvas:AddChildToCanvas(item.wrapper)
                    slot:SetAnchors(native('Slate.Anchors',{Minimum={X=0,Y=0},Maximum={X=1,Y=1}}))
                    slot:SetSize(xy(0,0)); slot:SetPosition(xy(0,0))
                    item.slot=item.wrapper:AddChildToCanvas(widget)
                    item.slot:SetAnchors(native('Slate.Anchors',{Minimum={X=.5,Y=.5},Maximum={X=.5,Y=.5}}))
                    item.slot:SetAlignment(xy(.5,.5)); item.slot:SetAutoSize(true)
                end
            end
        end
    end
end
local function update(s,dt)
    s.scan=s.scan+dt; s.settings=s.settings+dt
    if s.settings>=1 then
        s.settings=0
        s.distance=assert(tonumber(vr:get_mod_value('UI_Distance')))
        s.height=assert(tonumber(vr:get_mod_value('UI_Size')))
        s.xoffset=tonumber(vr:get_mod_value('UI_X_Offset')) or 0
        s.yoffset=tonumber(vr:get_mod_value('UI_Y_Offset')) or 0
        s.supported=vr:get_mod_value('UI_OverlayType')=='0'
        local viewport=s.layout:GetViewportSize(s.pc)
        s.logical_height=viewport.Y/math.max(s.layout:GetViewportScale(s.pc),.01)
    end
    if s.scan>=1 then s.scan=0; adopt(s) end
    local visible=s.supported and vr.is_runtime_ready() and vr:get_mod_value('VR_2DScreenMode')~='true' and (not s.pc or not s.pc.bShowMouseCursor) and not uevr.params.functions.is_drawing_ui()
    s.canvas:SetVisibility(visible and 4 or 2)
    if not visible then return end
end
local function render_markers(s)
    if not s.supported or not vr.is_runtime_ready() or vr:get_mod_value('VR_2DScreenMode')=='true' or (s.pc and s.pc.bShowMouseCursor) or uevr.params.functions.is_drawing_ui() then return end
    local hp,hr=s.head:K2_GetComponentLocation(),s.head:K2_GetComponentRotation()
    s.visible_count=0
    for _,item in ipairs(s.items) do
        if alive(item.widget) and alive(item.component) then
            local point=item.component:K2_GetComponentLocation()
            local x,y=project(hp,hr,point,s.distance,s.height,s.xoffset,s.yoffset,s.logical_height)
            local show=x~=nil and item.component:IsVisible() and not item.component.bHiddenInGame
            item.wrapper:SetVisibility(show and 4 or 2)
            if show then
                item.slot:SetPosition(xy(x,y))
                s.visible_count=s.visible_count+1
            end
        elseif alive(item.wrapper) then item.wrapper:SetVisibility(2) end
    end
    s.updates=s.updates+1
end
uevr.sdk.callbacks.on_pre_calculate_stereo_view_offset(function()
    if fault or not state then return end
    local ok,err=pcall(function() render_markers(state) end)
    if not ok then fault=tostring(err); cleanup(); fs.write('farfarvr-objectives-status.txt','ERROR in view update: '..fault) end
end)
uevr.sdk.callbacks.on_post_engine_tick(function(_,dt)
    if fault then return end
    local ok,err=pcall(function()
        local pc=api:get_player_controller(0)
        local pawn=pc and pc.Pawn
        if pawn~=last_pawn then cleanup(); last_pawn=pawn; scan_time=0 end
        if state and (not alive(state.hud) or not state.hud:IsInViewport()) then cleanup() end
        if not pawn then return end
        local pname=pawn:get_full_name()
        if not pname:find('BP_Player',1,true) and not pname:find('BP_Mount_C ',1,true) then return end
        if not state then
            scan_time=scan_time+dt
            if scan_time<1 then return end
            scan_time=0
            build(pawn,pc) -- lobby and mounted HUDs need no UI_Player dependency
        end
        if state then update(state,dt) end

    end)
    if not ok then fault=tostring(err); cleanup(); fs.write('farfarvr-objectives-status.txt','ERROR: '..fault) end
end)
uevr.sdk.callbacks.on_script_reset(cleanup)
