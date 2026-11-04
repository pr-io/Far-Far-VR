-- Far Far VR by PR_VR (https://www.youtube.com/@PR_VR)
-- Powered by UEVR created by praydog
-- Original animated artwork follows the firing ray using the accepted per-view renderer.
local api,vr=uevr.api,uevr.params.vr
local health=require('farfarvr_health')
local state,fault,last_pawn,title_camera,title_location,title_owner
local possessed_pawn
local scan_time=0
local retry_time,stage=0,'startup'
local recover_hud
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
local function safe_full_name(o) return o:get_full_name() end
local function safe_class(o) return o:get_class() end
local function safe_find_is_drawn(cls) return cls:find_property('isDrawn') end
local function native(path,values)
    local obj=StructObject.new(find('ScriptStruct /Script/'..path))
    for key,value in pairs(values) do obj[key]=value end
    return obj
end
local function v(x,y,z) return native('CoreUObject.Vector',{X=x,Y=y,Z=z}) end
local identity=native('CoreUObject.Transform',{Translation={X=0,Y=0,Z=0},Rotation={X=0,Y=0,Z=0,W=1},Scale3D={X=1,Y=1,Z=1}})
local function log(s) uevr.params.functions.log_info('[Far Far VR HUD] '..s) end
local function alive(o)
    if o==nil then return false end
    local ok,exists=pcall(UEVR_UObjectHook.exists,o)
    return ok and exists
end
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
local function clear_aim() vr.set_mod_value('VR_FarFarGunAimTarget','') end
local function cleanup()
    pcall(clear_aim)
    local s=state;state=nil;if not s then return end
    for _,item in ipairs(s.images or {}) do
        if alive(item.source) and alive(item.parent) then pcall(function()
            item.source:RemoveFromParent();local slot=item.parent:AddChildToCanvas(item.source)
            slot:SetLayout(item.layout);slot:SetZOrder(item.z)
        end) end
        if alive(item.wrapper) then pcall(function() item.wrapper:RemoveFromParent() end) end
    end
    if alive(s.canvas) then pcall(function() s.canvas:RemoveFromParent() end) end
    for _,anchor in ipairs({s.head,s.hand}) do
        if alive(anchor) then pcall(function()
            UEVR_UObjectHook.remove_motion_controller_state(anchor);anchor:K2_DestroyComponent(s.pawn)
        end) end
    end
end
local function show_marker(s,visible)
    local suppress_originals=s.aim_enabled or visible
    if s.showing==visible and s.originals_showing==not suppress_originals then return end
    s.reticle:SetVisibility(visible and 4 or 2)
    for _,item in ipairs(s.images) do item.wrapper:SetVisibility(suppress_originals and 2 or 4) end
    s.showing=visible;s.originals_showing=not suppress_originals;s.dirty=true
end
local function sync_images(s)
    local origin=s.images[1].layout.Offsets
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
        local width,height=l.Offsets.Right,l.Offsets.Bottom
        if item.width~=width or item.height~=height then
            item.slot:SetSize(xy(width,height));item.width,item.height=width,height
        end
        -- All three images are siblings in the original aiming panel. Retain
        -- their offsets relative to Crosshair_Top; independently zeroing each
        -- image would misalign hits authored with a different size/alignment.
        local x,y=l.Offsets.Left-origin.Left,l.Offsets.Top-origin.Top
        if item.x~=x or item.y~=y then
            item.slot:SetPosition(xy(x,y));item.x,item.y=x,y
        end
    end
end
local function build(pawn,pc,hud)
    local s={pawn=pawn,pc=pc,hud=hud,root=hud.WidgetTree.RootWidget,images={},showing=false,animation_time=0,dirty=true,updates=0};state=s
    UEVR_UObjectHook.activate()
    -- Match the accepted per-view aim reference. These private anchors read
    -- tracking; they never change the weapon, game camera, or locomotion.
    s.head=assert(pawn:AddComponentByClass(find('Class /Script/Engine.SceneComponent'),true,identity,false))
    local head_tracking=assert(UEVR_UObjectHook.get_or_add_motion_controller_state(s.head))
    head_tracking:set_hand(2);head_tracking:set_permanent(true)
    s.hand=assert(pawn:AddComponentByClass(find('Class /Script/Engine.SceneComponent'),true,identity,false))
    local hand_tracking=assert(UEVR_UObjectHook.get_or_add_motion_controller_state(s.hand))
    hand_tracking:set_hand(1);hand_tracking:set_permanent(true)
    s.layout=find('Class /Script/UMG.WidgetLayoutLibrary'):get_class_default_object()
    local center=native('Slate.Anchors',{Minimum={X=.5,Y=.5},Maximum={X=.5,Y=.5}})
    local full=native('Slate.Anchors',{Minimum={X=0,Y=0},Maximum={X=1,Y=1}})
    s.canvas=assert(api:spawn_object(find('Class /Script/UMG.CanvasPanel'),hud.WidgetTree))
    local layer_slot=s.root:AddChildToCanvas(s.canvas)
    layer_slot:SetAnchors(full);layer_slot:SetPosition(xy(0,0));layer_slot:SetSize(xy(0,0));layer_slot:SetZOrder(100)
    s.canvas:SetVisibility(4)
    s.reticle=assert(api:spawn_object(find('Class /Script/UMG.CanvasPanel'),hud.WidgetTree))
    s.reticle_slot=s.canvas:AddChildToCanvas(s.reticle)
    s.reticle_slot:SetAnchors(center);s.reticle_slot:SetAlignment(xy(.5,.5));s.reticle_slot:SetSize(xy(256,256))
    s.reticle:SetVisibility(2)
    -- Keep game-owned originals alive for hit animations; move their live copies.
    for _,key in ipairs({'Crosshair_Top','HitImage','HitShield'}) do
        local source=hud[key]
        if source then
            local old=source.Slot
            local item={key=key,source=source,parent=source:GetParent(),layout=copy_layout(old),z=old.ZOrder}
            s.images[#s.images+1]=item
            item.wrapper=assert(api:spawn_object(find('Class /Script/UMG.CanvasPanel'),hud.WidgetTree))
            source:RemoveFromParent()
            local slot=item.parent:AddChildToCanvas(item.wrapper)
            slot:SetAnchors(full);slot:SetAlignment(xy(0,0));slot:SetPosition(xy(0,0));slot:SetSize(xy(0,0));slot:SetZOrder(item.z)
            local live_slot=item.wrapper:AddChildToCanvas(source);live_slot:SetLayout(item.layout);live_slot:SetZOrder(item.z)
            item.wrapper:SetVisibility(4)
            item.clone=assert(api:spawn_object(find('Class /Script/UMG.Image'),hud.WidgetTree))
            item.slot=s.reticle:AddChildToCanvas(item.clone);item.slot:SetAnchors(center)
            item.slot:SetAlignment(xy(item.layout.Alignment.X,item.layout.Alignment.Y));item.slot:SetZOrder(item.z)
        end
    end
    assert(#s.images>0,'No original reticle artwork found');sync_images(s)
    health.set('HUD','Active'); fault=nil;log('Restored per-view firing-ray renderer for crosshair, hit X and hands; no offscreen target or readiness handshake.')
end
local function valid_state(s)
    if not alive(s.hud) or not s.hud:IsInViewport() or s.hud.WidgetTree.RootWidget~=s.root or not alive(s.canvas) or not alive(s.reticle) or not alive(s.head) or not alive(s.hand) then return false end
    for _,item in ipairs(s.images) do if not alive(item.source) or not alive(item.clone) or not alive(item.wrapper) or not item.source.Slot then return false end end
    return true
end
local function update(s,pc,dt)
    s.animation_time=s.animation_time+dt
    local visible=vr.is_runtime_ready() and vr.is_hmd_active() and vr:get_mod_value('VR_2DScreenMode')~='true' and not pc.bShowMouseCursor and not uevr.params.functions.is_drawing_ui()
    s.aim_enabled=visible
    if not visible then show_marker(s,false) end
    if s.dirty or (visible and s.animation_time>=1/60) then
        sync_images(s);s.animation_time=0;s.dirty=false
    end

end
local function render_aim(s)
    if not s.aim_enabled then return end
    -- Firearms use the camera actor that F_TraceBullet reads, as in the
    -- wearer-accepted implementation. Empty hands retain right-hand aim.
    local items=s.pawn:get_class():find_property('AC_PlayerItems') and s.pawn.AC_PlayerItems
    local item=items and items.equipedItem
    local is_empty=false
    if alive(item) then
        local ok,name=pcall(safe_full_name,item)
        if ok and type(name)=='string' and name:find('BP_Item_Empty',1,true) then
            is_empty=true
        else
            local ok_cls,cls=pcall(safe_class,item)
            if ok_cls and cls then
                local ok_cn,cname=pcall(safe_full_name,cls)
                if ok_cn and type(cname)=='string' and cname:find('BP_Item_Empty',1,true) then
                    is_empty=true
                end
            end
        end
    end
    local aim_method=tonumber(vr:get_mod_value('VR_AimMethod')) or 2
    local hand_index=(aim_method==3 or aim_method==5) and 0 or 1
    if s.hand_index~=hand_index then UEVR_UObjectHook.get_or_add_motion_controller_state(s.hand):set_hand(hand_index); s.hand_index=hand_index end
    local drawn=false
    if alive(item) and not is_empty then
        local has_drawn_prop=false
        local ok_cls,cls=pcall(safe_class,item)
        if ok_cls and cls and cls.find_property then
            local ok_prop,prop=pcall(safe_find_is_drawn,cls)
            if ok_prop and prop then has_drawn_prop=true end
        end
        if has_drawn_prop then
            drawn=(item.isDrawn and item.isDrawn~=0) and true or false
        else
            drawn=true
        end
    end
    local firearm=drawn
    local start,direction
    if firearm then
        local camera=assert(s.pc.PlayerCameraManager,'Firing camera unavailable')
        start=camera:K2_GetActorLocation();direction=camera:GetActorForwardVector()
        s.aim_source='game firing camera'
    else
        start=s.hand:K2_GetComponentLocation();direction=s.hand:GetForwardVector()
        s.aim_source='tracked right hand'
    end
    local hp,hr=s.head:K2_GetComponentLocation(),s.head:K2_GetComponentRotation()
    local dx,dy,dz=start.X+direction.X*275-hp.X,start.Y+direction.Y*275-hp.Y,start.Z+direction.Z*275-hp.Z
    local p,y,r=math.rad(hr.Pitch),math.rad(hr.Yaw),math.rad(hr.Roll)
    local cp,sp,cy,sy,cr,sr=math.cos(p),math.sin(p),math.cos(y),math.sin(y),math.cos(r),math.sin(r)
    local forward=dx*cp*cy+dy*cp*sy+dz*sp
    local right=dx*(sr*sp*cy-cr*sy)+dy*(sr*sp*sy+cr*cy)-dz*sr*cp
    local up=-dx*(cr*sp*cy+sr*sy)+dy*(cy*sr-cr*sp*sy)+dz*cr*cp
    -- Accepted v7 firing-ray projection, sampled after per-view tracking updates.
    -- This moves the artwork with the gun/hand instead of leaving a centered fallback.
    local distance=tonumber(vr:get_mod_value('UI_Distance')) or 2.75
    local height=tonumber(vr:get_mod_value('UI_Size')) or 2
    local xoffset=tonumber(vr:get_mod_value('UI_X_Offset')) or 0
    local yoffset=tonumber(vr:get_mod_value('UI_Y_Offset')) or 0
    local viewport=s.layout:GetViewportSize(s.pc)
    local logical_height=viewport.Y/math.max(s.layout:GetViewportScale(s.pc),.01)
    if forward<=1 or height<=0 then show_marker(s,false);return end
    local x=(right/forward*distance-xoffset)/height*logical_height
    local y=-(up/forward*distance-yoffset)/height*logical_height
    if vr:get_mod_value('UI_FollowView')~='true' or vr:get_mod_value('UI_OverlayType')~='0' then
        -- Use the existing native surface projection when the HUD is locked.
        -- Its coordinates run from 0 to 1 across the actual UI surface.
        vr.set_mod_value('VR_FarFarGunAimTarget',string.format('%.9g %.9g %.9g',right,up,-forward))
        local aim=vr:get_mod_value('VR_FarFarGunAim')
        local ax,ay
        if type(aim)=='string' then ax,ay=aim:match('^([%+%-%.%deE]+) ([%+%-%.%deE]+)$') end
        ax,ay=tonumber(ax),tonumber(ay)
        if not ax or not ay then show_marker(s,false);return end
        x,y=(ax-.5)*viewport.X/math.max(s.layout:GetViewportScale(s.pc),.01),(ay-.5)*logical_height
    end
    s.reticle_slot:SetPosition(xy(x,y));show_marker(s,true)
    s.aim_updates=(s.aim_updates or 0)+1
end
pcall(clear_aim)
uevr.sdk.callbacks.on_pre_calculate_stereo_view_offset(function()
    if not state or retry_time>0 then return end
    stage='per-view firing aim'
    local ok,err=pcall(render_aim,state)
    if not ok then
        pcall(clear_aim);recover_hud(err)
    end
end)
recover_hud=function(err)
    health.set('HUD','Recovering'); fault=stage..': '..tostring(err);pcall(cleanup);pcall(restore_title);last_pawn=nil;scan_time=0;retry_time=1
    uevr.params.functions.log_error('[Far Far VR HUD] Rebuilding private gun layer after '..fault)
    fs.write('farfarvr-hud-status.txt','ERROR in independent gun layer: '..fault)
end
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
uevr.sdk.callbacks.on_post_engine_tick(function(_,dt)
    retry_time=math.max(0,retry_time-dt);if retry_time>0 then return end
    local ok,err=pcall(function()
        stage='player/context';local pc=api:get_player_controller(0);if not pc then return end
        local pawn=pc.Pawn;title_height(pawn)
        -- HUD recovery can clear last_pawn without changing possession.
        -- Only a real possession transition may establish lobby forward.
        if pawn~=possessed_pawn then
            possessed_pawn=pawn
            if pawn and pawn:get_full_name():find('BP_Player_Lobby',1,true) then
                if vr.recenter_view then pcall(vr.recenter_view) end
            end
        end
        if pawn~=last_pawn then cleanup();last_pawn=pawn;scan_time=0 end
        if state and not valid_state(state) then cleanup();scan_time=0 end
        if not pawn or not pawn:get_full_name():find('BP_Player',1,true) then return end
        if not state then
            scan_time=scan_time+dt;if scan_time<1 then return end;scan_time=0
            local cls=api:find_uobject('WidgetBlueprintGeneratedClass /Game/Interfaces/UI_Player.UI_Player_C')
            if cls then for _,hud in ipairs(cls:get_objects_matching(false)) do
                if hud:IsInViewport() and hud:GetOwningPlayer()==pc then stage='private gun layer build';build(pawn,pc,hud);break end
            end end
        end
        stage='private gun artwork';if state then update(state,pc,dt) end

    end)
    if not ok then recover_hud(err) end
end)
uevr.sdk.callbacks.on_script_reset(function() cleanup();restore_title() end)
