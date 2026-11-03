using System.Diagnostics;

namespace FarFarWestVR;

internal sealed class UpdatesForm : Form
{
    private readonly Label status = Theme.Label("Check for the latest release.", 24, 112, 650, 52);
    private readonly TextBox notes = new() { Multiline=true, ReadOnly=true, ScrollBars=ScrollBars.Vertical,
        Bounds=new Rectangle(24,192,650,222), BackColor=Theme.Surface, ForeColor=Theme.Text, BorderStyle=BorderStyle.FixedSingle };
    private readonly Button check = new() { Text="Check for updates", Bounds=new Rectangle(24,434,195,44) };
    private readonly Button download = new() { Text="Download update", Bounds=new Rectangle(231,434,210,44) };
    private readonly Button install = new() { Text="Install & restart", Bounds=new Rectangle(453,434,221,44) };
    private readonly System.Windows.Forms.Timer timer = new() { Interval=500 };
    private string state = "idle";
    private readonly Func<bool> busy;

    internal UpdatesForm(UpdateService service, Func<string,Task> run, Func<bool> busy)
    {
        this.busy=busy;
        Text="Far Far VR — Updates"; ClientSize=new Size(698,512); Theme.Form(this);
        FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false;
        StartPosition=FormStartPosition.CenterParent;
        Controls.Add(Theme.Label("Mod updates",24,20,650,38,22,true));
        Controls.Add(Theme.Label("Installed version: v"+UpdateService.CurrentVersion,24,64,650,24,10,false,true));
        Controls.Add(Theme.Label("Release notes / update details",24,166,650,24,10,true));
        Theme.Button(check); Theme.Button(download,true); Theme.Button(install,true);
        Controls.AddRange([status,notes,check,download,install]);
        var repo=new LinkLabel { Text="pr-io/Far-Far-VR on GitHub", Bounds=new Rectangle(24,484,650,24), LinkColor=Theme.Accent };
        repo.LinkClicked+=(_,_)=>Process.Start(new ProcessStartInfo("https://github.com/pr-io/Far-Far-VR/releases") { UseShellExecute=true });
        Controls.Add(repo);
        check.Click+=async (_,_)=>{ var task=run("check"); RefreshStatus(); await task; if(!IsDisposed)RefreshStatus(); };
        download.Click+=async (_,_)=>{ var task=run("stage"); RefreshStatus(); await task; if(!IsDisposed)RefreshStatus(); };
        install.Click+=(_,_)=>{
            if(busy())return;
            if(SettingsIsolation.IsGameRunning()) { status.Text="Close Far Far West before installing. Your download is ready."; return; }
            try {
                if(UpdateService.StartPendingApply(["--apply-update"])) Application.Exit();
            } catch(Exception ex) { status.Text="Could not start installation: "+ex.Message; }
        };
        timer.Tick+=(_,_)=>RefreshStatus();
        FormClosed+=(_,_)=>{timer.Stop();timer.Dispose();};
        RefreshStatus(); timer.Start();
    }

    private void RefreshStatus()
    {
        try {
            string path=UpdateService.StatusPath(Setup.ProfileFolder);
            if(File.Exists(path)) {
                string content=File.ReadAllText(path).Replace("\r\n","\n");
                string Get(string key)=>content.Split('\n').FirstOrDefault(line=>line.StartsWith(key+"="))?[(key.Length+1)..]??"";
                state=Get("state"); string version=Get("version");
                int offset=content.IndexOf("notes:\n",StringComparison.Ordinal);
                string detail=offset>=0?content[(offset+7)..]:"";
                if(notes.Text.Replace("\r\n","\n")!=detail)notes.Text=detail.Replace("\n",Environment.NewLine);
                status.Text=state switch {
                    "checking"=>"Checking GitHub releases…",
                    "downloading"=>"Downloading and verifying the update…",
                    "available"=>"Update available: "+version,
                    "up-to-date"=>"You're up to date. Installed: v"+UpdateService.CurrentVersion,
                    "prepared"=>"Ready to install: "+version+". Close the game, then select Install & restart.",
                    "applied"=>"Update installed.",
                    "error"=>"Update failed. See the details below, then try again.",
                    _=>"Check for the latest release."
                };
            }
        } catch(IOException) { } catch(UnauthorizedAccessException) { status.Text="Cannot read update status."; }
        bool working=busy();
        check.Enabled=!working; download.Enabled=!working&&state=="available";
        install.Enabled=!working&&state=="prepared";
    }
}
