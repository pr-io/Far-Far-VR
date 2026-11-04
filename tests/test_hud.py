"""Independent firing-ray artwork, HUD clipping boundary and recovery contracts."""
from pathlib import Path
import unittest
from lupa.lua54 import LuaRuntime
ROOT=Path(__file__).resolve().parents[1]
HARNESS=r'''
files={};values={VR_FarFarReticleReady='123',VR_2DScreenMode='false',VR_WorldScale='1',VR_AimMethod='2',UI_Distance='2.75',UI_Size='2',UI_FollowView='true',UI_OverlayType='0'};writes={};runtime=true;hmd=true;menu=false
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
 for _,k in ipairs({'Visibility','Brush','ColorAndOpacity','RenderOpacity','RenderTransform','Widget','WidgetSpace','DrawSize','CollisionEnabled','RenderInMainPass','RenderInDepthPass','CastShadow','RenderCustomDepth','VisibleInRayTracing','ReceivesDecals','WindowFocusable','ComponentTickEnabled','HiddenInGame'})do n['Set'..k]=function(self,v)if k=='DrawSize' then assert(v.struct_path=='ScriptStruct /Script/CoreUObject.Vector2D','SetDrawSize requires Vector2D, not IntPoint')end;self['set_'..k]=v end end
 function n:GetVisibility()return self.visibility or 0 end
 function n:StopAllAnimations()self.stopped=true end
 function n:RequestRedraw()self.redraws=(self.redraws or 0)+1 end
 function n:InitWidget()self.initialized=true end
 function n:UpdateRenderTarget()self.render_target_updated=true end
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
  if path:find('WidgetLayoutLibrary',1,true)then return {get_class_default_object=function()return {GetViewportSize=function()return {X=1920,Y=1080}end,GetViewportScale=function()return 1 end}end}end
  if path:find('WidgetBlueprintLibrary',1,true)then return {get_class_default_object=function()return library end}end
  if path:find('UI_Player.UI_Player_C',1,true)then return {get_objects_matching=function()return {hud}end}end
  return {path=path}
 end,spawn_object=function(_,cls,outer)local o=node(cls.path);o.outer=outer;return o end}
StructObject={new=function(...)assert(select('#',...)==1);return {native=true,struct_path=(...).path}end}
UEVR_UObjectHook={exists=function(o)return o and o.valid~=false end,activate=function()end,
 remove_motion_controller_state=function(o)o.untracked=true end,
 get_or_add_motion_controller_state=function(o)return {set_hand=function(_,v)o.hand=v end,set_permanent=function()end}end}
uevr={api=api,params={vr={is_runtime_ready=function()return runtime end,is_hmd_active=function()return hmd end,
 get_mod_value=function(_,k)return values[k]end,
 set_mod_value=function(k,v)values[k]=v;table.insert(writes,{k,v})end},functions={log_info=function()end,log_error=function()end,is_drawing_ui=function()return menu end}},
 sdk={callbacks={on_post_engine_tick=function(f)tick=f end,on_pre_calculate_stereo_view_offset=function(f)view_tick=f end,on_script_reset=function(f)reset=f end}}}
function capture(fn,key)for i=1,80 do local k,v=debug.getupvalue(fn,i);if k==key then return v end;if not k then break end end;error('Missing '..key)end
'''

