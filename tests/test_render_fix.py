"""Execute graphics ownership, float precision, context restoration and retries."""
from pathlib import Path
import unittest
from lupa.lua54 import LuaRuntime
ROOT=Path(__file__).resolve().parents[1]
class RenderFixTests(unittest.TestCase):
 def setUp(self):
  self.lua=LuaRuntime()
  self.lua.execute('package.path='+repr(str(ROOT/'Profile/scripts/?.lua').replace('\\','/'))+" .. ';' .. package.path")
  self.lua.execute('''
  active=true;ready=true;manager_ready=true;values={VR_NativeStereoFix='true',VR_2DScreenMode='false'}
  cvars={['r.RefractionQuality']=2,['r.MotionBlur.Amount']=.5,['r.TemporalAA.Upsampling']=1}
  local manager={find_variable=function(_,name)
   if cvars[name]~=nil then return {get_int=function()return math.floor(cvars[name])end,get_float=function()return cvars[name]end,
   set_int=function(_,v)cvars[name]=v end,set_float=function(_,v)cvars[name]=v end}end end}
  uevr={api={get_console_manager=function()return manager_ready and manager or nil end},params={vr={
   is_runtime_ready=function()return ready end,is_hmd_active=function()return active end,get_mod_value=function(_,k)return values[k]end}},
   sdk={callbacks={on_post_engine_tick=function(f)post=f end,on_script_reset=function(f)reset=f end}}}
  ''')
  self.lua.execute((ROOT/'Profile/scripts/farfarvr_render_fix.lua').read_text())
 def test_float_zero_and_taa_single_owner(self):
  self.lua.execute("post(nil,.01);assert(cvars['r.MotionBlur.Amount']==0 and cvars['r.RefractionQuality']==0 and cvars['r.TemporalAA.Upsampling']==1)")
 def test_cinema_and_idle_restore_original_values(self):
  self.lua.execute("post(nil,.1);values.VR_2DScreenMode='true';post(nil,.1);assert(cvars['r.MotionBlur.Amount']==.5 and cvars['r.RefractionQuality']==2);values.VR_2DScreenMode='false';post(nil,.1);active=false;post(nil,.1);assert(cvars['r.RefractionQuality']==2)")
 def test_missing_manager_recovers(self):
  self.lua.execute("manager_ready=false;post(nil,4);assert(cvars['r.RefractionQuality']==2);manager_ready=true;post(nil,4);assert(cvars['r.RefractionQuality']==0)")
 def test_external_edit_is_not_overwritten_on_reset(self):
  self.lua.execute("post(nil,.1);cvars['r.RefractionQuality']=3;reset();assert(cvars['r.RefractionQuality']==3 and cvars['r.MotionBlur.Amount']==.5)")
if __name__=='__main__':unittest.main()
