using System.Diagnostics;
using FarFarWestVR;
if(ExitMonitor.TryRun(args))return;
string fixture=Path.GetFullPath(args[0]),root=Path.Combine(Path.GetTempPath(),"ffvr-exit-"+Guid.NewGuid());Directory.CreateDirectory(root);
void Check(bool value,string message){if(!value)throw new Exception(message);}
Process Spawn(string path,params string[] argv){var info=new ProcessStartInfo(path){UseShellExecute=false,CreateNoWindow=true};foreach(var value in argv)info.ArgumentList.Add(value);return Process.Start(info)!;}
Process Watch(Process target,string log,long? ticks=null)=>Spawn(Environment.ProcessPath!,"--watch-exit",target.Id.ToString(),(ticks??target.StartTime.ToUniversalTime().Ticks).ToString(),fixture,log);
Check(!SteamLaunchLog.Refused("GameAction [AppID 12, ActionID 1] : LaunchApp failed"),"Other game's failure misattributed");
Check(!SteamLaunchLog.Refused("GameAction [AppID 3124540, ActionID 1] : LaunchApp changed task to WaitingPrevProcess"),"Pending launch misattributed");
Check(SteamLaunchLog.Refused("GameAction [AppID 3124540, ActionID 1] : LaunchApp failed with AppError_16"),"Steam refusal missed");
var steamRoot=Path.Combine(root,"steam");Directory.CreateDirectory(Path.Combine(steamRoot,"logs"));string console=Path.Combine(steamRoot,"logs","console_log.txt");
File.WriteAllText(console,"GameAction [AppID 3124540, ActionID 1] : LaunchApp failed\n");var launchLog=new SteamLaunchLog(steamRoot);Check(!launchLog.Refused(),"Old Steam error reused");File.AppendAllText(console,"GameAction [AppID 3124540, ActionID 2] : LaunchApp failed\n");Check(launchLog.Refused(),"New Steam error missed");
foreach(string mode in new[]{"active","complete"}){
 string log=Path.Combine(root,mode+".log");using var target=Spawn(fixture,mode,log);
 try {
  for(int i=0;i<100&&!File.Exists(log);i++)Thread.Sleep(50);Check(File.Exists(log),"Fixture did not start");
  if(mode=="active"){
   using var wrongIdentity=Watch(target,log,target.StartTime.ToUniversalTime().Ticks+1);Check(wrongIdentity.WaitForExit(3000),"Wrong identity watcher did not refuse");Check(!target.HasExited,"Wrong identity killed a process");
   using var watch=Watch(target,log);Thread.Sleep(10000);Check(!target.HasExited,"Incomplete shutdown killed an active session");target.Kill();Check(watch.WaitForExit(4000),"Watcher did not exit with target");
  }else{
   using var watch=Watch(target,log);Check(target.WaitForExit(14000),"Completed shutdown was not cleaned up");Check(watch.WaitForExit(4000),"Cleanup watcher did not finish");
  }
 }finally{if(!target.HasExited)target.Kill();}
}
Console.WriteLine("Real-process checks passed: active session preserved, wrong creation identity refused, completed shutdown cleaned up, monitor exited.");
namespace FarFarWestVR{internal static class LauncherLog{internal static void Write(string value)=>Console.WriteLine(value);}}