class HudTests(unittest.TestCase):
 def setUp(self):
  self.lua=LuaRuntime()
  self.lua.execute("package.path = "+repr(str(ROOT/'Profile/scripts/?.lua').replace('\\','/'))+" .. ';' .. package.path")
  self.lua.execute(HARNESS)
  self.lua.execute((ROOT/'Profile/scripts/farfarvr_hud.lua').read_text())
  self.lua.execute("build=capture(tick,'build');update=capture(tick,'update');cleanup=capture(tick,'cleanup');valid_state=capture(tick,'valid_state');render_aim=capture(view_tick,'render_aim');function built()build(pawn,pc,hud);return capture(tick,'state')end")
 def check(self,s): self.lua.execute(s)
 def test_artwork_is_owned_by_private_texture_outside_information_hud(self):
  self.check("local s=built();assert(s.component and s.host and #s.images==3);assert(s.canvas:GetParent()==nil and s.host.WidgetTree.RootWidget==s.canvas and s.component.set_Widget==s.host);assert(s.component.set_DrawSize.X==256 and s.component.set_DrawSize.Y==256);for _,i in ipairs(s.images)do assert(i.clone.outer==s.host.WidgetTree and i.clone.outer~=hud.WidgetTree and i.source:GetParent()==i.wrapper)end")
 def test_live_material_and_hit_transform_are_preserved(self):
  self.check("local s=built();hud.HitImage.RenderOpacity=.7;hud.HitImage.RenderTransform.Angle=15;update(s,pc,.02);assert(s.images[2].clone.set_RenderOpacity==.7 and s.images[2].clone.set_RenderTransform.Angle==15);assert(s.images[1].clone.set_Brush==hud.Crosshair_Top.Brush)")
 def test_empty_hands_follow_selected_hand(self):
  self.check("local s=built();update(s,pc,.02);view_tick();assert(s.aim_source=='tracked right hand' and s.hand.hand==1);values.VR_AimMethod='3';view_tick();assert(s.hand.hand==0)")
 def test_drawn_weapons_use_firing_camera(self):
  self.check("local s=built();local item=node('weapon');item.isDrawn=true;pawn.AC_PlayerItems={equipedItem=item};pc.PlayerCameraManager=node('camera');pc.PlayerCameraManager.forward={X=1,Y=.2,Z=.1};update(s,pc,.02);view_tick();local x,y,z=values.VR_FarFarReticleTarget:match('([^ ]+) ([^ ]+) ([^ ]+)');assert(s.aim_source=='game firing camera' and math.abs(tonumber(x)-.55)<.001 and math.abs(tonumber(y)-.275)<.001 and math.abs(tonumber(z)+2.75)<.001)")
 def test_holstered_weapons_use_hand(self):
  self.check("local s=built();local item=node('weapon');item.isDrawn=false;pawn.AC_PlayerItems={equipedItem=item};update(s,pc,.02);view_tick();assert(s.aim_source=='tracked right hand')")
 def test_hud_placement_does_not_change_firing_endpoint(self):
  self.check("local s=built();s.hand.forward={X=1,Y=.2,Z=.1};update(s,pc,.02);view_tick();local before=values.VR_FarFarReticleTarget;values.UI_FollowView='false';values.UI_Size='.01';values.UI_X_Offset='20';values.UI_OverlayType='1';view_tick();assert(values.VR_FarFarReticleTarget==before)")
 def test_aim_outside_information_hud_is_not_clipped(self):
  self.check("local s=built();s.hand.forward={X=1,Y=2,Z=0};update(s,pc,.02);view_tick();local x=tonumber(values.VR_FarFarReticleTarget:match('([^ ]+)'));assert(math.abs(x-5.5)<.001 and s.reticle.set_Visibility==4 and s.images[1].wrapper.set_Visibility==2)")
 def test_cinema_and_idle_restore_original_art(self):
  self.check("local s=built();update(s,pc,.02);view_tick();assert(s.reticle.set_Visibility==4);values.VR_2DScreenMode='true';update(s,pc,.02);assert(s.reticle.set_Visibility==2 and s.images[1].wrapper.set_Visibility==4);values.VR_2DScreenMode='false';hmd=false;update(s,pc,.02);assert(not s.aim_enabled)")
 def test_cleanup_restores_sources_and_releases_anchors(self):
  self.check("local s=built();cleanup();for _,i in ipairs(s.images)do assert(i.source:GetParent()==root and i.wrapper.removed)end;assert(s.head.destroyed and s.hand.destroyed and s.component.destroyed and s.host.removed and values.VR_FarFarReticleTarget=='' and values.VR_FarFarReticleSource=='0')")
 def test_private_lifetime_and_hud_replacement_are_validated(self):
  self.check("local s=built();assert(valid_state(s));s.head.valid=false;assert(not valid_state(s));s.head.valid=true;hud.WidgetTree.RootWidget={};assert(not valid_state(s))")
 def test_temporary_view_failure_rebuilds(self):
  self.check("local s=built();update(s,pc,.02);s.hand.GetForwardVector=function()error('temporary')end;view_tick();assert(capture(tick,'state')==nil);for i=1,120 do tick(nil,.02)end;assert(capture(tick,'state')~=nil)")
 def test_lobby_recenter_occurs_once_per_possession(self):
  self.check("local n=0;uevr.params.vr.recenter_view=function()n=n+1 end;function pawn:get_full_name()return 'BP_Player_Lobby_C Test'end;tick(nil,.02);for i=1,120 do tick(nil,.02)end;assert(n==1);pc.Pawn=nil;tick(nil,.02);pc.Pawn=pawn;tick(nil,.02);assert(n==2)")
 def test_hud_recovery_does_not_recenter_the_same_lobby_pawn(self):
  self.check("local n=0;uevr.params.vr.recenter_view=function()n=n+1 end;function pawn:get_full_name()return 'BP_Player_Lobby_C Test'end;tick(nil,.02);assert(n==1);capture(tick,'recover_hud')('temporary resize');for i=1,120 do tick(nil,.02)end;assert(n==1);pc.Pawn=nil;tick(nil,.02);pc.Pawn=pawn;tick(nil,.02);assert(n==2)")
if __name__=='__main__':unittest.main()
