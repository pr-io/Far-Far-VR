namespace FarFarWestVR;
internal sealed class SteamLaunchLog
{
    private readonly string path;
    private readonly long start;
    internal SteamLaunchLog(string root){path=Path.Combine(root,"logs","console_log.txt");try{start=new FileInfo(path).Length;}catch(IOException){}}
    internal static bool Refused(string text)=>text.Split('\n').Any(line=>line.Contains("GameAction [AppID 3124540,",StringComparison.Ordinal)&&line.Contains("LaunchApp failed",StringComparison.Ordinal));
    internal bool Refused(){try{
        using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        long offset=file.Length<start?0:start;file.Seek(Math.Max(offset,file.Length-65536),SeekOrigin.Begin);
        using var reader=new StreamReader(file);return Refused(reader.ReadToEnd());
    }catch(IOException){return false;}}
}
