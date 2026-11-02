using FarFarWestVR;
using System.Text.Json;
var path=Path.Combine(Path.GetTempPath(),"farfarvr-session-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(path);
void Check(bool result){if(!result)throw new Exception("Session handoff check failed");}
var link=new SessionLink(path);long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
void Write(int pid,string session,long stamp,bool ready=true,bool menu=true)=>File.WriteAllText(Path.Combine(path,"farfarvr-session.json"),JsonSerializer.Serialize(new {processId=pid,session,writtenAt=stamp,runtimeReady=ready,menuReady=menu,menuVisible=true}));
try {
 Check(link.Read(10,DateTimeOffset.UtcNow)==null);
 File.WriteAllText(Path.Combine(path,"farfarvr-session.json"),"{");Check(link.Read(10,DateTimeOffset.UtcNow)==null);
 Write(10,"session",now);var good=link.Read(10,DateTimeOffset.UtcNow);Check(good is {RuntimeReady:true,MenuReady:true,MenuVisible:true});
 Check(link.Read(11,DateTimeOffset.UtcNow)==null);
 Write(10,"session",now-4000);Check(link.Read(10,DateTimeOffset.UtcNow)==null);
 Write(10,"session",now+10000);Check(link.Read(10,DateTimeOffset.UtcNow)==null);
 Write(10,"",now);Check(link.Read(10,DateTimeOffset.UtcNow)==null);
 Write(10,"session",now);link.SetVisible(good!,false);
 using(var command=JsonDocument.Parse(File.ReadAllText(Path.Combine(path,"farfarvr-menu-command.json")))) {
  Check(command.RootElement.GetProperty("action").GetString()=="hide");Check(command.RootElement.GetProperty("session").GetString()=="session");Check(command.RootElement.GetProperty("processId").GetInt32()==10);
 }
 Write(10,"new-session",now);bool rejected=false;try{link.SetVisible(good!,true);}catch(IOException){rejected=true;}Check(rejected);
 Write(10,"session",now,false);rejected=false;try{link.SetVisible(good!,true);}catch(IOException){rejected=true;}Check(rejected);
 Write(10,"session",now);link.SetVisible(good!,true);using(var command=JsonDocument.Parse(File.ReadAllText(Path.Combine(path,"farfarvr-menu-command.json"))))Check(command.RootElement.GetProperty("action").GetString()=="show");
 Console.WriteLine("Session live/stale/PID/restart/readiness checks and scoped show/hide requests passed");
} finally {Directory.Delete(path,true);}
