using System.Text.Json;

namespace FarFarWestVR;

internal sealed record SessionSnapshot(int ProcessId, string Session, long WrittenAt, bool RuntimeReady, bool MenuReady, bool MenuVisible);

// A narrow local bridge: only show/hide the existing in-game menu.
internal sealed class SessionLink(string profile)
{
    public SessionSnapshot? Read(int processId, DateTimeOffset now)
    {
        try
        {
            using var file=new FileStream(Path.Combine(profile,"farfarvr-session.json"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
            if(file.Length>2048)return null;
            using var document=JsonDocument.Parse(file);var root=document.RootElement;
            var result=new SessionSnapshot(root.GetProperty("processId").GetInt32(),root.GetProperty("session").GetString()??"",
                root.GetProperty("writtenAt").GetInt64(),root.GetProperty("runtimeReady").GetBoolean(),root.GetProperty("menuReady").GetBoolean(),root.GetProperty("menuVisible").GetBoolean());
            long age=now.ToUnixTimeMilliseconds()-result.WrittenAt;
            return result.ProcessId==processId&&processId>0&&result.Session.Length is >0 and <=128&&age is >=-1000 and <=3000?result:null;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException){return null;}
    }
    public void SetVisible(SessionSnapshot snapshot,bool visible)
    {
        var current=Read(snapshot.ProcessId,DateTimeOffset.UtcNow);
        if(current==null||current.Session!=snapshot.Session||!current.RuntimeReady||!current.MenuReady)
            throw new IOException("The VR menu is not ready. Put on your headset, then try again.");
        string path=Path.Combine(profile,"farfarvr-menu-command.json"),pending=path+".pending";
        var command=new {processId=current.ProcessId,session=current.Session,id=Guid.NewGuid().ToString("N"),sentAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),action=visible?"show":"hide"};
        File.WriteAllText(pending,JsonSerializer.Serialize(command));File.Move(pending,path,true);
    }
}
