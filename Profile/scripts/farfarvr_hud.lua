-- Original animated crosshair on an independent compositor aim layer.
local api,vr=uevr.api,uevr.params.vr
local state,fault,last_pawn,title_camera,title_location,title_owner
local scan_time,status_time=0,0
local function find(path)
    local obj=api:find_uobject(path)
    assert(obj,'Missing '..path)
    return obj
end
local function native(path,values)
    local obj=StructObject.new(find('ScriptStruct /Script/'..path))
    for key,value in pairs(values) do obj[key]=value end
    return obj
end
local function v(x,y,z) return native('CoreUObject.Vector',{X=x,Y=y,Z=z}) end
local identity=native('CoreUObject.Transform',{Translation={X=0,Y=0,Z=0},Rotation={X=0,Y=0,Z=0,W=1},Scale3D={X=1,Y=1,Z=1}})
local function log(s) uevr.params.functions.log_info('[Far Far VR HUD] '..s) end
local function alive(o) return o~=nil and UEVR_UObjectHook.exists(o) end
local function restore_title()
    if title_camera and (alive(title_camera) or api:get_local_pawn(0)==title_owner) then
        pcall(function() title_camera:K2_SetRelativeLocation(title_location,false,{},true) end)
    end
    title_camera,title_location,title_owner=nil,nil,nil
end
-- Original game widgets remain in UI_Player's tree so animation bindings and
-- weapon callbacks keep their real targets. Hidden full-size canvas wrappers
-- suppress only their viewport paint while live copies draw on the aim canvas.
local function xy(x,y) return native('CoreUObject.Vector2D',{X=x,Y=y}) end
local function copy_layout(slot)
    local l=slot.LayoutData
    return native('UMG.AnchorData',{Offsets={Left=l.Offsets.Left,Top=l.Offsets.Top,Right=l.Offsets.Right,Bottom=l.Offsets.Bottom},
        Anchors={Minimum={X=l.Anchors.Minimum.X,Y=l.Anchors.Minimum.Y},Maximum={X=l.Anchors.Maximum.X,Y=l.Anchors.Maximum.Y}},
        Alignment={X=l.Alignment.X,Y=l.Alignment.Y}})
end
local function cleanup()
    local s=state; state=nil
    if not s then return end
    if alive(s.canvas) then pcall(function() s.canvas:RemoveFromParent() end) end
    if alive(s.head) then pcall(function() UEVR_UObjectHook.remove_motion_controller_state(s.head); s.head:K2_DestroyComponent(s.pawn) end) end
    for _,item in ipairs(s.images or {}) do
        if alive(item.source) and alive(item.parent) then pcall(function()
            item.source:RemoveFromParent()
            local slot=item.parent:AddChildToCanvas(item.source)
            slot:SetLayout(item.layout); slot:SetZOrder(item.z)
        end) end
        if alive(item.wrapper) then pcall(function() item.wrapper:RemoveFromParent() end) end
    end
end
local function marker_target(hit)
    local dx,dy,dz=hit.point.X-hit.start.X,hit.point.Y-hit.start.Y,hit.point.Z-hit.start.Z
    local distance=math.sqrt(dx*dx+dy*dy+dz*dz)
    if distance<1 then return nil end
    local pos=v(hit.start.X+dx/distance*275,hit.start.Y+dy/distance*275,hit.start.Z+dz/distance*275)
    local rot=native('CoreUObject.Rotator',{Pitch=math.deg(math.atan(dz,math.sqrt(dx*dx+dy*dy))),Yaw=math.deg(math.atan(dy,dx)),Roll=0})
    return pos,rot
end
local function show_marker(s,visible)
    if s.showing==visible then return end
    s.reticle:SetVisibility(visible and 4 or 2)
    for _,item in ipairs(s.images) do item.wrapper:SetVisibility(visible and 2 or 4) end
    s.showing=visible
