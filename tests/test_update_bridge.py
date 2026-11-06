"""Real Lua menu to launcher filesystem bridge, with no network or game."""
from pathlib import Path
import subprocess,tempfile,unittest,json
from lupa.lua54 import LuaRuntime
from test_menu import HARNESS
ROOT=Path(__file__).resolve().parents[1]
class UpdateBridgeTests(unittest.TestCase):
 def test_real_lua_request_and_launcher_response_share_data_folder(self):
  with tempfile.TemporaryDirectory(prefix='ffvr-bridge-') as folder:
   profile=Path(folder);data=profile/'data';data.mkdir()
   (profile/'config.txt').write_text('installed profile fixture')
   lua=LuaRuntime();lua.execute('package.path='+repr(str(ROOT/'Profile/scripts/?.lua').replace('\\','/'))+" .. ';' .. package.path")
   lua.execute(HARNESS)
   for field in json.loads((ROOT/'Profile/VRSettings.json').read_text()):lua.globals()["values"][field['key']]=str(field['default']).lower()
   lua.globals().disk_write=lambda name,value:(data/name).write_text(value)
   lua.globals().disk_read=lambda name:(data/name).read_text() if (data/name).exists() else ''
   lua.execute("fs.write=function(name,value)disk_write(name,value)end;fs.read=function(name)return disk_read(name)end")
   lua.execute((ROOT/'Profile/scripts/farfarvr_menu.lua').read_text())
   lua.execute("events.Updates=true;draw();events['Check for Updates']=true;draw()")
   result=subprocess.run(['dotnet','run','--no-build','--project',str(ROOT/'tests/UpdaterTests/UpdaterTests.csproj'),'--','--bridge-profile',str(profile)],capture_output=True,text=True,timeout=30)
   self.assertEqual(result.returncode,0,result.stderr)
   lua.execute("draw();assert(table.concat(texts,' '):find('Update Available: v0.4.8',1,true));assert(table.concat(texts,' '):find('Bridge integration verified.',1,true))")
if __name__=='__main__':unittest.main()
