"""Run the real update helper in disposable installations; never starts the launcher."""
import ctypes,hashlib,json,os,subprocess,tempfile,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
SCRIPT=ROOT/'Launcher/Apply-Update.ps1'
class ApplyUpdateTests(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory(prefix='ffvr-apply-');self.root=Path(self.temp.name)
  self.install=self.root/'install';self.stage=self.root/'pending/package';self.install.mkdir();self.stage.mkdir(parents=True)
  self.env=dict(os.environ,APPDATA=str(self.root/'appdata'))
  self.names=['Far Far VR.exe','Apply-Update.ps1','Profile/config.txt','Runtime/backend.ffvrdelta']
  entries=[]
  for name in self.names:
   for base,text in [(self.install,b'old'),(self.stage,b'new')]:
    p=base/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(text+name.encode())
   entries.append({'File':name.replace('/','\\'),'SHA256':hashlib.sha256((self.stage/name).read_bytes()).hexdigest().upper()})
  (self.stage/'manifest.json').write_text(json.dumps({'version':'0.4.7','files':entries}))
  (self.stage.parent/'prepared.txt').write_text('v0.4.7')
 def tearDown(self):self.temp.cleanup()
 def run_helper(self,recover=False):
  args=['powershell.exe','-NoProfile','-ExecutionPolicy','Bypass','-File',str(SCRIPT),'-InstallRoot',str(self.install),'-StagingRoot',str(self.stage),'-WaitForProcessId','2147483647','-NoRelaunch']
  if recover:args+=['-RecoverOnly']
  return subprocess.run(args,env=self.env,capture_output=True,text=True,timeout=30)
 def test_complete_update_and_data_folder_status(self):
  result=self.run_helper();self.assertEqual(result.returncode,0,result.stderr)
  for n in self.names:self.assertEqual((self.install/n).read_bytes(),(self.stage/n).read_bytes())
  self.assertIn('state=applied',(self.root/'appdata/UnrealVRMod/FarFarWest-Win64-Shipping/data/farfarvr-updates.txt').read_text())
  self.assertFalse((self.install/'.farfarvr-update-transaction.json').exists())
 def test_corruption_rejected_before_installed_files_change(self):
  (self.stage/'Profile/config.txt').write_bytes(b'corrupt')
  result=self.run_helper();self.assertNotEqual(result.returncode,0)
  for n in self.names:self.assertTrue((self.install/n).read_bytes().startswith(b'old'))
 def test_recovery_after_interruption_restores_and_removes_created_files(self):
  backup=self.install/'.farfarvr-update-rollback-fixture';backup.mkdir()
  (backup/'0.bak').write_bytes(b'old launcher')
  (self.install/'Far Far VR.exe').write_bytes(b'partial new launcher')
  (self.install/'new.lua').write_bytes(b'new file')
  (self.install/'.farfarvr-update-transaction.json').write_text(json.dumps({'Version':'v0.4.7','BackupFolder':backup.name,'Entries':[
   {'Relative':'Far Far VR.exe','Existed':True,'Backup':backup.name+'/0.bak'},
   {'Relative':'new.lua','Existed':False,'Backup':backup.name+'/1.bak'}]}))
  result=self.run_helper(True);self.assertEqual(result.returncode,0,result.stderr)
  self.assertEqual((self.install/'Far Far VR.exe').read_bytes(),b'old launcher');self.assertFalse((self.install/'new.lua').exists())
 def test_rollback_attempts_remaining_files_and_retains_backups_when_one_fails(self):
  backup=self.install/'.farfarvr-update-rollback-fixture';backup.mkdir()
  (backup/'1.bak').write_bytes(b'restored')
  (self.install/'good.txt').write_bytes(b'partial')
  (self.install/'.farfarvr-update-transaction.json').write_text(json.dumps({'Version':'v0.4.7','BackupFolder':backup.name,'Entries':[
   {'Relative':'missing.txt','Existed':True,'Backup':backup.name+'/missing.bak'},
   {'Relative':'good.txt','Existed':True,'Backup':backup.name+'/1.bak'}]}))
  result=self.run_helper(True);self.assertNotEqual(result.returncode,0)
  self.assertEqual((self.install/'good.txt').read_bytes(),b'restored');self.assertTrue(backup.exists())
  self.assertTrue((self.install/'.farfarvr-update-transaction.json').exists())
if __name__=='__main__':unittest.main()
