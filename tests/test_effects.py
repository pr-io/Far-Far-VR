"""Execute the effects-only mirror and ownership/lifecycle logic. Not headset acceptance."""
from pathlib import Path
import sys,unittest
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/lua-test'))
from lupa.lua54 import LuaRuntime
HARNESS=r'''
files={}; fs={write=function(k,v) files[k]=v end};values={};cinema=false;menu=false;hmd=true;runtime=true
function capture(fn,key)
 for i=1,80 do local k,v=debug.getupvalue(fn,i);if k==key then return v end;if not k then break end end
 error('Missing '..key)
end
function assign(fn,key,value)
 for i=1,80 do local k=debug.getupvalue(fn,i);if k==key then debug.setupvalue(fn,i,value);return end;if not k then break end end
 error('Missing '..key)
end
StructObject={new=function(...) assert(select('#',...)==1,'Native struct constructor accepts exactly one argument');return {struct_path=(...).path} end}
UEVR_UObjectHook={exists=function(o) return o.valid~=false end,remove_motion_controller_state=function(o) o.untracked=true end,activate=function() end}
uevr={api={find_uobject=function(_,p) return {path=p} end},params={vr={
 get_mod_value=function(_,k) if k=='VR_2DScreenMode' then return cinema and 'true' or 'false' end;return values[k] end,
 set_mod_value=function(k,v) values[k]=v end,
 is_runtime_ready=function() return runtime end,is_hmd_active=function() return hmd end},functions={is_drawing_ui=function() return menu end,log_error=function() end}},
 sdk={callbacks={on_post_engine_tick=function(f) tick=f end,on_script_reset=function(f) reset=f end}}}
function widget()
 local n={RenderOpacity=1,visible=true,RenderTransform={Translation={X=0,Y=0},Scale={X=1,Y=1},Shear={X=0,Y=0},Angle=0},ColorAndOpacity={R=1,G=1,B=1,A=1}}
 function n:get_class() return {find_property=function(_,k) if self[k]~=nil then return {} end end,find_function=function(_,k) if self[k] then return {} end end} end
 function n:GetParent() return self.parent end
 function n:RemoveFromParent() self.parent=nil;self.removed=true end
 function n:IsInViewport() return self.in_view~=false end
 function n:IsVisible() return self.visible end
 function n:GetVisibility() return self.visibility or 3 end
 function n:SetVisibility(v) self.visibility=v end
 function n:SetComponentTickEnabled(v) self.tick_enabled=v end
 function n:SetHiddenInGame(v) self.hidden_in_game=v end
 function n:GetRenderTarget() return self.render_target or {} end
 function n:SetBrush(v) self.brush=v end
 function n:SetColorAndOpacity(v) self.tint=v end
 function n:SetRenderOpacity(v) self.opacity=v end
 function n:SetRenderTransform(v) self.transform=v end
 function n:GetCachedGeometry() return self.geometry or {size={X=200,Y=100},center={X=1200,Y=540}} end
 function n:AddChildToCanvas(child)
  child.parent=self;local slot={ZOrder=0,LayoutData={Offsets={Left=0,Top=0,Right=64,Bottom=64},Anchors={Minimum={X=0,Y=0},Maximum={X=1,Y=1}},Alignment={X=0,Y=0}}}
  for _,key in ipairs({'Anchors','Alignment','Position','Size','ZOrder','Layout'}) do slot['Set'..key]=function(self,v) self[key]=v end end
  child.Slot=slot;return slot
 end
 return n
end
function setup(directional)
 local source,clone,wrapper=widget(),widget(),widget()
 source.Brush={ResourceObject={native_material=true}};source.RenderOpacity=.8
 local owner=widget();owner.WidgetTree={RootWidget=wrapper};local parent=widget();parent:AddChildToCanvas(source)
 local component=widget();component.redraws=0;component.RequestRedraw=function(self) self.redraws=self.redraws+1 end
 local pc={bShowMouseCursor=false,PlayerCameraManager={K2_GetActorRotation=function() return {Yaw=100} end}}
 local slot=widget():AddChildToCanvas(clone)
 local s={pc=pc,pawn={},head={K2_GetComponentRotation=function() return {Yaw=10} end},component=component,token='123',redraw=0,dirty=false,
  layout={GetViewportSize=function() return {X=1920,Y=1080} end,GetViewportScale=function() return 1 end,GetViewportWidgetGeometry=function() return {} end},
  slate={GetLocalSize=function(_,g) return g.size end,LocalToAbsolute=function(_,g,p) return g.center end,AbsoluteToLocal=function(_,g,p) return p end}}
 s.items={{owner=owner,source=source,clone=clone,wrapper=wrapper,parent=parent,slot=slot,directional=directional,reference_yaw=directional and 40 or nil}}
 s.known={[source]=s.items[1]};return s
end
'''
class EffectsTests(unittest.TestCase):
 def setUp(self):
  self.lua=LuaRuntime()
  self.lua.execute("package.path = " + repr(str(ROOT / "Profile/scripts/?.lua").replace("\\", "/")) + " .. ';' .. package.path");self.lua.execute(HARNESS)
  self.lua.execute((ROOT/'Profile/scripts/farfarvr_effects.lua').read_text(encoding='utf-8'))
  self.lua.execute("sync=capture(tick,'sync'); cleanup=capture(tick,'cleanup'); build=capture(tick,'build'); scan=capture(tick,'scan'); capture_image=capture(scan,'capture'); damage_position=capture(sync,'damage_position')")
 def check(self,s):self.lua.execute(s)
 def test_transparent_effects_do_not_copy_artwork_project_or_redraw(self):
  self.check('''
  local s=setup(true);values.VR_FarFarEffectsReady='123'
  s.items[1].source.RenderOpacity=0
  s.pc.PlayerCameraManager.K2_GetActorRotation=function() error('Invisible effect queried camera') end
  s.head.K2_GetComponentRotation=function() error('Invisible effect queried head') end
  s.layout.GetViewportSize=function() error('Invisible effect queried geometry') end
  s.items[1].clone.SetBrush=function() error('Invisible effect copied brush') end
  s.items[1].slot.SetPosition=function() error('Invisible effect changed layout') end
  sync(s,.02)
  local writes=0
  s.items[1].clone.SetVisibility=function() writes=writes+1 end
  s.items[1].wrapper.SetVisibility=function() writes=writes+1 end
  for i=1,1200 do sync(s,1/120) end
  assert(writes==0 and s.component.redraws==0 and not s.active)
  ''')
 def test_reappearing_effect_copies_current_artwork_and_native_animation_without_delay(self):
  self.check('''
  local s=setup(false);values.VR_FarFarEffectsReady='123'
  local item=s.items[1];item.source.RenderOpacity=0;sync(s,.02)
  item.source.Brush={ResourceObject={updated_material=true}}
  item.source.RenderTransform.Angle=37;item.source.RenderOpacity=.7
  sync(s,.02)
  assert(s.active and item.clone.opacity==.7 and item.clone.transform.Angle==37)
  assert(item.clone.brush.ResourceObject==item.source.Brush.ResourceObject and item.clone.visibility==3)
  item.source.RenderOpacity=0;local n=s.component.redraws;sync(s,.02)
  assert(not s.active and item.clone.visibility==2 and s.component.redraws==n+1)
  for i=1,120 do sync(s,1/120) end
  assert(s.component.redraws==n+1)
  item.source.RenderOpacity=.5;sync(s,.02)
  assert(s.active and item.clone.visibility==3 and item.clone.opacity==.5)
  ''')
 def test_no_continuous_blank_redraw_when_native_ack_unavailable(self):
  self.check('''
  local s=setup(false);s.items[1].source.RenderOpacity=0;s.dirty=true
  for i=1,2400 do sync(s,1/120) end
  assert(s.component.redraws==9 and not s.ready and s.items[1].wrapper.visibility==4)
  local count=s.component.redraws
  for i=1,2400 do sync(s,1/120) end
  assert(s.component.redraws==count)
  -- Visible artwork still wakes the target after blank warmup is exhausted.
  s.items[1].source.RenderOpacity=.8;sync(s,.02)
  assert(s.active and s.component.redraws==count+1)
  values.VR_FarFarEffectsReady='123';sync(s,.02)
  assert(s.ready and s.items[1].wrapper.visibility==2)
  ''')
 def test_originals_visible_until_native_ack_and_live_material_timing_preserved(self):
  self.check('''
  local s=setup(false);sync(s,.02);local item=s.items[1]
  assert(item.wrapper.visibility==4 and not s.ready)
  values.VR_FarFarEffectsReady='123';sync(s,.02)
  assert(s.ready and item.wrapper.visibility==2)
  assert(item.clone.brush.ResourceObject==item.source.Brush.ResourceObject and item.clone.opacity==.8 and item.source.RenderOpacity==.8)
  item.source.RenderOpacity=.2;sync(s,.02);assert(item.clone.opacity==.2 and item.source.RenderOpacity==.2)
  values.VR_FarFarEffectsReady='999';sync(s,.02);assert(item.wrapper.visibility==4)
  ''')
 def test_flashes_fill_effects_canvas_without_moving_source_layout(self):
  self.check('''
  local s=setup(false);local slot=s.items[1].source.Slot;sync(s,.02)
  assert(s.items[1].slot.Position.X==512 and s.items[1].slot.Position.Y==512)
  assert(s.items[1].slot.Size.X==1024 and s.items[1].slot.Size.Y==1024)
  assert(s.items[1].source.Slot==slot and s.items[1].source.RenderTransform.Angle==0)
  ''')
 def test_direction_uses_head_heading_and_not_later_gun_camera_motion(self):
  self.check('''
  local s=setup(true);values.VR_FarFarEffectsReady='123'
  s.items[1].source.RenderTransform.Angle=15
  s.items[1].owner.RenderTransform.Angle=5
  sync(s,.02);assert(s.items[1].clone.transform.Angle==50)
  local x=s.items[1].slot.Position.X
  s.pc.PlayerCameraManager.K2_GetActorRotation=function() return {Yaw=-80} end
  sync(s,.02);assert(s.items[1].clone.transform.Angle==50 and s.items[1].slot.Position.X==x)
  s.head.K2_GetComponentRotation=function() return {Yaw=30} end
  sync(s,.02);assert(s.items[1].clone.transform.Angle==30 and s.items[1].slot.Position.X~=x)
  ''')
 def test_unpainted_directional_widget_stays_on_original_until_geometry_ready(self):
  self.check('''
  local s=setup(true);values.VR_FarFarEffectsReady='123';s.items[1].source.geometry={size={X=0,Y=0},center={X=0,Y=0}}
  sync(s,.02);assert(s.items[1].wrapper.visibility==4 and s.items[1].clone.visibility==2)
  s.items[1].source.geometry=nil;sync(s,.02);assert(s.items[1].wrapper.visibility==2)
  ''')
 def test_menus_cinema_inactive_runtime_restore_originals(self):
  self.check('''
  local s=setup(false);values.VR_FarFarEffectsReady='123';sync(s,.02);assert(s.ready)
  menu=true;sync(s,.02);assert(not s.ready and s.items[1].wrapper.visibility==4 and values.VR_FarFarEffectsActive=='false')
  menu=false;cinema=true;sync(s,.02);assert(not s.ready)
  cinema=false;hmd=false;sync(s,.02);assert(not s.ready)
  hmd=true;runtime=false;sync(s,.02);assert(not s.ready)
  ''')
 def test_opacity_tint_and_native_visibility_are_retained_and_idle_clears_once(self):
  self.check('''
  local s=setup(false);local item=s.items[1];values.VR_FarFarEffectsReady='123'
  item.owner.RenderOpacity=.5;item.parent.RenderOpacity=.5;item.owner.ColorAndOpacity.A=.25
  sync(s,.02);assert(item.clone.opacity==.2 and item.clone.tint.A==.25)
  item.source.RenderOpacity=0;local before=s.component.redraws
  sync(s,.02);assert(values.VR_FarFarEffectsActive=='false' and s.component.redraws==before+1)
  sync(s,.02);assert(s.component.redraws==before+1)
  item.source.RenderOpacity=.8;item.owner.visible=false;sync(s,.02);assert(item.clone.visibility==2 and values.VR_FarFarEffectsActive=='false')
  ''')
 def test_root_and_canvas_ownership_are_restored_on_cleanup(self):
  self.check('''
  uevr.api.spawn_object=function() return widget() end
  local s=setup(false);s.items={};s.known={};s.host={WidgetTree={}};s.canvas=widget()
  local owner=widget();local source=widget();owner.WidgetTree={RootWidget=source}
  capture_image(s,owner,source,true);assert(owner.WidgetTree.RootWidget~=source and #s.items==1)
  local original=widget();local parent=widget();parent:AddChildToCanvas(original)
  capture_image(s,owner,original,false);assert(original:GetParent()~=parent)
  assert(original.Slot.Anchors.Minimum.X==0 and original.Slot.Anchors.Maximum.X==1)
  assert(original.Slot.Position.X==0 and original.Slot.Size.X==0 and original.Slot.Layout==nil)
  assert(s.items[2].wrapper.Slot.Layout.Offsets.Right==64)
  s.component.SetWidget=function() end;s.component.K2_DestroyComponent=function(self) self.destroyed=true end
  s.head.K2_DestroyComponent=function(self) self.destroyed=true end;s.host.RemoveFromParent=function() end
  assign(tick,'state',s);cleanup()
  assert(owner.WidgetTree.RootWidget==source and original:GetParent()==parent)
  assert(s.component.destroyed and s.head.destroyed and values.VR_FarFarEffectsSource=='0')
  ''')
 def test_whitelist_does_not_move_health_crosshair_or_hit_x(self):
  self.check('''
  uevr.api.spawn_object=function() return widget() end
  local s=setup(false);s.items={};s.known={};s.host={WidgetTree={}};s.canvas=widget()
  local owner=widget();owner.WidgetTree={};owner.GetOwningPlayer=function() return s.pc end
  local parent=widget();for _,key in ipairs({'HealDamageScreen','Crosshair_Top','HitImage','HealProgressBar'}) do owner[key]=widget();parent:AddChildToCanvas(owner[key]) end
  local health_parent=owner.HealProgressBar:GetParent();local cross_parent=owner.Crosshair_Top:GetParent()
  uevr.api.find_uobject=function(_,name)
   if name:find('UI_Player.UI_Player_C',1,true) then return {get_objects_matching=function() return {owner} end} end
   if name:find('WidgetBlueprintGeneratedClass',1,true) then return nil end
   return {path=name}
  end
  scan(s);assert(#s.items==1 and s.items[1].source==owner.HealDamageScreen)
  assert(owner.HealProgressBar:GetParent()==health_parent and owner.Crosshair_Top:GetParent()==cross_parent and owner.HitImage:GetParent()==cross_parent)
  ''')
 def test_private_renderer_never_enters_scene_color_depth_or_input(self):
  self.check("""
  local head={};local component=widget();local calls={}
  component.BackgroundColor={};component.bReceiveHardwareInput=true
  for _,key in ipairs({'SetWidgetSpace','SetDrawSize','SetCollisionEnabled','SetRenderInMainPass','SetRenderInDepthPass','SetCastShadow','SetRenderCustomDepth','SetVisibleInRayTracing','SetReceivesDecals','SetWindowFocusable','SetWidget'}) do
   component[key]=function(_,v) if key=='SetDrawSize' then assert(v.struct_path=='ScriptStruct /Script/CoreUObject.Vector2D','SetDrawSize requires Vector2D') end;calls[key]=v end
  end
  component.RequestRedraw=function() calls.redraw=true end
  component.get_address=function() return 123 end
  local host=widget();host.WidgetTree={};host.StopAllAnimations=function() calls.stopped=true end
  local lib={Create=function() return host end}
  uevr.api.find_uobject=function(_,name)
   if name:find('WidgetBlueprintLibrary',1,true) then return {get_class_default_object=function() return lib end} end
   if name:find('WidgetLayoutLibrary',1,true) or name:find('SlateBlueprintLibrary',1,true) then return {get_class_default_object=function() return {} end} end
   return {path=name}
  end
  uevr.api.spawn_object=function() return widget() end
  UEVR_UObjectHook.get_or_add_motion_controller_state=function() return {set_hand=function(_,h) calls.hand=h end,set_permanent=function() end} end
  local pawn={AddComponentByClass=function(_,c) return c.path:find('WidgetComponent',1,true) and component or head end}
  build(pawn,{})
  assert(calls.SetRenderInMainPass==false and calls.SetRenderInDepthPass==false and calls.SetCastShadow==false)
  assert(calls.SetVisibleInRayTracing==false and calls.SetRenderCustomDepth==false and calls.SetReceivesDecals==false)
  assert(calls.SetCollisionEnabled==0 and calls.SetWindowFocusable==false and component.bReceiveHardwareInput==false)
  assert(calls.SetDrawSize.X==1024 and calls.SetDrawSize.Y==1024 and component.TickWhenOffscreen and component.bManuallyRedraw)
  assert(component.BackgroundColor.A==0 and calls.hand==2 and calls.SetWidget==host and calls.stopped and calls.redraw)
  assert(values.VR_FarFarEffectsSource=='123')
  """)
 def test_reflection_helper_does_not_forward_assert_message_to_constructor(self):
  self.check("""
  local native=capture(build,'native')
  local calls=0
  StructObject.new=function(...) assert(select('#',...)==1);calls=calls+1;return {} end
  native('CoreUObject.Transform',{Translation={X=0,Y=0,Z=0},Rotation={X=0,Y=0,Z=0,W=1},Scale3D={X=1,Y=1,Z=1}})
  assert(calls==1)
  """)

 def test_incoming_damage_notification_is_read_only_and_captures_creation_heading(self):
  self.check("""
  local s=setup(false);s.items={};s.known={};s.damage_yaw={};s.host={WidgetTree={}};s.canvas=widget()
  local owner=widget();local image=widget();owner.WidgetTree={RootWidget=image}
  owner.GetOwningPlayer=function() return s.pc end
  local callbacks={};local f={hook_ptr=function(_,pre,post) assert(pre==nil);callbacks[#callbacks+1]=post end}
  uevr.api.find_uobject=function(_,name)
   if name:find('/Game/Player/BP_Player.',1,true) then return {find_function=function(_,n) assert(n=='F_PlayerReceiveDamagesFeedback');return f end} end
   if name:find('UI_DamageScreen',1,true) then return {get_objects_matching=function() return {owner} end} end
   if name:find('BP_Mount',1,true) then return nil end
   return {path=name}
  end
  uevr.api.spawn_object=function() return widget() end
  assign(tick,'state',s)
  capture(build,'install_damage_hooks')();assert(#callbacks==1)
  s.pc.PlayerCameraManager.K2_GetActorRotation=function() return {Yaw=40} end
  assert(callbacks[1](nil,{})==nil and s.damage_yaw[owner]==nil)
  assert(callbacks[1](nil,s.pawn)==nil and s.damage_yaw[owner]==40)
  s.pc.PlayerCameraManager.K2_GetActorRotation=function() return {Yaw=100} end
  capture_image(s,owner,image,true);assert(s.items[1].reference_yaw==40)
  capture(build,'install_damage_hooks')();assert(#callbacks==1)
  """)
 def test_headset_idle_cannot_allocate_effects_or_leave_originals_hidden(self):
  self.check("""
  local s=setup(false);values.VR_FarFarEffectsReady='123';sync(s,.02);assert(s.items[1].wrapper.visibility==2)
  assign(tick,'state',s);hmd=false
  uevr.api.get_player_controller=function() error('Idle must not query/build a gameplay renderer') end
  tick(nil,.02)
  assert(values.VR_FarFarEffectsSource=='0' and values.VR_FarFarEffectsActive=='false')
  assert(s.items[1].wrapper.visibility==4 and capture(tick,'state')==nil)
  """)
if __name__=='__main__':unittest.main()
