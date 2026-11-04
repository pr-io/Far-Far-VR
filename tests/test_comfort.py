"""Execute context overrides and crash recovery; not headset comfort acceptance."""
from pathlib import Path
import sys
import unittest
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/lua-test'))
from lupa.lua54 import LuaRuntime

class ComfortTests(unittest.TestCase):
 def setup_lua(self,journal='',persisted_aim='2',persisted_move='1'):
  lua=LuaRuntime()
  lua.execute("package.path = " + repr(str(ROOT / "Profile/scripts/?.lua").replace("\\", "/")) + " .. ';' .. package.path")
  lua.globals().initial_journal=journal
  lua.globals().persisted_aim=persisted_aim
  lua.globals().persisted_move=persisted_move
  lua.execute('''
  values={VR_MovementOrientation=persisted_move,VR_AimMethod=persisted_aim,UI_FollowView='true',VR_DecoupledPitchUIAdjust='true',VR_DecoupledPitch='true',VR_2DScreenMode='false',VR_RoomscaleMovement='true',VR_NativeStereoFix='true'}
  aa_method,taa_scaling=2,1
  local aa={get_int=function() return aa_method end}
  local scaling={get_int=function() return taa_scaling end,set_int=function(_,v) taa_scaling=v end}
  console={find_variable=function(_,k) if k=='r.AntiAliasingMethod' then return aa elseif k=='r.TemporalAA.Upsampling' then return scaling end end}
  files={['farfarvr-comfort-restore.txt']=initial_journal}
  fs={read=function(p) return files[p] end,write=function(p,v) files[p]=v end}
  camera_class={}
  function klass(o) return {find_property=function(_,key) if o[key]~=nil then return {} end end} end
  function pawn(n)
   local o={get_full_name=function() return n..' /Game/Test' end,is_a=function(_,c) return c==camera_class and (n=='CameraActor' or n=='CineCameraActor') end}
   o.get_class=function() return klass(o) end
   return o
  end
  pc={Pawn=pawn('BP_Player_C'),bShowMouseCursor=false,GetViewTarget=function(self) return self.target or self.Pawn end}
  pc.get_class=function() return klass(pc) end
  hmd_active=true
  tracking_ready=true
  pose={x=.1,y=1.7,z=.2}
  origin={x=0,y=1.6,z=0}
  origin_writes=0
  UEVR_Vector3f={new=function() return {x=0,y=0,z=0} end}
  UEVR_Quaternionf={new=function() return {x=0,y=0,z=0,w=1} end}
  uevr={api={get_console_manager=function() return console end,get_player_controller=function() return pc end,find_uobject=function() return camera_class end},params={vr={
   is_hmd_active=function() return hmd_active end,
   is_runtime_ready=function() return tracking_ready end,
   get_hmd_index=function() return 0 end,
   get_pose=function(index,p,r) assert(index==0); p.x=pose.x; p.y=pose.y; p.z=pose.z end,
   get_standing_origin=function(o) o.x=origin.x; o.y=origin.y; o.z=origin.z end,
   set_standing_origin=function(o) origin={x=o.x,y=o.y,z=o.z}; origin_writes=origin_writes+1 end,
   get_mod_value=function(self,k) return values[k] end,
   set_mod_value=function(k,v) assert(type(k)=='string'); values[k]=v end}},
   sdk={callbacks={on_pre_engine_tick=function(f) tick=f end,on_post_engine_tick=function(f) post_tick=f end,on_script_reset=function(f) reset=f end}}}
  ''')
  lua.execute((ROOT/'Profile/scripts/farfarvr_comfort.lua').read_text())
  return lua
 def test_loading_train_uses_authored_camera_and_restores_across_countdown_and_cutscene(self):
  self.setup_lua().execute('''
  pc.Pawn=pawn('BP_LoadingScreen_C'); tick(nil,.1)
  assert(values.VR_AimMethod=='0' and values.VR_2DScreenMode=='true')
  assert(values.VR_DecoupledPitch=='false' and values.VR_RoomscaleMovement=='false')
  assert(values.VR_NativeStereoFix=='false' and values.UI_FollowView=='false')
  pc.bShowMouseCursor=true; post_tick(); assert(values.VR_2DScreenMode=='true')
  pc.target=pawn('CineCameraActor'); post_tick()
  assert(values.VR_2DScreenMode=='true' and values.VR_DecoupledPitch=='true')
  pc.target=nil; post_tick(); assert(values.VR_DecoupledPitch=='false')
  pc.Pawn=pawn('BP_Player_C'); pc.bShowMouseCursor=false; post_tick()
  assert(values.VR_AimMethod=='2' and values.VR_2DScreenMode=='false')
  assert(values.VR_DecoupledPitch=='true' and values.VR_NativeStereoFix=='true')
  ''')
 def test_taa_filter_preserved_with_scaling_restoration(self):
  self.setup_lua().execute('''
  tick(nil,.1); assert(aa_method==2 and taa_scaling==0)
  aa_method=1; tick(nil,.1); assert(taa_scaling==1 and aa_method==1)
  aa_method=4; tick(nil,.1); assert(taa_scaling==1 and aa_method==4)
  aa_method=2; tick(nil,.1); assert(taa_scaling==0)
  pc.Pawn.isInCinematic=true; tick(nil,.1); assert(taa_scaling==1)
  pc.Pawn.isInCinematic=false; tick(nil,.1); assert(taa_scaling==0)
  reset(); assert(taa_scaling==1 and aa_method==2)
  ''')
 def test_taa_missing_cvar_does_not_disable_comfort(self):
  self.setup_lua().execute('''
  console.find_variable=function() return nil end
  pc.Pawn=pawn('BP_LoadingScreen_C'); tick(nil,4)
  assert(values.VR_2DScreenMode=='true')
  assert(not files['farfarvr-comfort-status.txt'])
  pc.Pawn=pawn('BP_Player_C');tick(nil,.1);assert(values.VR_2DScreenMode=='false')
  ''')
 def test_idle_does_not_query_rendering_variables(self):
  self.setup_lua().execute('''
  hmd_active=false
  uevr.api.get_console_manager=function() error('Idle must not access console variables') end
  tick(nil,4)
  assert(values.VR_NativeStereoFix=='false')
  assert(not files['farfarvr-taa-status.txt'])
  ''')
 def test_rejected_taa_write_does_not_disable_camera_transitions(self):
  self.setup_lua().execute('''
  console.find_variable=function(_,key)
   if key=='r.AntiAliasingMethod' then return {get_int=function() return 2 end} end
   return {get_int=function() return 1 end,set_int=function() end}
  end
  tick(nil,4)
  assert(files['farfarvr-taa-status.txt']:find('ERROR',1,true))
  pc.Pawn=pawn('BP_LoadingScreen_C'); tick(nil,.1)
  assert(values.VR_2DScreenMode=='true' and values.VR_AimMethod=='0')
  pc.Pawn=pawn('BP_Player_C'); tick(nil,.1)
  assert(values.VR_2DScreenMode=='false' and values.VR_AimMethod=='2')
  ''')
 def test_title_pause_and_resume(self):
  self.setup_lua().execute('''
  pc.Pawn=pawn('BP_TitleScreen_C'); tick(nil,.1)
  assert(values.VR_AimMethod=='0' and values.UI_FollowView=='false' and values.VR_DecoupledPitchUIAdjust=='false')
  tick(nil,.1); pc.Pawn=pawn('BP_Player_C'); tick(nil,.1)
  assert(values.VR_AimMethod=='2' and values.UI_FollowView=='true' and values.VR_DecoupledPitchUIAdjust=='true')
  pc.bShowMouseCursor=true; tick(nil,.1); assert(values.VR_AimMethod=='0')
  pc.bShowMouseCursor=false; tick(nil,.1); assert(values.VR_AimMethod=='2')
  ''')
 def test_mount_sprint_blend_menu_and_dismount(self):
  self.setup_lua().execute('''
  pc.Pawn=pawn('BP_Mount_C'); tick(nil,.1)
  assert(values.VR_AimMethod=='0' and values.UI_FollowView=='true')
  for i=1,30 do tick(nil,.1) end
  pc.bShowMouseCursor=true; tick(nil,.1); assert(values.UI_FollowView=='false')
  pc.bShowMouseCursor=false; tick(nil,.1); assert(values.VR_AimMethod=='0' and values.UI_FollowView=='true')
  pc.target=pc.Pawn; pc.Pawn=pawn('BP_Player_C'); tick(nil,.1); assert(values.VR_AimMethod=='0')
  pc.target=nil; tick(nil,.1); assert(values.VR_AimMethod=='2')
  ''')
 def test_emote_orbit_and_horse_movement_restore_after_menu_and_dismount(self):
  self.setup_lua().execute("""
  tick(nil,.1)
  pc.Pawn.isPlayingEmote=true; tick(nil,.1)
  assert(values.VR_AimMethod=='0' and values.VR_MovementOrientation=='1')
  assert(values.UI_FollowView=='true' and values.VR_2DScreenMode=='false')
  pc.Pawn.isPlayingEmote=false; tick(nil,.1); assert(values.VR_AimMethod=='2')
  pc.Pawn=pawn('BP_Mount_C'); tick(nil,.1)
  assert(values.VR_MovementOrientation=='0' and values.VR_AimMethod=='0')
  pc.bShowMouseCursor=true; tick(nil,.1); assert(values.VR_MovementOrientation=='1')
  pc.bShowMouseCursor=false; tick(nil,.1); assert(values.VR_MovementOrientation=='0')
  pc.Pawn=pawn('BP_Player_C'); tick(nil,.1)
  assert(values.VR_MovementOrientation=='1' and values.VR_AimMethod=='2')
  pc.Pawn=pawn('BP_Mount_C'); tick(nil,.1); reset()
  assert(values.VR_MovementOrientation=='1' and values.VR_AimMethod=='2')
  """)
 def test_mounted_crash_recovers_head_movement(self):
  self.setup_lua('VR_MovementOrientation=1',persisted_move='0').execute("""
  assert(values.VR_MovementOrientation=='1')
  pc.Pawn=pawn('BP_Mount_C'); tick(nil,.1); assert(values.VR_MovementOrientation=='0')
  reset(); assert(values.VR_MovementOrientation=='1')
  """)
 def test_reset_and_camera_preference_preserved(self):
  self.setup_lua().execute('''
  values.VR_AimMethod='4'; pc.bShowMouseCursor=true; tick(nil,.1)
  assert(files['farfarvr-comfort-restore.txt']:find('VR_AimMethod=4',1,true))
  reset(); assert(values.VR_AimMethod=='4' and values.UI_FollowView=='true')
  assert(files['farfarvr-comfort-restore.txt']:find('VR_AimMethod=4',1,true))
  ''')
 def test_crash_journal_restores_prior_preferences(self):
  lua=self.setup_lua('VR_AimMethod=4\nUI_FollowView=true\nVR_DecoupledPitchUIAdjust=true','0')
  lua.execute("assert(values.VR_AimMethod=='4' and values.UI_FollowView=='true')")
 def test_normal_saved_aim_takes_precedence_over_old_journal(self):
  lua=self.setup_lua('VR_AimMethod=4','2')
  lua.execute("assert(values.VR_AimMethod=='2')")
 def test_player_cinematic_flag_enters_stationary_cinema_and_restores(self):
  self.setup_lua().execute('''
  pc.Pawn.isInCinematic=true; tick(nil,.1)
  assert(values.VR_AimMethod=='0' and values.VR_2DScreenMode=='true' and values.UI_FollowView=='false')
  assert(values.VR_RoomscaleMovement=='false')
  pc.Pawn.isInCinematic=false; tick(nil,.1)
  assert(values.VR_AimMethod=='2' and values.VR_2DScreenMode=='false' and values.UI_FollowView=='true')
  assert(values.VR_RoomscaleMovement=='true')
  ''')
 def test_sequencer_camera_cut_without_pawn_is_cinematic(self):
  self.setup_lua().execute('''
  pc.Pawn=nil; pc.target=pawn('CineCameraActor'); post_tick()
  assert(values.VR_2DScreenMode=='true' and values.VR_AimMethod=='0')
  pc.Pawn=pawn('BP_Player_Tutorial_C'); pc.target=nil; post_tick()
  assert(values.VR_2DScreenMode=='false' and values.VR_AimMethod=='2')
  ''')
 def test_camera_shake_during_gameplay_is_not_cinematic(self):
  self.setup_lua().execute('''
  pc.target=pawn('CameraShake'); post_tick()
  assert(values.VR_2DScreenMode=='false' and values.VR_AimMethod=='2')
  ''')
 def test_controller_cinematic_mode_and_pause_preserve_cinema_until_end(self):
  self.setup_lua().execute('''
  pc.bCinematicMode=true; post_tick(); pc.bShowMouseCursor=true; post_tick()
  assert(values.VR_AimMethod=='0' and values.VR_2DScreenMode=='true')
  pc.bCinematicMode=false; post_tick()
  assert(values.VR_2DScreenMode=='false' and values.VR_AimMethod=='0' and values.UI_FollowView=='false')
  pc.bShowMouseCursor=false; post_tick()
  assert(values.VR_AimMethod=='2' and values.UI_FollowView=='true')
  ''')
 def test_cinematic_reset_and_crash_recovery(self):
  self.setup_lua().execute('''
  pc.Pawn.isInCinematic=true; post_tick(); reset()
  assert(values.VR_2DScreenMode=='false' and values.VR_RoomscaleMovement=='true')
  assert(files['farfarvr-comfort-restore.txt']:find('VR_RoomscaleMovement=true',1,true))
  ''')
  lua=self.setup_lua()
  lua.execute("values.VR_2DScreenMode='true'; values.VR_RoomscaleMovement='false'; files['farfarvr-comfort-restore.txt']='VR_RoomscaleMovement=true'")
  lua.execute((ROOT/'Profile/scripts/farfarvr_comfort.lua').read_text())
  lua.execute("assert(values.VR_2DScreenMode=='false' and values.VR_RoomscaleMovement=='true')")
 def test_legacy_menu_journal_recovers_a_leaked_cinema_toggle(self):
  lua=self.setup_lua()
  lua.execute("files['farfarvr-comfort-restore.txt']='VR_AimMethod=2\\nUI_FollowView=true'; values.VR_2DScreenMode='true'")
  lua.execute((ROOT/'Profile/scripts/farfarvr_comfort.lua').read_text())
  lua.execute("assert(values.VR_2DScreenMode=='false')")
 def test_cinema_uses_original_renderer_and_restores_combat_correction(self):
  self.setup_lua().execute('''
  tick(nil,.1); assert(values.VR_NativeStereoFix=='true')
  pc.Pawn.isInCinematic=true; post_tick()
  assert(values.VR_2DScreenMode=='true' and values.VR_NativeStereoFix=='false')
  assert(files['farfarvr-comfort-restore.txt']:find('VR_NativeStereoFix=true',1,true))
  pc.Pawn.isInCinematic=false; post_tick()
  assert(values.VR_2DScreenMode=='false' and values.VR_NativeStereoFix=='true')
  pc.bShowMouseCursor=true; post_tick(); assert(values.VR_NativeStereoFix=='true')
  pc.bShowMouseCursor=false; post_tick(); reset(); assert(values.VR_NativeStereoFix=='true')
  ''')
 def test_crash_during_cinema_recovers_combat_correction(self):
  lua=self.setup_lua()
  lua.execute("values.VR_NativeStereoFix='false'; files['farfarvr-comfort-restore.txt']='VR_NativeStereoFix=true'")
  lua.execute((ROOT/'Profile/scripts/farfarvr_comfort.lua').read_text())
  lua.execute("assert(values.VR_NativeStereoFix=='true')")
 def test_idle_headset_suspends_capture_and_restores_preferences(self):
  self.setup_lua().execute('''
  tick(nil,.1); hmd_active=false; tick(nil,.1)
  assert(values.VR_NativeStereoFix=='false' and values.VR_2DScreenMode=='false')
  assert(files['farfarvr-comfort-restore.txt']:find('VR_NativeStereoFix=true',1,true))
  for i=1,10 do post_tick() end
  hmd_active=true; post_tick()
  assert(values.VR_NativeStereoFix=='true' and values.VR_AimMethod=='2' and values.UI_FollowView=='true')
  pc.Pawn.isInCinematic=true; post_tick(); hmd_active=false; post_tick()
  hmd_active=true; post_tick()
  assert(values.VR_NativeStereoFix=='false' and values.VR_2DScreenMode=='true')
  pc.Pawn.isInCinematic=false; post_tick()
  assert(values.VR_NativeStereoFix=='true' and values.VR_2DScreenMode=='false')
  hmd_active=false; post_tick(); reset(); assert(values.VR_NativeStereoFix=='true')
  ''')
 def test_taa_guard_recovers_after_temporary_write_failure(self):
  self.setup_lua().execute('''
  local find=console.find_variable
  local rejected=true
  console.find_variable=function(self,key)
   if key=='r.TemporalAA.Upsampling' and rejected then
    return {get_int=function() return 1 end,set_int=function() end}
   end
   return find(self,key)
  end
  tick(nil,.1); assert(files['farfarvr-taa-status.txt']:find('ERROR',1,true))
  rejected=false
  tick(nil,1.1); assert(taa_scaling==0 and aa_method==2)
  aa_method=1; tick(nil,.1); assert(taa_scaling==1 and aa_method==1)
  ''')
 def test_game_tick_cannot_leave_taa_scaling_reenabled_before_render(self):
  self.setup_lua().execute('''
  tick(nil,.1); assert(taa_scaling==0)
  taa_scaling=1 -- game settings/effect write during the engine tick
  post_tick(); assert(taa_scaling==0 and aa_method==2)
  aa_method=1; post_tick(); assert(taa_scaling==1 and aa_method==1)
  aa_method=4; post_tick(); assert(taa_scaling==1 and aa_method==4)
  ''')

 def test_steady_gameplay_reuses_console_variables_and_has_no_periodic_status_writes(self):
  self.setup_lua().execute('''
  local finds=0;local original=console.find_variable
  console.find_variable=function(self,key)finds=finds+1;return original(self,key)end
  tick(nil,.01)
  for i=1,1000 do tick(nil,.01);post_tick()end
  assert(finds==2 and taa_scaling==0)
  assert(not files['farfarvr-comfort-status.txt'] and not files['farfarvr-taa-status.txt'])
  assert(files['farfarvr-comfort-restore.txt']~=nil)
  ''')

 def test_menu_preferences_survive_title_and_mount_overrides(self):
  self.setup_lua().execute("""
  pc.Pawn=pawn('BP_TitleScreen_C');tick(nil,.1)
  assert(farfarvr_preferences.get('VR_AimMethod')=='2' and values.VR_AimMethod=='0')
  farfarvr_preferences.set('VR_AimMethod','3')
  farfarvr_preferences.set('UI_FollowView','true')
  assert(values.VR_AimMethod=='0')
  local queued=false
  uevr.params.vr.save_config=function() queued=true end
  farfarvr_preferences.save()
  assert(queued and values.VR_AimMethod=='0' and values.UI_FollowView=='false')
  local journal=files['farfarvr-comfort-restore.txt']
  assert(journal:find('VR_AimMethod=3',1,true) and journal:find('UI_FollowView=true',1,true))
  pc.Pawn=pawn('BP_Player_C');tick(nil,.1);assert(values.VR_AimMethod=='3')
  pc.Pawn=pawn('BP_Mount_C');tick(nil,.1)
  farfarvr_preferences.set('VR_MovementOrientation','3')
  pc.Pawn=pawn('BP_Player_C');tick(nil,.1);assert(values.VR_MovementOrientation=='3')
  """)
 def test_transient_context_error_recovers(self):
  self.setup_lua().execute("""
  local original=pc.GetViewTarget;pc.GetViewTarget=function()error('temporary')end
  tick(nil,.1);pc.GetViewTarget=original;pc.Pawn=pawn('BP_Mount_C')
  tick(nil,1.1);assert(values.VR_AimMethod=='0')
  pc.Pawn=pawn('BP_Player_C');tick(nil,.1);assert(values.VR_AimMethod=='2')
  """)

if __name__=='__main__': unittest.main()
