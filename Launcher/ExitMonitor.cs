using System.Diagnostics;
using System.Globalization;
namespace FarFarWestVR;
internal static class ExitMonitor
{
    internal static bool ShutdownComplete(string tail) {
        var lines=tail.Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        const string prefix=@"^\[\d{4}\.\d{2}\.\d{2}-[^\]]+\]\[\s*\d+\]";
        return lines.Length>=2 && System.Text.RegularExpressions.Regex.IsMatch(lines[^2],prefix+@"LogExit: Exiting\.$") &&
            System.Text.RegularExpressions.Regex.IsMatch(lines[^1],prefix+@"Log file closed,");
    }
    internal static void Start(Process game,string gamePath,string gameLog)
    {
        try {
            var info=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};
            foreach(var arg in new[]{"--watch-exit",game.Id.ToString(CultureInfo.InvariantCulture),game.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture),gamePath,gameLog})info.ArgumentList.Add(arg);
            foreach(var key in new[]{"SteamAppId","SteamGameId","SteamOverlayGameId"})info.Environment.Remove(key);
            using var child=Process.Start(info);
        }catch(Exception ex){LauncherLog.Write("Exit monitor unavailable: "+ex.Message);}
    }
    internal static bool TryRun(string[] args)
    {
        if(args.Length==0||args[0]!="--watch-exit")return false;
        if(args.Length!=5||!int.TryParse(args[1],out int id)||!long.TryParse(args[2],out long ticks))return true;
        try {
            using var guard=new Mutex(true,$@"Local\FarFarVR.Exit.{id}.{ticks}",out bool first);if(!first)return true;
            using var game=Process.GetProcessById(id);
            if(game.ProcessName!="FarFarWest-Win64-Shipping"||game.StartTime.ToUniversalTime().Ticks!=ticks||
                !Path.GetFullPath(game.MainModule!.FileName).Equals(Path.GetFullPath(args[3]),StringComparison.OrdinalIgnoreCase))return true;
            // Retain this exact process handle; never rediscover or kill by name.
            _=game.Handle;
            DateTime? completed=null;
            while(!game.WaitForExit(1000)) {
                string tail="";
                try {
                    if(File.GetLastWriteTimeUtc(args[4]).Ticks>=ticks) {
                        using var file=new FileStream(args[4],FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
                        file.Seek(Math.Max(0,file.Length-16384),SeekOrigin.Begin);using var reader=new StreamReader(file);tail=reader.ReadToEnd();
                    }
                }catch(IOException){}
                if(!ShutdownComplete(tail)){completed=null;continue;}
                completed??=DateTime.UtcNow;
                if(DateTime.UtcNow-completed.Value<TimeSpan.FromSeconds(8))continue;
                LauncherLog.Write($"Game PID {id} finished saving and closed its log, but process teardown stalled. Ending that exact completed session.");
                game.Kill();game.WaitForExit(10000);return true;
            }
        }catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException){LauncherLog.Write("Exit monitor: "+ex.Message);}
        return true;
    }
}
