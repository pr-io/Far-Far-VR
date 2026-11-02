"""Private reticle ownership and native-art lifecycle. Software checks, not wearer acceptance."""
from pathlib import Path
import sys,unittest
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/lua-test'))
from lupa.lua54 import LuaRuntime
HARNESS=r'''
files={};values={VR_FarFarReticleReady='0',VR_2DScreenMode='false',VR_WorldScale='1'};writes={};runtime=true;hmd=true;menu=false
fs={write=function(k,v)files[k]=v end}
function node(kind)
 local n={kind=kind,valid=true,children={},RenderOpacity=1,ColorAndOpacity={R=1,G=1,B=1,A=1},RenderTransform={Translation={X=0,Y=0},Scale={X=1,Y=1},Shear={X=0,Y=0},Angle=0},Brush={native_material=kind}}
 function n:get_class()return {find_property=function(_,k)if n[k]~=nil then return {} end end,find_function=function(_,k)if type(n[k])=='function' then return {} end end}end
 function n:IsInViewport()return self.in_view~=false end
 function n:GetParent()return self.parent end
 function n:RemoveFromParent()if self.parent then for i,v in ipairs(self.parent.children)do if v==self then table.remove(self.parent.children,i);break end end end;self.parent=nil;self.removed=true end
 function n:AddChildToCanvas(child)
  child:RemoveFromParent();child.parent=self;table.insert(self.children,child)
  local slot={ZOrder=0,LayoutData={Offsets={Left=0,Top=0,Right=256,Bottom=256},Anchors={Minimum={X=.5,Y=.5},Maximum={X=.5,Y=.5}},Alignment={X=.5,Y=.5}}}
  for _,k in ipairs({'Anchors','Alignment','Position','Size','ZOrder'})do slot['Set'..k]=function(self,v)self[k]=v end end
  function slot:SetLayout(v)self.LayoutData=v end;child.Slot=slot;return slot
 end
 for _,k in ipairs({'Visibility','Brush','ColorAndOpacity','RenderOpacity','RenderTransform','Widget','WidgetSpace','DrawSize','CollisionEnabled','RenderInMainPass','RenderInDepthPass','CastShadow','RenderCustomDepth','VisibleInRayTracing','ReceivesDecals','WindowFocusable'})do n['Set'..k]=function(self,v)if k=='DrawSize' then assert(v.struct_path=='ScriptStruct /Script/CoreUObject.Vector2D','SetDrawSize requires Vector2D, not IntPoint')end;self['set_'..k]=v end end
 function n:GetVisibility()return self.visibility or 0 end
 function n:StopAllAnimations()self.stopped=true end
 function n:RequestRedraw()self.redraws=(self.redraws or 0)+1 end
 function n:K2_DestroyComponent()self.destroyed=true end
 function n:get_address()return 123 end
 n.location={X=0,Y=0,Z=0};n.rotation={Pitch=0,Yaw=0,Roll=0};n.forward={X=1,Y=0,Z=0}
 function n:K2_GetComponentLocation()return self.location end
 function n:K2_GetComponentRotation()return self.rotation end
 function n:GetForwardVector()return self.forward end
 function n:K2_GetActorLocation()return self.location end
 function n:GetActorForwardVector()return self.forward end
 return n
end
root=node('information HUD');hud=node('UI_Player');hud.WidgetTree={RootWidget=root};hud.GetOwningPlayer=function()return pc end
for i,k in ipairs({'Crosshair_Top','HitImage','HitShield'})do local image=node(k);root:AddChildToCanvas(image);image.Slot.ZOrder=i;hud[k]=image end
hud.HitImage.Slot.LayoutData.Offsets={Left=-16,Top=-16,Right=32,Bottom=32};hud.HitImage.Slot.LayoutData.Alignment={X=0,Y=0}
health=node('health');root:AddChildToCanvas(health)
pawn=node('pawn');function pawn:get_full_name()return 'BP_Player_C Test' end
function pawn:AddComponentByClass(cls)
 if cls.path:find('SceneComponent',1,true)then return node('tracking anchor')end
 assert(cls.path:find('WidgetComponent',1,true));component=node('private component');component.BackgroundColor={};component.bReceiveHardwareInput=false;return component
end
pc={Pawn=pawn,bShowMouseCursor=false}
library={Create=function()host=node('private host');host.in_view=false;host.WidgetTree={RootWidget=node('initial root')};return host end}
api={get_player_controller=function()return pc end,get_local_pawn=function()return nil end,
 find_uobject=function(_,path)
  if path:find('WidgetBlueprintLibrary',1,true)then return {get_class_default_object=function()return library end}end
  if path:find('UI_Player.UI_Player_C',1,true)then return {get_objects_matching=function()return {hud}end}end
  return {path=path}
 end,spawn_object=function(_,cls,outer)local o=node(cls.path);o.outer=outer;return o end}
StructObject={new=function(...)assert(select('#',...)==1);return {native=true,struct_path=(...).path}end}
UEVR_UObjectHook={exists=function(o)return o and o.valid~=false end,activate=function()end,
 remove_motion_controller_state=function(o)o.untracked=true end,
 get_or_add_motion_controller_state=function(o)return {set_hand=function(_,v)o.hand=v end,set_permanent=function()end}end}
uevr={api=api,params={vr={is_runtime_ready=function()return runtime end,is_hmd_active=function()return hmd end,
 get_mod_value=function(_,k)assert(not k:find('UI_',1,true),'Information HUD settings must not be read');return values[k]end,
 set_mod_value=function(k,v)assert(k:find('VR_FarFarReticle',1,true)==1);values[k]=v;table.insert(writes,{k,v})end},functions={log_info=function()end,log_error=function()end,is_drawing_ui=function()return menu end}},
 sdk={callbacks={on_post_engine_tick=function(f)tick=f end,on_pre_calculate_stereo_view_offset=function(f)view_tick=f end,on_script_reset=function(f)reset=f end}}}
function capture(fn,key)for i=1,80 do local k,v=debug.getupvalue(fn,i);if k==key then return v end;if not k then break end end;error('Missing '..key)end
'''
class HudTests(unittest.TestCase):
 def setUp(self):
  self.lua=LuaRuntime();self.lua.execute(HARNESS);self.lua.execute((ROOT/'Profile/scripts/farfarvr_hud.lua').read_text(encoding='utf-8'))
  self.lua.execute("build=capture(tick,'build');update=capture(tick,'update');cleanup=capture(tick,'cleanup');valid_state=capture(tick,'valid_state');title_height=capture(tick,'title_height');function built()build(pawn,pc,hud);return capture(tick,'state')end")
 def check(self,s):self.lua.execute(s)
 def test_crosshair_and_hit_x_are_outside_the_information_hud(self):
  self.check("""local s=built();assert(s.host~=hud and s.reticle==s.host.WidgetTree.RootWidget and hud.WidgetTree.RootWidget==root)
  for _,i in ipairs(s.images)do assert(i.clone:GetParent()==s.reticle and i.clone.outer==host.WidgetTree);assert(i.source:GetParent()==i.wrapper and i.wrapper:GetParent()==root)end
  assert(health:GetParent()==root and #root.children==4 and #s.images==3)
  assert(values.VR_FarFarReticleSource=='123')""")
 def test_empty_hand_dot_uses_the_same_independent_right_hand_layer(self):
  self.check("""local s=built();pawn.Weapon=nil;pawn.isDrawn=false
  hud.Crosshair_Top.Brush={native_material='original empty-hand dot'}
  update(s,pc,.02)
  assert(s.showing and values.VR_FarFarReticleActive=='true')
  assert(s.images[1].clone.set_Brush.native_material=='original empty-hand dot')
  assert(s.images[1].clone:GetParent()==s.reticle and s.reticle~=root)
  hud.Crosshair_Top.Brush={native_material='original gun crosshair'};pawn.isDrawn=true
  update(s,pc,.02)
  assert(s.images[1].clone.set_Brush.native_material=='original gun crosshair' and values.VR_FarFarReticleSource=='123')""")

 def test_private_renderer_has_no_scene_geometry_or_input(self):
  self.check("""local s=built();assert(component.set_DrawSize.X==256 and component.set_DrawSize.Y==256)
  assert(component.set_RenderInMainPass==false and component.set_RenderInDepthPass==false and component.set_CastShadow==false)
  assert(component.set_VisibleInRayTracing==false and component.set_CollisionEnabled==0 and component.set_WindowFocusable==false)
  assert(component.BackgroundColor.A==0 and component.bReceiveHardwareInput==false and component.TickWhenOffscreen and component.bManuallyRedraw)
  assert(component.set_Widget==host and not host:IsInViewport() and host.stopped)""")
 def test_native_material_and_hit_animation_state_are_preserved(self):
  self.check("""local s=built();hud.HitImage.RenderOpacity=.7;hud.HitImage.RenderTransform.Angle=15;update(s,pc,.02)
  assert(s.images[2].clone.set_Brush==hud.HitImage.Brush and s.images[2].clone.set_RenderOpacity==.7)
  assert(s.images[2].clone.set_RenderTransform.Angle==15 and hud.HitImage.RenderOpacity==.7)
  assert(s.images[2].slot.Position.X==-16 and s.images[2].slot.Size.X==32)""")
 def test_settings_and_menu_state_cannot_attach_it_to_hud(self):
  self.check("""local s=built();update(s,pc,.02);assert(s.showing and values.VR_FarFarReticleActive=='true')
  values.UI_Distance='99';values.UI_Size='.1';values.UI_FollowView='false';values.UI_OverlayType='1';update(s,pc,.02)
  assert(s.showing and s.images[2].clone:GetParent()==s.reticle and s.reticle~=root)
  menu=true;update(s,pc,.02);assert(not s.showing and values.VR_FarFarReticleActive=='false');assert(s.images[1].wrapper.set_Visibility==4)
  menu=false;update(s,pc,.02);assert(s.showing and s.images[1].wrapper.set_Visibility==2)""")
 def test_waiting_native_layer_does_not_display_centered_gameplay_images(self):
  self.check("local s=built();update(s,pc,.02);assert(not s.ready and s.images[1].wrapper.set_Visibility==2);values.VR_FarFarReticleReady='123';update(s,pc,.02);assert(s.ready)")
 def test_cinema_and_runtime_idle_suspend_only_reticle(self):
  self.check("""local s=built();update(s,pc,.02);values.VR_2DScreenMode='true';update(s,pc,.02);assert(not s.showing)
  values.VR_2DScreenMode='false';update(s,pc,.02);assert(s.showing);hmd=false;update(s,pc,.02);assert(not s.showing and health:GetParent()==root)""")
 def test_cleanup_restores_native_sources_and_only_destroys_private_renderer(self):
  self.check("local s=built();update(s,pc,.02);cleanup();assert(hud.HitImage:GetParent()==root and component.destroyed and host.removed and values.VR_FarFarReticleSource=='0' and health:GetParent()==root)")
 def test_hud_replacement_and_private_renderer_lifetime_are_validated(self):
  self.check("local s=built();assert(valid_state(s));component.valid=false;assert(not valid_state(s));component.valid=true;hud.WidgetTree.RootWidget={};assert(not valid_state(s))")
 def test_unavailable_backend_keeps_native_widget_ownership(self):
  self.check("values.VR_FarFarReticleReady='';tick(nil,2);assert(hud.Crosshair_Top:GetParent()==root);for _,w in ipairs(writes)do assert(w[2]=='0' or w[2]=='false' or (w[1]=='VR_FarFarReticleTarget' and w[2]==''))end")
 def test_artwork_redraw_is_bounded(self):
  self.check("local s=built();for i=1,1000 do update(s,pc,.001)end;assert(component.redraws<65 and component.redraws>50)")
 def test_stable_layout_skips_writes_but_animated_hit_offsets_still_update(self):
  self.check('''
  local s=built();local writes=0
  for _,item in ipairs(s.images) do
   local pos,size=item.slot.SetPosition,item.slot.SetSize
   item.slot.SetPosition=function(slot,v) writes=writes+1;pos(slot,v) end
   item.slot.SetSize=function(slot,v) writes=writes+1;size(slot,v) end
  end
  for i=1,120 do update(s,pc,1/60) end
  assert(writes==0)
  local offsets=hud.HitImage.Slot.LayoutData.Offsets
  offsets.Left=-25;offsets.Bottom=80;update(s,pc,.02)
  assert(writes==2 and s.images[2].slot.Position.X==-25 and s.images[2].slot.Size.Y==80)
  hud.Crosshair_Top.Slot.LayoutData.Offsets.Left=5;update(s,pc,.02)
  assert(s.images[2].slot.Position.X==-30)
  ''')
 def test_hidden_reticle_does_not_continuously_redraw_and_restores_live_hit_x(self):
  self.check('''
  local s=built();update(s,pc,.02);menu=true;update(s,pc,.02)
  local redraws=component.redraws
  for i=1,120 do update(s,pc,1/120) end
  assert(component.redraws==redraws)
  hud.HitImage.RenderOpacity=.8;hud.HitImage.RenderTransform.Angle=29
  menu=false;update(s,pc,.02)
  assert(s.showing and component.redraws==redraws+1)
  assert(s.images[2].clone.set_RenderOpacity==.8 and s.images[2].clone.set_RenderTransform.Angle==29)
  assert(s.images[2].clone:GetParent()==s.reticle and s.reticle~=root)
  ''')
 def test_title_camera_adjustment_is_preserved_and_restored(self):
  self.check("""local camera={RelativeLocation={X=1,Y=2,Z=90},K2_SetRelativeLocation=function(self,v)self.RelativeLocation=v end}
  local title={CineCamera=camera,get_full_name=function()return 'BP_TitleScreen_C Test'end}
  title_height(title);assert(camera.RelativeLocation.Z==150);title_height(title);assert(camera.RelativeLocation.Z==150)
  title_height(pawn);assert(camera.RelativeLocation.Z==90)""")
 def test_drawn_gun_uses_the_real_firing_camera_in_each_view(self):
  self.check("""local s=built();update(s,pc,.02)
  pawn.AC_PlayerItems={equipedItem={valid=true,canBeReloaded=true,isDrawn=true,get_class=function()return {find_property=function()return {}end}end}}
  pc.PlayerCameraManager=node('firing camera');pc.PlayerCameraManager.location={X=20,Y=10,Z=30}
  s.hand.location={X=900,Y=800,Z=700};view_tick()
  local x,y,z=values.VR_FarFarReticleTarget:match('([^ ]+) ([^ ]+) ([^ ]+)')
  assert(math.abs(tonumber(x)-.1)<1e-6 and math.abs(tonumber(y)-.3)<1e-6 and math.abs(tonumber(z)+2.95)<1e-6)
  assert(s.aim_source=='game firing camera' and s.aim_updates==1)
  pc.PlayerCameraManager.forward={X=0,Y=1,Z=0};view_tick()
  x,y,z=values.VR_FarFarReticleTarget:match('([^ ]+) ([^ ]+) ([^ ]+)')
  assert(math.abs(tonumber(x)-2.85)<1e-6 and math.abs(tonumber(z)+.2)<1e-6 and s.aim_updates==2)""")
 def test_empty_hand_aim_tracks_hand_without_requiring_a_weapon(self):
  self.check("""local s=built();update(s,pc,.02);s.hand.location={X=10,Y=50,Z=20};view_tick()
  local x,y,z=values.VR_FarFarReticleTarget:match('([^ ]+) ([^ ]+) ([^ ]+)')
  assert(math.abs(tonumber(x)-.5)<1e-6 and math.abs(tonumber(y)-.2)<1e-6 and math.abs(tonumber(z)+2.85)<1e-6)
  assert(s.aim_source=='tracked right hand' and s.hand.hand==1 and s.head.hand==2)
  s.hand.forward={X=0,Y=1,Z=0};view_tick();x,y,z=values.VR_FarFarReticleTarget:match('([^ ]+) ([^ ]+) ([^ ]+)')
  assert(math.abs(tonumber(x)-3.25)<1e-6 and math.abs(tonumber(z)+.1)<1e-6)""")
 def test_stationary_hand_target_compensates_for_head_translation_rotation_and_scale(self):
  self.check("""local s=built();update(s,pc,.02);s.hand.location={X=0,Y=0,Z=0};view_tick()
  s.head.location={X=0,Y=50,Z=10};s.head.rotation.Yaw=90;values.VR_WorldScale='2';view_tick()
  local x,y,z=values.VR_FarFarReticleTarget:match('([^ ]+) ([^ ]+) ([^ ]+)')
  assert(math.abs(tonumber(x)+1.375)<1e-6 and math.abs(tonumber(y)+.05)<1e-6 and math.abs(tonumber(z)-.25)<1e-6)
  menu=true;update(s,pc,.02);assert(values.VR_FarFarReticleTarget=='');local count=s.aim_updates;view_tick();assert(s.aim_updates==count)""")
 def test_reset_releases_tracking_anchors_and_invalidates_target(self):
  self.check("local s=built();update(s,pc,.02);view_tick();cleanup();assert(s.head.untracked and s.head.destroyed and s.hand.untracked and s.hand.destroyed and values.VR_FarFarReticleTarget=='')")
 def test_weapons_without_reload_such_as_minigun_and_lasso_use_firing_camera(self):
  self.check("""local s=built();update(s,pc,.02)
  pawn.AC_PlayerItems={equipedItem={valid=true,canBeReloaded=false,isDrawn=true,get_class=function()return {find_property=function(_,k)if k=='isDrawn' then return {} end end}end}}
  pc.PlayerCameraManager=node('firing camera');pc.PlayerCameraManager.location={X=20,Y=10,Z=30}
  s.hand.location={X=900,Y=800,Z=700};view_tick()
  local x,y,z=values.VR_FarFarReticleTarget:match('([^ ]+) ([^ ]+) ([^ ]+)')
  assert(math.abs(tonumber(x)-.1)<1e-6 and math.abs(tonumber(y)-.3)<1e-6 and math.abs(tonumber(z)+2.95)<1e-6)
  assert(s.aim_source=='game firing camera' and s.aim_updates==1)""")
 def test_empty_hand_item_uses_tracked_right_hand(self):
  self.check("""local s=built();update(s,pc,.02)
  local empty_item={valid=true,isDrawn=true,get_full_name=function()return 'BP_Item_Empty_C /Game/Maps/Test.PersistentLevel.BP_Item_Empty_C_1' end,get_class=function()return {find_property=function()return {}end}end}
  pawn.AC_PlayerItems={equipedItem=empty_item}
  pc.PlayerCameraManager=node('firing camera');pc.PlayerCameraManager.location={X=20,Y=10,Z=30}
  s.hand.location={X=10,Y=50,Z=20};view_tick()
  local x,y,z=values.VR_FarFarReticleTarget:match('([^ ]+) ([^ ]+) ([^ ]+)')
  assert(math.abs(tonumber(x)-.5)<1e-6 and math.abs(tonumber(y)-.2)<1e-6 and math.abs(tonumber(z)+2.85)<1e-6)
  assert(s.aim_source=='tracked right hand')""")
 def test_holstered_weapon_uses_tracked_right_hand(self):
  self.check("""local s=built();update(s,pc,.02)
  pawn.AC_PlayerItems={equipedItem={valid=true,canBeReloaded=false,isDrawn=false,get_class=function()return {find_property=function(_,k)if k=='isDrawn' then return {} end end}end}}
  pc.PlayerCameraManager=node('firing camera');pc.PlayerCameraManager.location={X=20,Y=10,Z=30}
  s.hand.location={X=10,Y=50,Z=20};view_tick()
  local x,y,z=values.VR_FarFarReticleTarget:match('([^ ]+) ([^ ]+) ([^ ]+)')
  assert(math.abs(tonumber(x)-.5)<1e-6 and math.abs(tonumber(y)-.2)<1e-6 and math.abs(tonumber(z)+2.85)<1e-6)
  assert(s.aim_source=='tracked right hand')""")
if __name__=='__main__':unittest.main()
