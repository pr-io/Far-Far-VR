"""Execute mounted camera constraints/restoration; does not prove terrain collision."""
from pathlib import Path
import sys,unittest
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/lua-test'))
from lupa.lua54 import LuaRuntime

class MountTests(unittest.TestCase):
 def setUp(self):
  self.lua=LuaRuntime()
  self.lua.execute("package.path = " + repr(str(ROOT / "Profile/scripts/?.lua").replace("\\", "/")) + " .. ';' .. package.path")
  self.lua.execute('''
  fs={write=function() end}; cinema='false'
  function component(p,y,r)
    return {RelativeRotation={Pitch=p,Yaw=y,Roll=r},bUsePawnControlRotation=true,
      bInheritPitch=true,bInheritRoll=true,bDoCollisionTest=false,TargetArmLength=140,FieldOfView=95,
      K2_SetRelativeRotation=function(self,value) self.RelativeRotation=value end}
  end
  arm=component(-25,10,4); camera=component(8,2,3)
  mount={SpringArm=arm,Camera=camera,get_full_name=function() return 'BP_Mount_C /Game/Test' end}
  player={get_full_name=function() return 'BP_Player_C /Game/Test' end}
  pc={Pawn=mount,GetViewTarget=function(self) return self.target or self.Pawn end}
  UEVR_UObjectHook={exists=function(o) return o.valid~=false end}
  StructObject={new=function() return {} end}
  uevr={api={find_uobject=function() return {} end,get_player_controller=function() return pc end},
    params={vr={get_mod_value=function() return cinema end}},sdk={callbacks={
    on_pre_engine_tick=function(f) tick=f end,on_post_engine_tick=function(f) post=f end,on_script_reset=function(f) reset=f end}}}
  ''')
  self.lua.execute((ROOT/'Profile/scripts/farfarvr_mount.lua').read_text())
 def test_native_stick_orbit_and_collision_without_changing_zoom_or_fov(self):
  self.lua.execute('''
  tick(nil,.1)
  assert(arm.RelativeRotation.Pitch==-25 and arm.RelativeRotation.Yaw==10)
  assert(camera.RelativeRotation.Pitch==0 and camera.RelativeRotation.Yaw==2)
  assert(arm.bUsePawnControlRotation and arm.bInheritPitch and not arm.bInheritRoll)
  assert(arm.bDoCollisionTest and not camera.bUsePawnControlRotation)
  arm.TargetArmLength=220; camera.FieldOfView=105
  arm.RelativeRotation.Pitch=80; camera.RelativeRotation.Pitch=-80; post()
  assert(arm.RelativeRotation.Pitch==80 and camera.RelativeRotation.Pitch==0)
  assert(arm.TargetArmLength==220 and camera.FieldOfView==105)
  ''')
 def test_restore_on_dismount_reset_and_camera_blend(self):
  self.lua.execute('''
  tick(nil,.1); pc.Pawn=player; pc.target=mount; post(); assert(arm.bInheritPitch)
  pc.target=nil; post()
  assert(arm.RelativeRotation.Pitch==-25 and arm.RelativeRotation.Roll==4)
  assert(camera.RelativeRotation.Pitch==8 and camera.RelativeRotation.Roll==3)
  assert(arm.bUsePawnControlRotation and arm.bInheritPitch and arm.bInheritRoll and not arm.bDoCollisionTest)
  assert(camera.bUsePawnControlRotation)
  pc.Pawn=mount; post(); reset(); assert(arm.RelativeRotation.Pitch==-25)
  ''')
 def test_cutscene_restores_authored_mount_camera(self):
  self.lua.execute('''
  post(); cinema='true'; post()
  assert(arm.RelativeRotation.Pitch==-25 and camera.RelativeRotation.Pitch==8)
  cinema='false'; post(); assert(arm.bInheritPitch and camera.RelativeRotation.Pitch==0)
  ''')

if __name__=='__main__': unittest.main()
