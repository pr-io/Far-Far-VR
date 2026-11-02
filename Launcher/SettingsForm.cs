using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace FarFarWestVR;

internal sealed class SetupForm : Form
{
    private readonly Dictionary<string, Func<string>> readControls = new();
    private readonly Dictionary<string, string> originalControls = new();
    private readonly Dictionary<string, NumericUpDown> numbers = new();
    private readonly Panel content = new() { Bounds = new Rectangle(210, 110, 630, 480) };
    private readonly Dictionary<string, Panel> pages = new();
    private readonly List<Button> navigation = new();
    public SetupForm(Setup setup)
    {
        Text = "Far Far VR · Settings"; ClientSize = new Size(870, 680); Theme.Form(this);
        StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        Controls.Add(Theme.Label("SETTINGS",30,24,600,24,10,true,true));
        Controls.Add(Theme.Label("Make VR feel right.",28,51,700,45,24,true));
        Controls.Add(content);
        foreach (string name in new[] {"Setup","Comfort","Calibration","Input","HUD","Display"}) AddPage(name);
        var game = Folder("Far Far West installation",setup.GameFolder,pages["Setup"]);
        var runtime = Folder("VR runtime",setup.RuntimeFolder,pages["Setup"]);
        var detect = MakeButton("Find game in Steam",new Rectangle(0,180,220,40));
        detect.Click += (_,_) => { string? found=Setup.DetectGame(); if(found!=null) game.Text=found; else MessageBox.Show(this,"Game not found. Select its installation folder with Browse."); };
        pages["Setup"].Controls.Add(detect);
        var download = new LinkLabel { Text="Get compatible runtime", Bounds=new Rectangle(250,190,290,30), LinkColor=Theme.Accent, ActiveLinkColor=Theme.Text };
        download.LinkClicked += (_,_) => Process.Start(new ProcessStartInfo("https://github.com/joeyhodge/UEVR/releases/tag/102626_FFR_SWGR_Patch"){UseShellExecute=true});
        pages["Setup"].Controls.Add(download);
        pages["Setup"].Controls.Add(Theme.Label("The launcher stays open. Once VR is ready, Far Far VR settings appear in your headset.",0,245,575,56,10,false,true));
        pages["Setup"].Controls.Add(Theme.Label("Connect your headset to PCVR before starting.\nOpen VR settings by holding Start or pressing Insert.",0,310,575,80,10,false,true));
        string profile=Path.Combine(Setup.ProfileFolder,"config.txt");
        string source=File.Exists(profile)?profile:Path.Combine(AppContext.BaseDirectory,"Profile","config.txt");
        string[] original=File.ReadAllLines(source);
        string Read(string key) => original.FirstOrDefault(line=>line.StartsWith(key+"=",StringComparison.Ordinal))?.Split('=',2)[1] ?? throw new IOException("Profile setting missing: "+key);
        var fields=JsonSerializer.Deserialize<SettingsField[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Profile","VRSettings.json")))!;
        var positions=new Dictionary<string,int>{{"Comfort",60},{"Calibration",138},{"Input",60},{"HUD",60},{"Display",60}};
        pages["Comfort"].Controls.Add(Theme.Label("Turning, movement and performance feedback.",0,8,580,45,11,false,true));
        pages["Display"].Controls.Add(Theme.Label("Frame rate and desktop mirror. Headset resolution and refresh rate stay in the PCVR app.",0,8,580,45,10,false,true));
        pages["Input"].Controls.Add(Theme.Label("The corrected Touch button mapping is preserved.",0,8,580,45,11,false,true));
        pages["HUD"].Controls.Add(Theme.Label("Place the game interface and settings menu in VR.",0,8,580,45,11,false,true));
        pages["Calibration"].Controls.Add(Theme.Label("Recenter view changes direction, not height.\nFor current-pose calibration, use Calibration in the in-game menu.",0,8,580,54,10,false,true));
        var reset=MakeButton("Reset height & camera",new Rectangle(0,77,250,42));
        reset.Click += (_,_) => { foreach(var key in new[]{"VR_CameraUpOffset","VR_CameraForwardOffset","VR_CameraRightOffset"}) numbers[key].Value=0; };
        pages["Calibration"].Controls.Add(reset);
        foreach(var field in fields)
        {
            int y=positions[field.group]; positions[field.group]+=62;
            var page=pages[field.group];
            page.Controls.Add(Theme.Label(field.label+(field.unit.Length>0?"  ·  "+field.unit:""),0,y+7,345,34,10));
            Control editor;
            if(field.kind=="bool")
            {
                var check=new CheckBox {Text="Enabled",Checked=Read(field.key)=="true",ForeColor=Theme.Text}; editor=check;
                readControls[field.key]=()=>check.Checked?"true":"false";
            }
            else if(field.kind=="choice")
            {
                var combo=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Theme.Surface,ForeColor=Theme.Text,FlatStyle=FlatStyle.Flat};
                combo.Items.AddRange(field.choices); combo.SelectedIndex=int.Parse(Read(field.key),CultureInfo.InvariantCulture);
                editor=combo; readControls[field.key]=()=>combo.SelectedIndex.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                decimal value=decimal.Parse(Read(field.key),CultureInfo.InvariantCulture);
                var numeric=new NumericUpDown {Minimum=field.min,Maximum=field.max,DecimalPlaces=field.kind=="int"?0:2,Increment=field.unit=="cm"?1:field.kind=="int"?5:.05m,
                    Value=Math.Clamp(value,field.min,field.max),BackColor=Theme.Surface,ForeColor=Theme.Text,BorderStyle=BorderStyle.FixedSingle};
                editor=numeric; numbers[field.key]=numeric; readControls[field.key]=()=>numeric.Value.ToString(CultureInfo.InvariantCulture);
            }
            editor.Bounds=new Rectangle(360,y+4,220,34); editor.AccessibleName=field.label; page.Controls.Add(editor);
            originalControls[field.key]=readControls[field.key]();
        }
        var footer=Theme.Label("VR changes apply next launch. Save creates a profile backup.",30,608,540,44,9,false,true); Controls.Add(footer);
        var save=MakeButton("Save changes",new Rectangle(660,605,180,46),true);
        var cancel=MakeButton("Cancel",new Rectangle(545,605,100,46)); cancel.DialogResult=DialogResult.Cancel;
        save.Click += (_,_)=>
        {
            try
            {
                var candidate=new Setup {GameFolder=game.Text.Trim(),RuntimeFolder=runtime.Text.Trim(),CloseAfterLaunch=false}; candidate.Validate(allowPendingRuntime: true);
                var changes=readControls.Where(pair=>pair.Value()!=originalControls[pair.Key]).ToDictionary(pair=>pair.Key,pair=>pair.Value());
                if(changes.Count>0)
                {
                    var processes=Process.GetProcessesByName("FarFarWest-Win64-Shipping"); bool running=processes.Length>0; foreach(var p in processes)p.Dispose();
                    if(running)throw new IOException("Close Far Far West before changing saved VR settings.");
                    Setup.EnsureProfile();
                    if(!File.ReadAllLines(profile).SequenceEqual(original))throw new IOException("The profile changed. Reopen Settings before saving.");
                    var lines=original.ToList();
                    foreach(var pair in changes) { int index=lines.FindIndex(line=>line.StartsWith(pair.Key+"=",StringComparison.Ordinal)); if(index<0)lines.Add(pair.Key+"="+pair.Value);else lines[index]=pair.Key+"="+pair.Value; }
                    File.WriteAllLines(profile+".pending",lines); File.Replace(profile+".pending",profile,profile+".backup-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ"));
                }
                candidate.Save(); setup.GameFolder=candidate.GameFolder; setup.RuntimeFolder=candidate.RuntimeFolder; setup.CloseAfterLaunch=candidate.CloseAfterLaunch; DialogResult=DialogResult.OK;
            }
            catch(Exception ex){MessageBox.Show(this,ex.Message,"Settings need attention",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        };
        Controls.AddRange([save,cancel]); AcceptButton=save; CancelButton=cancel; ShowPage("Setup");
    }
    private static Button MakeButton(string text,Rectangle bounds,bool primary=false){var b=new Button{Text=text,Bounds=bounds};Theme.Button(b,primary);return b;}
    private void AddPage(string name)
    {
        var page=new Panel {Dock=DockStyle.Fill,AutoScroll=true,BackColor=Theme.Background,Visible=false};pages[name]=page;content.Controls.Add(page);
        var button=MakeButton(name=="HUD"?"Interface":name,new Rectangle(28,114+navigation.Count*54,152,43)); button.Tag=name;button.Click+=(_,_)=>ShowPage(name);navigation.Add(button);Controls.Add(button);
    }
    private void ShowPage(string name){foreach(var pair in pages)pair.Value.Visible=pair.Key==name;foreach(var b in navigation){b.BackColor=(string)b.Tag! ==name?Theme.Accent:Theme.Surface;b.ForeColor=(string)b.Tag! ==name?Theme.Background:Theme.Text;}}
    private TextBox Folder(string label,string value,Panel page)
    {
        int y=page.Controls.Count==0?0:90;
        page.Controls.Add(Theme.Label(label,0,y,580,28,10,true));
        var text=new TextBox {Text=value,Bounds=new Rectangle(0,y+34,466,30),BackColor=Theme.Surface,ForeColor=Theme.Text,BorderStyle=BorderStyle.FixedSingle};
        var browse=MakeButton("Browse",new Rectangle(480,y+30,100,37));browse.Click+=(_,_)=>{using var dialog=new FolderBrowserDialog{Description=label,UseDescriptionForTitle=true,InitialDirectory=Directory.Exists(text.Text)?text.Text:""};if(dialog.ShowDialog(this)==DialogResult.OK)text.Text=dialog.SelectedPath;};
        page.Controls.AddRange([text,browse]);return text;
    }
}