end
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
local function sync_images(s)
    for _,item in ipairs(s.images) do
        local src,clone=item.source,item.clone
        -- Share the actual brush resource, including animated material instances.
        -- Copy transient widget animation state; never start substitute animations.
        clone:SetBrush(src.Brush)
        clone:SetColorAndOpacity(src.ColorAndOpacity)
        clone:SetRenderOpacity(src.RenderOpacity)
        clone:SetRenderTransform(src.RenderTransform)
        clone:SetVisibility(src:GetVisibility())
        local l=src.Slot.LayoutData
        item.slot:SetSize(xy(l.Offsets.Right,l.Offsets.Bottom))
        item.slot:SetPosition(xy(l.Offsets.Left-item.layout.Offsets.Left,l.Offsets.Top-item.layout.Offsets.Top))
    end
end
local function build(pawn,pc,hud)
    local s={pawn=pawn,hud=hud,elapsed=0,animation_time=0,settings_time=1,weapon_scan=1,trace_count=0,images={},showing=false}
    state=s
    UEVR_UObjectHook.activate()
    s.head=assert(pawn:AddComponentByClass(find('Class /Script/Engine.SceneComponent'),true,identity,false))
    local tracking=assert(UEVR_UObjectHook.get_or_add_motion_controller_state(s.head))
    tracking:set_hand(2); tracking:set_permanent(true)
    s.layout=find('Class /Script/UMG.WidgetLayoutLibrary'):get_class_default_object()
    local center=native('Slate.Anchors',{Minimum={X=.5,Y=.5},Maximum={X=.5,Y=.5}})
    local full=native('Slate.Anchors',{Minimum={X=0,Y=0},Maximum={X=1,Y=1}})
    s.canvas=assert(api:spawn_object(find('Class /Script/UMG.CanvasPanel'),hud.WidgetTree),'Crosshair canvas unavailable')
    local layer_slot=hud.WidgetTree.RootWidget:AddChildToCanvas(s.canvas)
    layer_slot:SetAnchors(full); layer_slot:SetPosition(xy(0,0)); layer_slot:SetSize(xy(0,0)); layer_slot:SetZOrder(100)
    s.canvas:SetVisibility(4)
    s.reticle=assert(api:spawn_object(find('Class /Script/UMG.CanvasPanel'),hud.WidgetTree))
    s.reticle_slot=s.canvas:AddChildToCanvas(s.reticle)
    s.reticle_slot:SetAnchors(center); s.reticle_slot:SetAlignment(xy(.5,.5)); s.reticle_slot:SetSize(xy(256,256))
    s.reticle:SetVisibility(2)
    for _,key in ipairs({'Crosshair_Top'}) do
        local source=hud[key]
        if source then
            local old=source.Slot
            local item={key=key,source=source,parent=source:GetParent(),layout=copy_layout(old),z=old.ZOrder}
            s.images[#s.images+1]=item
            item.wrapper=assert(api:spawn_object(find('Class /Script/UMG.CanvasPanel'),hud.WidgetTree))
            source:RemoveFromParent()
            local wrap_slot=item.parent:AddChildToCanvas(item.wrapper)
            wrap_slot:SetAnchors(full); wrap_slot:SetAlignment(xy(0,0)); wrap_slot:SetPosition(xy(0,0)); wrap_slot:SetSize(xy(0,0)); wrap_slot:SetZOrder(item.z)
            local live_slot=item.wrapper:AddChildToCanvas(source)
            live_slot:SetLayout(item.layout); live_slot:SetZOrder(item.z)
            item.wrapper:SetVisibility(4)
            item.clone=assert(api:spawn_object(find('Class /Script/UMG.Image'),hud.WidgetTree))
            item.slot=s.reticle:AddChildToCanvas(item.clone)
            item.slot:SetAnchors(center)
            item.slot:SetAlignment(xy(item.layout.Alignment.X,item.layout.Alignment.Y))
            item.slot:SetZOrder(item.z)
            log(string.format('Live crosshair copy %s; original size %.1f x %.1f',key,item.layout.Offsets.Right,item.layout.Offsets.Bottom))
        end
    end
    assert(#s.images>0,'No original crosshair widgets found')
    sync_images(s)
    s.camera=assert(pc.PlayerCameraManager,'Aim camera unavailable')
    s.weapon_class=find('BlueprintGeneratedClass /Game/Items/BP_Item.BP_Item_C')
    log('Original animated crosshair on independent compositor aim layer; 275 cm gun target; no render target.')
end
local function update(s,pc,dt)
    s.animation_time=s.animation_time+dt
    s.settings_time=s.settings_time+dt
    if s.animation_time>=1/60 then s.animation_time=0; sync_images(s) end
    s.weapon_scan=s.weapon_scan+dt
    local cinema=vr:get_mod_value('VR_2DScreenMode')=='true'
    local visible=vr.is_runtime_ready() and not cinema and not pc.bShowMouseCursor and not uevr.params.functions.is_drawing_ui()
    s.hide_reason=not vr.is_runtime_ready() and 'runtime unavailable'
        or cinema and 'cinema screen'
        or pc.bShowMouseCursor and 'game menu'
        or uevr.params.functions.is_drawing_ui() and 'VR menu' or 'none'
    s.ready=false
    if not visible then show_marker(s,false); s.adjusted=false; return end
    if s.settings_time>=1 then
        s.settings_time=0
        s.distance=assert(tonumber(vr:get_mod_value('UI_Distance')))
        s.height=assert(tonumber(vr:get_mod_value('UI_Size')))
        s.xoffset=tonumber(vr:get_mod_value('UI_X_Offset')) or 0
        s.yoffset=tonumber(vr:get_mod_value('UI_Y_Offset')) or 0
        s.supported=vr:get_mod_value('UI_FollowView')=='true' and vr:get_mod_value('UI_OverlayType')=='0'
        local viewport=s.layout:GetViewportSize(pc)
        s.logical_height=viewport.Y/math.max(s.layout:GetViewportScale(pc),.01)
    end
    if not s.supported then s.hide_reason='unsupported UI projection'; show_marker(s,false); s.adjusted=false; return end
    if s.weapon and (not alive(s.weapon) or s.weapon.OwningPlayer~=s.pawn or not s.weapon.isDrawn) then s.weapon=nil end
    if s.weapon_scan>=1 then
        s.weapon_scan=0
        if not s.weapon then
            for _,weapon in ipairs(s.weapon_class:get_objects_matching(false)) do
                local name=weapon:get_full_name()
                if name:find('.PersistentLevel.',1,true) and not name:find('BP_Item_Empty_C',1,true)
                    and weapon.OwningPlayer==s.pawn and weapon.isDrawn and weapon.canBeReloaded then s.weapon=weapon; break end
            end
        end
    end
    if not s.weapon then s.hide_reason='no drawn firearm'; show_marker(s,false); s.adjusted=false; return end
    s.ready=true
end
local function render_aim(s)
    if not s.ready then return end
    -- F_TraceBullet uses these exact camera actor properties before adding spread.
    -- Read them after UObjectHook has updated the HMD for this stereo view, not
    -- against last frame's HMD at the end of the engine tick. No physics query.
    local start=s.camera:K2_GetActorLocation()
    local direction=s.camera:GetActorForwardVector()
    local pos=marker_target({start=start,point={X=start.X+direction.X*10000,Y=start.Y+direction.Y*10000,Z=start.Z+direction.Z*10000}})
    if not pos then s.hide_reason='empty aim trace'; show_marker(s,false); s.adjusted=false; return end
    local x,y=project(s.head:K2_GetComponentLocation(),s.head:K2_GetComponentRotation(),pos,
        s.distance,s.height,s.xoffset,s.yoffset,s.logical_height)
    if not x then s.hide_reason='gun target behind headset'; show_marker(s,false); s.adjusted=false; return end
    s.reticle_slot:SetPosition(xy(x,y))
    s.screen_x,s.screen_y=x,y
    show_marker(s,true)
    -- Wrappers hide only viewport paint; source animation properties stay intact.
    s.adjusted=true
    s.trace_count=s.trace_count+1
end
uevr.sdk.callbacks.on_pre_calculate_stereo_view_offset(function()
    if fault or not state then return end
    local ok,err=pcall(function() render_aim(state) end)
    if not ok then
        fault=tostring(err); cleanup(); restore_title()
        fs.write('farfarvr-hud-status.txt','ERROR in render aim: '..fault)
    end
end)
local function title_height(pawn)
    if pawn and pawn:get_full_name():find('BP_TitleScreen_C ',1,true) then
        if title_camera~=pawn.CineCamera then
            title_camera=pawn.CineCamera
            title_owner=pawn
            local loc=title_camera.RelativeLocation
            title_location=v(loc.X,loc.Y,loc.Z)
            title_camera:K2_SetRelativeLocation(v(loc.X,loc.Y,loc.Z+60),false,{},true)
            assert(math.abs(title_camera.RelativeLocation.Z-(title_location.Z+60))<.1,'Main-menu camera elevation was rejected')
            log('Main-menu camera raised 60 cm; gameplay camera unchanged.')
            fs.write('farfarvr-hud-status.txt','Main-menu camera raised 60 cm; menu projection unchanged')
        end
    elseif title_camera then
        restore_title()
    end
end
uevr.sdk.callbacks.on_post_engine_tick(function(engine,dt)
    if fault then return end
    local ok,err=pcall(function()
        local pc=api:get_player_controller(0)
        if not pc then return end
        local pawn=pc.Pawn
        title_height(pawn)
        if pawn~=last_pawn then cleanup(); last_pawn=pawn; scan_time=0 end
        if state and (not alive(state.hud) or not state.hud:IsInViewport()) then cleanup(); scan_time=0 end
        if not pawn or not pawn:get_full_name():find('BP_Player',1,true) then return end
        if not state then
            scan_time=scan_time+dt
            if scan_time<1 then return end
            scan_time=0
            local cls=api:find_uobject('WidgetBlueprintGeneratedClass /Game/Interfaces/UI_Player.UI_Player_C')
            if not cls then return end
            for _,hud in ipairs(cls:get_objects_matching(false)) do
                if hud:get_full_name():find('/Engine/Transient.',1,true) and hud:IsInViewport() and hud:GetOwningPlayer()==pc then
                    build(pawn,pc,hud); break
                end
            end
        end
        if state then update(state,pc,dt) end
        status_time=status_time+dt
        if status_time>5 then
            status_time=0
            local message='Waiting for gameplay HUD'
            if state then message=string.format('Original animated crosshair v7; synchronized stereo-view aim; 275 cm gun target\nweapon %s\naim_view_updates %d (no physics queries)\nmarker_visible %s\nhide_reason %s\nsource_visibility %s opacity %.3f\nscreen_offset %.1f %.1f; no crosshair render target',
                state.weapon and state.weapon:get_full_name() or 'none equipped',state.trace_count,tostring(state.adjusted or false),state.hide_reason or 'none',
                tostring(state.images[1].source:GetVisibility()),state.images[1].source.RenderOpacity,
                state.screen_x or 0,state.screen_y or 0) end
            fs.write('farfarvr-hud-status.txt',message)
        end
    end)
    if not ok then
        fault=tostring(err); cleanup(); restore_title()
        uevr.params.functions.log_error('[Far Far VR HUD] Restored original HUD: '..fault)
        fs.write('farfarvr-hud-status.txt','ERROR: '..fault)
    end
end)
uevr.sdk.callbacks.on_script_reset(function() cleanup(); restore_title() end)
