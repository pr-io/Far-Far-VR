-- Far Far VR by PR_VR (https://www.youtube.com/@PR_VR)
-- Powered by UEVR created by praydog
-- Separate event artwork from the distant information HUD. Originals retain
-- animation/game ownership; only their viewport paint is suppressed after the
-- native effects-only compositor layer has acknowledged this private renderer.
local api,vr=uevr.api,uevr.params.vr
local health=require('farfarvr_health')
local state,retry,scan_elapsed=nil,0,0
local obj_cache = {}
local last_status,last_operating_status,status_elapsed=nil,nil,1
local function status(message)
    last_operating_status=message
    if message~=last_status and status_elapsed>=1 then
        last_status=message;status_elapsed=0;pcall(fs.write,'farfarvr-effects-status.txt',message)
    end
end
local function find(path)
    local object = obj_cache[path]
    if not object then
        object = api:find_uobject(path)
        assert(object, 'Missing '..path)
        obj_cache[path] = object
    end
    return object -- assert returns its message too; reflected constructors take one argument.
end
local function alive(o)
    if not o then return false end
    local ok,v=pcall(UEVR_UObjectHook.exists,o);return ok and v
end
local function attached(owner)
    return alive(owner) and (owner:IsInViewport() or (alive(owner:GetParent()) and owner:IsVisible()))
end
local redraw_methods=setmetatable({}, {__mode='k'})
local function redraw(component)
    -- RequestRenderUpdate also wakes a component whose tick was deferred while
    -- it had no render target. RequestRedraw alone only sets the redraw flag.
    local update=redraw_methods[component]
    if update==nil then update=component:get_class():find_function('RequestRenderUpdate')~=nil;redraw_methods[component]=update end
    if update then component:RequestRenderUpdate()
    else component:SetComponentTickEnabled(true);component:RequestRedraw() end
end
local function has(o,k) return alive(o) and o:get_class():find_property(k)~=nil end
local function native(path,values)
    local s=StructObject.new(find('ScriptStruct /Script/'..path));for k,v in pairs(values) do s[k]=v end;return s
end
local function xy(x,y) return native('CoreUObject.Vector2D',{X=x,Y=y}) end
local function transform(t,owner,angle)
    local p=owner and owner.RenderTransform
    return native('UMG.WidgetTransform',{Translation={X=0,Y=0},Scale={X=t.Scale.X*(p and p.Scale.X or 1),Y=t.Scale.Y*(p and p.Scale.Y or 1)},
        Shear={X=t.Shear.X,Y=t.Shear.Y},Angle=t.Angle+(p and p.Angle or 0)+(angle or 0)})
end
local function layout(slot)
    local l=slot.LayoutData
    return native('UMG.AnchorData',{Offsets={Left=l.Offsets.Left,Top=l.Offsets.Top,Right=l.Offsets.Right,Bottom=l.Offsets.Bottom},
        Anchors={Minimum={X=l.Anchors.Minimum.X,Y=l.Anchors.Minimum.Y},Maximum={X=l.Anchors.Maximum.X,Y=l.Anchors.Maximum.Y}},Alignment={X=l.Alignment.X,Y=l.Alignment.Y}})
end
local function bridge(k,v) vr.set_mod_value('VR_FarFarEffects'..k,v) end
local function paint(item,original)
    if alive(item.wrapper) then item.wrapper:SetVisibility(original and 4 or 2) end
end
local function restore_item(item)
    paint(item,true)
    if alive(item.source) then pcall(function()
        item.source:RemoveFromParent()
        if item.root and alive(item.owner) then item.owner.WidgetTree.RootWidget=item.source
        elseif alive(item.parent) then local slot=item.parent:AddChildToCanvas(item.source);slot:SetLayout(item.layout);slot:SetZOrder(item.z) end
    end) end
    if alive(item.wrapper) then pcall(function() item.wrapper:RemoveFromParent() end) end
    if alive(item.clone) then pcall(function() item.clone:RemoveFromParent() end) end
end
local function cleanup()
    pcall(bridge,'Source','0');pcall(bridge,'Active','false')
    local s=state;state=nil;if not s then return end
    for _,item in ipairs(s.items) do pcall(restore_item,item) end
    if alive(s.component) then pcall(function() s.component:SetWidget(nil);s.component:K2_DestroyComponent(s.pawn) end) end
    if alive(s.head) then pcall(function() UEVR_UObjectHook.remove_motion_controller_state(s.head);s.head:K2_DestroyComponent(s.pawn) end) end
    if alive(s.host) then pcall(function() s.host:RemoveFromParent() end) end
end
local function angle_delta(camera,head) return (camera-head+180)%360-180 end
local function damage_position(x,y,degrees)
    local r=math.rad(degrees);local c,s=math.cos(r),math.sin(r);x=x-.5;y=y-.5
    return .5+x*c-y*s,.5+x*s+y*c
end
local damage_hooks={}
local function install_damage_hooks()
    for _,spec in ipairs({
        {'BlueprintGeneratedClass /Game/Player/BP_Player.BP_Player_C','F_PlayerReceiveDamagesFeedback'},
        {'BlueprintGeneratedClass /Game/Mounts/BP_Mount.BP_Mount_C','F_ClientDamageFeedback'}}) do
        if not damage_hooks[spec[1]] then pcall(function()
            local cls=api:find_uobject(spec[1]);local fn=cls and cls:find_function(spec[2])
            if not fn then return end
            fn:hook_ptr(nil,function(_,pawn)
                -- Read-only post notification: original damage handling runs first.
                pcall(function()
                    local s=state;if not s or pawn~=s.pawn or not alive(s.pc.PlayerCameraManager) then return end
                    local yaw=s.pc.PlayerCameraManager:K2_GetActorRotation().Yaw
                    local damage=api:find_uobject('WidgetBlueprintGeneratedClass /Game/Interfaces/UI_DamageScreen.UI_DamageScreen_C')
                    if damage then for _,widget in ipairs(damage:get_objects_matching(false)) do
                        if attached(widget) and not s.damage_yaw[widget] then
                            local pc=widget:GetOwningPlayer()
                            if pc==s.pc or pc==nil then s.damage_yaw[widget]=yaw end
                        end
                    end end
                end)
            end)
            damage_hooks[spec[1]]=true
        end) end
    end
end
local function build(pawn,pc)
    local cls=api:find_uobject('WidgetBlueprintGeneratedClass /Game/Interfaces/UI_ScreenFlashes.UI_ScreenFlashes_C')
    if not cls then return end
    local s={pawn=pawn,pc=pc,items={},known={},damage_yaw=setmetatable({}, {__mode='k'}),redraw=0,warmup=0,dirty=true};state=s
    local identity=native('CoreUObject.Transform',{Translation={X=0,Y=0,Z=0},Rotation={X=0,Y=0,Z=0,W=1},Scale3D={X=1,Y=1,Z=1}})
    s.head=assert(pawn:AddComponentByClass(find('Class /Script/Engine.SceneComponent'),true,identity,false))
    UEVR_UObjectHook.activate();local tracking=assert(UEVR_UObjectHook.get_or_add_motion_controller_state(s.head));tracking:set_hand(2);tracking:set_permanent(true)
    s.library=find('Class /Script/UMG.WidgetBlueprintLibrary'):get_class_default_object()
    s.layout=find('Class /Script/UMG.WidgetLayoutLibrary'):get_class_default_object()
    s.slate=find('Class /Script/UMG.SlateBlueprintLibrary'):get_class_default_object()
    s.host=assert(s.library:Create(pc,cls,pc),'Effects host creation failed');s.host:StopAllAnimations()
    s.canvas=assert(api:spawn_object(find('Class /Script/UMG.CanvasPanel'),s.host.WidgetTree));s.host.WidgetTree.RootWidget=s.canvas
    s.component=assert(pawn:AddComponentByClass(find('Class /Script/UMG.WidgetComponent'),true,identity,false))
    s.component:SetWidgetSpace(0);s.component:SetDrawSize(xy(1024,1024))
    s.component:SetCollisionEnabled(0);s.component:SetRenderInMainPass(false);s.component:SetRenderInDepthPass(false);s.component:SetCastShadow(false)
    for _,method in ipairs({'SetRenderCustomDepth','SetVisibleInRayTracing','SetReceivesDecals'}) do
        if s.component:get_class():find_function(method) then s.component[method](s.component,false) end
    end
    if has(s.component,'BackgroundColor') then s.component.BackgroundColor=native('CoreUObject.LinearColor',{R=0,G=0,B=0,A=0}) end
    s.component.TickWhenOffscreen=true;s.component.bManuallyRedraw=true
    if s.component:get_class():find_function('SetTickWhenOffscreen') then s.component:SetTickWhenOffscreen(true) end
    s.component:SetComponentTickEnabled(true)
    s.component:SetVisibility(true,true)
    s.component:SetHiddenInGame(false,true)
    s.component:SetWindowFocusable(false)
    if has(s.component,'bReceiveHardwareInput') then s.component.bReceiveHardwareInput=false end
    s.component:SetWidget(s.host);redraw(s.component)
    s.token=string.format('%.0f',s.component:get_address());bridge('Source',s.token)
    install_damage_hooks()
end
local function capture(s,owner,source,directional)
    if not alive(source) or s.known[source] then return end
    local item={owner=owner,source=source,directional=directional,parent=source:GetParent()}
    if directional and alive(s.pc.PlayerCameraManager) then item.reference_yaw=(s.damage_yaw and s.damage_yaw[owner]) or s.pc.PlayerCameraManager:K2_GetActorRotation().Yaw end
    if item.parent then
        if not item.parent:get_class():find_function('AddChildToCanvas') then return end
        item.layout=layout(source.Slot);item.z=source.Slot.ZOrder
    elseif owner.WidgetTree.RootWidget==source then item.root=true else return end
    s.items[#s.items+1]=item;s.known[source]=item -- track partial setup for recovery
    item.wrapper=assert(api:spawn_object(find('Class /Script/UMG.CanvasPanel'),owner.WidgetTree))
    source:RemoveFromParent()
    if item.root then owner.WidgetTree.RootWidget=item.wrapper
    else local slot=item.parent:AddChildToCanvas(item.wrapper);slot:SetLayout(item.layout);slot:SetZOrder(item.z) end
    local slot=item.wrapper:AddChildToCanvas(source)
    -- The wrapper occupies the original slot. Its child fills that slot once;
    -- reapplying the original offsets here would move/resize the source twice.
    slot:SetAnchors(native('Slate.Anchors',{Minimum={X=0,Y=0},Maximum={X=1,Y=1}}));slot:SetPosition(xy(0,0));slot:SetSize(xy(0,0))
    paint(item,true)
    item.clone=assert(api:spawn_object(find('Class /Script/UMG.Image'),s.host.WidgetTree))
    item.slot=s.canvas:AddChildToCanvas(item.clone);item.slot:SetAnchors(native('Slate.Anchors',{Minimum={X=0,Y=0},Maximum={X=0,Y=0}}));item.slot:SetAlignment(xy(.5,.5))
    item.slot:SetPosition(xy(512,512));item.slot:SetSize(xy(1024,1024));item.slot:SetZOrder(directional and 20 or 10)
    s.dirty=true
end
local function scan(s)
    local specs={
        {'WidgetBlueprintGeneratedClass /Game/Interfaces/UI_ScreenFlashes.UI_ScreenFlashes_C',{'Image_Dash','Image_Shoot','Image_Heal','Image_Shield'}},
        {'WidgetBlueprintGeneratedClass /Game/Interfaces/UI_Player.UI_Player_C',{'HealDamageScreen'}},
        {'WidgetBlueprintGeneratedClass /Game/Interfaces/UI_DamageScreen.UI_DamageScreen_C',nil,true}}
    for _,spec in ipairs(specs) do
        local cls=api:find_uobject(spec[1])
        if cls then for _,owner in ipairs(cls:get_objects_matching(false)) do
            if owner~=s.host and attached(owner) then
                local pc=owner:GetOwningPlayer()
                if pc==s.pc or pc==nil then
                    if spec[3] then
                        local found=false;for _,item in ipairs(s.items) do if item.owner==owner then found=true;break end end
                        if not found then capture(s,owner,owner.WidgetTree.RootWidget,true) end
                    else for _,key in ipairs(spec[2]) do if has(owner,key) then capture(s,owner,owner[key],false) end end end
                end
            end
        end end
    end
end
local function eligible(s)
    return vr.is_runtime_ready() and vr.is_hmd_active() and not s.pc.bShowMouseCursor
        and vr:get_mod_value('VR_2DScreenMode')~='true' and not uevr.params.functions.is_drawing_ui()
end
local function sync(s,dt)
    local enabled=eligible(s)
    local ready=enabled and vr:get_mod_value('VR_FarFarEffectsReady')==s.token
    local active=false
    -- Projection is needed only for a visible directional effect, not every
    -- frame spent walking with an entirely transparent effects canvas.
    local head,correction,width,height
    local function projection()
        if head then return end
        local camera=s.pc.PlayerCameraManager
        local yaw=alive(camera) and camera:K2_GetActorRotation().Yaw or nil
        head=s.head:K2_GetComponentRotation()
        correction=yaw and angle_delta(yaw,head.Yaw) or 0
        local viewport=s.layout:GetViewportSize(s.pc);local scale=math.max(s.layout:GetViewportScale(s.pc),.01)
        width,height=viewport.X/scale,viewport.Y/scale
    end
    for i=#s.items,1,-1 do
        local item=s.items[i]
        if not alive(item.source) or not attached(item.owner) then
            pcall(restore_item,item);s.known[item.source]=nil;table.remove(s.items,i);s.dirty=true
        else
            local src,clone=item.source,item.clone
            local tint=src.ColorAndOpacity;local owner_tint=has(item.owner,'ColorAndOpacity') and item.owner.ColorAndOpacity
            local opacity=src.RenderOpacity*(item.owner.RenderOpacity or 1)
            local parent=item.parent;local depth=0
            while alive(parent) and parent~=item.owner and depth<12 do opacity=opacity*(parent.RenderOpacity or 1);parent=parent:GetParent();depth=depth+1 end
            local visibility=item.owner:IsVisible() and src:GetVisibility() or 2
            local visible=enabled and opacity*tint.A*(owner_tint and owner_tint.A or 1)>0.001 and (visibility==0 or visibility==3 or visibility==4)
            if visible then
                -- Keep the full original brush/material and every animation
                -- property when painted. Never replace or throttle live effects.
                clone:SetBrush(src.Brush)
                clone:SetColorAndOpacity(native('CoreUObject.LinearColor',{R=tint.R*(owner_tint and owner_tint.R or 1),G=tint.G*(owner_tint and owner_tint.G or 1),B=tint.B*(owner_tint and owner_tint.B or 1),A=tint.A*(owner_tint and owner_tint.A or 1)}))
                clone:SetRenderOpacity(opacity);clone:SetVisibility(visibility)
                local x,y,w,h=.5,.5,1,1
                local angle=0
                if item.directional then
                    projection()
                    local ok,a,b,c,d=pcall(function()
                        local g=src:GetCachedGeometry();local size=s.slate:GetLocalSize(g)
                        local center=s.slate:LocalToAbsolute(g,xy(size.X*.5,size.Y*.5))
                        local point=s.slate:AbsoluteToLocal(s.layout:GetViewportWidgetGeometry(s.pc),center)
                        return point.X/width,point.Y/height,size.X/width,size.Y/height
                    end)
                    if ok and c>0 and d>0 then item.bounds={a,b,c,d} end
                    if item.bounds then x,y,w,h=table.unpack(item.bounds) end
                    angle=item.reference_yaw and angle_delta(item.reference_yaw,head.Yaw) or correction
                    x,y=damage_position(x,y,angle)
                end
                item.slot:SetPosition(xy(x*1024,y*1024));item.slot:SetSize(xy(w*1024,h*1024))
                clone:SetRenderTransform(transform(src.RenderTransform,item.directional and item.owner or nil,angle))
                item.painted=true
            elseif item.painted~=false then
                clone:SetVisibility(2);item.painted=false
            end
            local usable=not item.directional or item.bounds~=nil
            if visible and not usable then clone:SetVisibility(2) end
            if visible and usable then active=true end
            -- Visibility writes also cross the reflection bridge. Update this
            -- only at acknowledgement/context transitions, including recovery.
            local original=not (ready and usable)
            if item.original~=original then paint(item,original);item.original=original end
        end
    end
    if not active and s.was_active then s.dirty=true end
    s.was_active=active
    bridge('Active',enabled and active and 'true' or 'false')
    s.redraw=s.redraw+dt
    s.warmup=(s.warmup or 0)+dt
    if s.dirty or (active and s.redraw>=1/60) or (enabled and not ready and s.warmup>=.25) then
        redraw(s.component);s.dirty=false;s.redraw=0;s.warmup=0
    end
    s.ready=ready;s.active=enabled and active; health.set('Effects',ready and 'Active' or 'Original effects (fallback)')
    if enabled then
        if ready then status('Active: independent full-view effects; sources='..#s.items)
        elseif status_elapsed>=1 then
            local target=s.component:GetRenderTarget()
            status(not target and 'Waiting: offscreen effects render target has not been created'
                or ('Waiting: native effects compositor acknowledgement; sources='..#s.items))
        end
    else
        status('Paused: cursor='..tostring(s.pc.bShowMouseCursor)..'; VR menu='..tostring(uevr.params.functions.is_drawing_ui())..'; cinema='..tostring(vr:get_mod_value('VR_2DScreenMode')))
    end
end
uevr.sdk.callbacks.on_post_engine_tick(function(_,dt)
    status_elapsed=status_elapsed+(dt or .016)
    retry=math.max(0,retry-dt);if retry>0 then return end
    local ok,err=pcall(function()
        if not vr.is_runtime_ready() or not vr.is_hmd_active() then
            if state then cleanup();fs.write('farfarvr-effects-status.txt','Headset inactive; last render state: '..(last_operating_status or 'not initialized')) end
            return
        end
        local backend=vr:get_mod_value('VR_FarFarEffectsReady')
        if not backend or backend=='' then return end
        local pc=api:get_player_controller(0);local pawn=pc and pc.Pawn
        if state and (state.pawn~=pawn or not alive(state.component) or not alive(state.host) or not alive(state.head)) then cleanup() end
        local name=pawn and pawn:get_full_name() or ''
        if not pawn or not (name:find('BP_Player',1,true) or name:find('BP_Mount_C ',1,true)) then return end
        if not state then build(pawn,pc) end;if not state then return end
        scan_elapsed=scan_elapsed+dt;if scan_elapsed>=(#state.items>=6 and 1.0 or 0.5) then scan_elapsed=0;scan(state) end
        sync(state,dt)

    end)
    if not ok then health.set('Effects','Recovering; original effects restored');cleanup();retry=1;uevr.params.functions.log_error('[Far Far VR Effects] '..tostring(err));fs.write('farfarvr-effects-status.txt','ERROR; original effects restored: '..tostring(err)) end
end)
uevr.sdk.callbacks.on_script_reset(cleanup)
