"""Verify independent gun target projection and query budget; not wearer acceptance."""
from pathlib import Path
import sys
import unittest
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/lua-test'))
from lupa.lua54 import LuaRuntime
HARNESS=r'''
fs={write=function() end}
uevr={api={find_uobject=function(self,p) return {path=p} end,get_local_pawn=function() return nil end},params={vr={
 is_runtime_ready=function() return true end,is_hmd_active=function() return true end,is_using_controllers=function() return true end,
 get_mod_value=function(self,key) return ({UI_Distance='2.75',UI_Size='2',UI_X_Offset='0',UI_Y_Offset='0',UI_FollowView='true',UI_OverlayType='0'})[key] end},
 functions={is_drawing_ui=function() return false end,log_info=function() end}},
 sdk={callbacks={on_post_engine_tick=function(fn) tick=fn end,on_script_reset=function(fn) reset=fn end,
 on_pre_calculate_stereo_view_offset=function(fn) view_tick=fn end}}}
UEVR_UObjectHook={exists=function(o) return o.valid~=false end}
StructObject={new=function(...) assert(select('#',...)==1); return {native=true} end}
function capture(fn,key)
 for i=1,100 do local k,v=debug.getupvalue(fn,i); if k==key then return v end; if not k then break end end
 error('Missing '..key)
end
function setup()
 local pawn={}
 local source={Brush={ResourceObject={id='native_material'}},ColorAndOpacity={R=1,G=1,B=1,A=1},RenderOpacity=.8,
 RenderTransform={Translation={X=0,Y=0},Scale={X=1,Y=1},Shear={X=0,Y=0},Angle=0},GetVisibility=function() return 0 end,
 Slot={LayoutData={Offsets={Left=0,Top=0,Right=64,Bottom=64}}}}
 local clone={SetBrush=function(self,b) self.brush=b end,SetColorAndOpacity=function(self,c) self.color=c end,
 SetRenderOpacity=function(self,o) self.opacity=o end,SetRenderTransform=function(self,t) self.transform=t end,
 SetVisibility=function(self,v) self.visibility=v end}
 local wrapper={SetVisibility=function(self,v) self.visibility=v end}
 local slot={SetSize=function(self,v) assert(v.native); self.size=v end,SetPosition=function(self,v) assert(v.native); self.position=v end}
 local marker={RequestRedraw=function() end,SetVisibility=function(self,b) self.visible=b==4; self.visibility=b end,
 K2_SetWorldLocationAndRotation=function(self,p,r) assert(p.native and r.native); self.position=p; self.rotation=r end,
 SetWorldScale3D=function(self,p) assert(p.native); self.scale=p end}
 local weapon={OwningPlayer=pawn,isDrawn=true,calls=0,F_TraceBullet=function(self,a,b,h,l)
 assert(a==0 and b==true); self.calls=self.calls+1; h.TraceStart={X=10,Y=0,Z=0}; h.ImpactPoint={X=1000,Y=100,Z=0}; h.TraceEnd={X=10000,Y=0,Z=0}; return trace_hit~=false end}
 local aim_slot={SetPosition=function(self,p) self.position=p end}
 local head={K2_GetComponentLocation=function() return {X=10,Y=0,Z=0} end,K2_GetComponentRotation=function() return {Pitch=0,Yaw=0,Roll=0} end}
 local layout={GetViewportSize=function() return {X=1920,Y=1080} end,GetViewportScale=function() return 1 end}
 local camera={direction={X=1,Y=0,Z=0},K2_GetActorLocation=function() return {X=10,Y=0,Z=0} end,
 GetActorForwardVector=function(self) return self.direction end}
 return {pawn=pawn,reticle=marker,reticle_slot=aim_slot,head=head,camera=camera,layout=layout,settings_time=0,supported=true,distance=2.75,height=2,xoffset=0,yoffset=0,logical_height=1080,
 images={{source=source,clone=clone,wrapper=wrapper,slot=slot,layout={Offsets={Left=0,Top=0}}}},weapon=weapon,elapsed=0,animation_time=0,weapon_scan=0,
 trace_count=0,hit={},look={}}
end
'''
class HudTests(unittest.TestCase):
 def setUp(self):
  self.lua=LuaRuntime(); self.lua.execute(HARNESS)
  self.lua.execute((ROOT/'Profile/scripts/farfarvr_hud.lua').read_text())
  self.lua.execute("update=capture(tick,'update'); render_aim=capture(view_tick,'render_aim'); marker_target=capture(render_aim,'marker_target'); title_height=capture(tick,'title_height'); function step(s,pc,dt) update(s,pc,dt); render_aim(s) end")
 def check(self,c): self.lua.execute(c)
 def test_constant_distance_for_near_and_far_targets(self):
  self.check('''
  local start={X=10,Y=20,Z=30}
  local a,ra=marker_target({start=start,point={X=1010,Y=120,Z=130}})
  local b,rb=marker_target({start=start,point={X=100010,Y=10020,Z=10030}})
  assert(math.abs(a.X-b.X)<.000001 and math.abs(a.Y-b.Y)<.000001 and math.abs(a.Z-b.Z)<.000001)
  assert(math.abs(math.sqrt((a.X-10)^2+(a.Y-20)^2+(a.Z-30)^2)-275)<.000001)
  assert(math.abs((a.Y-20)/(a.X-10)-.1)<.000001)
  assert(ra.native and math.abs(ra.Yaw-rb.Yaw)<.000001)
  assert(marker_target({start=start,point=start})==nil)
  ''')
 def test_hit_depth_does_not_move_crosshair(self):
  self.check('''
  local s=setup(); step(s,{bShowMouseCursor=false},.05)
  assert(s.reticle_slot.position.X==0 and s.reticle_slot.position.Y==0)
  trace_hit=false; s.hit.ImpactPoint={X=20,Y=500,Z=0}; step(s,{bShowMouseCursor=false},.05)
  assert(s.reticle_slot.position.X==0 and s.reticle_slot.position.Y==0)
  ''')
 def test_each_view_updates_aim_without_physics_queries(self):
  self.check('''
  local s=setup(); local pc={bShowMouseCursor=false}
  for i=1,1000 do step(s,pc,.001) end
  assert(s.weapon.calls==0 and s.trace_count==1000)
  s.camera.direction={X=1,Y=.1,Z=0}; render_aim(s)
  assert(s.reticle_slot.position.X>100 and s.trace_count==1001)
  ''')
 def test_world_marker_and_menu_restore(self):
  self.check('''
  local s=setup(); local pc={bShowMouseCursor=false}; step(s,pc,.05)
  assert(s.adjusted and s.reticle.visible and s.images[1].wrapper.visibility==2 and s.images[1].source.RenderOpacity==.8 and s.reticle_slot.position.X==0)
  pc.bShowMouseCursor=true; step(s,pc,.05)
  assert(not s.adjusted and not s.reticle.visible and s.images[1].wrapper.visibility==4 and s.images[1].source.RenderOpacity==.8 and s.weapon.calls==0)
  ''')
 def test_controller_inactivity_and_hmd_activity_do_not_hide_marker(self):
  self.check('''
  uevr.params.vr.is_using_controllers=function() return false end
  uevr.params.vr.is_hmd_active=function() return false end
  local s=setup(); step(s,{bShowMouseCursor=false},.05)
  assert(s.reticle.visible and s.weapon.calls==0)
  ''')
 def test_cinema_screen_hides_separate_aim_surface_and_resumes_afterward(self):
  self.check('''
  local s=setup(); local pc={bShowMouseCursor=false}; step(s,pc,.05); assert(s.reticle.visible)
  local original=uevr.params.vr.get_mod_value
  uevr.params.vr.get_mod_value=function(self,key) if key=='VR_2DScreenMode' then return 'true' end; return original(self,key) end
  step(s,pc,.05); assert(not s.reticle.visible and s.hide_reason=='cinema screen')
  uevr.params.vr.get_mod_value=original
  step(s,pc,.05); assert(s.reticle.visible)
  ''')
 def test_empty_hands_keep_original_dot_on_the_near_aim_surface(self):
  self.check('''
  local s=setup(); s.weapon=nil
  s.weapon_class={get_objects_matching=function() return {} end}
  s.images[1].source.Brush.ResourceObject={id='original_empty_hand_dot'}
  step(s,{bShowMouseCursor=false},1.1)
  assert(not s.reticle.visible and not s.ready and s.hide_reason=='no drawn firearm')
  ''')
 def test_live_original_material_and_animation_state_are_mirrored(self):
  self.check('''
  local s=setup(); local src=s.images[1].source; local clone=s.images[1].clone
  update(s,{bShowMouseCursor=false},.05)
  assert(clone.brush.ResourceObject==src.Brush.ResourceObject and clone.opacity==.8)
  src.RenderOpacity=.35; src.RenderTransform={Translation={X=4,Y=2},Scale={X=1.5,Y=1.5},Shear={X=0,Y=0},Angle=15}
  src.ColorAndOpacity={R=1,G=.2,B=.1,A=.5}; src.Slot.LayoutData.Offsets.Left=8
  update(s,{bShowMouseCursor=false},.02)
  assert(clone.opacity==.35 and clone.transform.Angle==15 and clone.transform.Scale.X==1.5 and clone.transform.Translation.X==4 and clone.color==src.ColorAndOpacity)
  assert(s.images[1].slot.position.X==8 and src.RenderOpacity==.35)
  ''')
 def test_head_and_hud_settings_change_projected_aim(self):
  self.check('''
  local s=setup(); update(s,{bShowMouseCursor=false},.01)
  s.camera.direction={X=1,Y=.1,Z=.2}; render_aim(s)
  local x,y=s.reticle_slot.position.X,s.reticle_slot.position.Y
  assert(x~=0 and y~=0)
  s.distance=8; s.height=.5; render_aim(s)
  assert(s.reticle_slot.position.X~=x and s.reticle_slot.position.Y~=y)
  ''')
 def test_title_height_only_and_restore(self):
  self.check('''
  local camera={RelativeLocation={X=1,Y=2,Z=90},K2_SetRelativeLocation=function(self,p) assert(p.native); self.RelativeLocation=p end}
  local title={CineCamera=camera,get_full_name=function() return 'BP_TitleScreen_C Test' end}
  title_height(title); assert(camera.RelativeLocation.Z==150)
  title_height(title); assert(camera.RelativeLocation.Z==150)
  title_height({get_full_name=function() return 'BP_Player_C Test' end}); assert(camera.RelativeLocation.Z==90)
  ''')
if __name__=='__main__': unittest.main()
