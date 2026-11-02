using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace FarFarWestVR;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        LauncherLog.Write($"Launcher opened. executable={Environment.ProcessPath}; corrected runtime={Setup.CurrentBackendHash}; arguments={string.Join(' ', args)}");
        using var instance = new Mutex(true, @"Local\FarFarWestVR.Launcher", out bool first);
        if (!first) { LauncherLog.Write("Another launcher instance is open; no second launch was started."); return; }
        try { if (UpdateService.StartPendingApply(args)) return; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception)
        {
            LauncherLog.Write("Pending update could not be started: " + ex);
            MessageBox.Show(ex.Message, "Far Far VR update", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        ApplicationConfiguration.Initialize();
        string? runtimeOverride = null;
        int runtimeIndex = Array.IndexOf(args, "--runtime");
        if (runtimeIndex >= 0)
        {
            if (runtimeIndex + 1 >= args.Length || !Directory.Exists(args[runtimeIndex + 1]))
            {
                MessageBox.Show("Choose an existing VR runtime folder after --runtime.", "Far Far VR");
                return;
            }
            runtimeOverride = Path.GetFullPath(args[runtimeIndex + 1]);
        }
        Setup.UsePreviewPreferences = runtimeOverride != null;
        Application.Run(new LauncherForm(runtimeOverride));
    }
}

internal sealed class LauncherForm : Form
{
    private readonly Button start = new();
    private readonly Label status = new();
    private readonly ProgressBar progress = new();
    private CancellationTokenSource? cancellation;
    private bool closing;
    private int? sessionProcess;
    private SessionSnapshot? session;
    private bool handedOff;
    private readonly SessionLink sessionLink = new(Setup.ProfileFolder);
    private readonly System.Windows.Forms.Timer sessionTimer = new() { Interval=500 };
    private readonly UpdateService updateService = new(Setup.ProfileFolder);
    private Task? updateTask;
    private readonly Setup setup = Setup.Load();
    private readonly Button options = new();
    private static readonly Color Gold = Color.FromArgb(244, 173, 62);

    public LauncherForm(string? runtimeOverride = null)
    {
        if (runtimeOverride != null) setup.RuntimeFolder = runtimeOverride;
        Text = runtimeOverride == null ? "Far Far VR" : "Far Far VR — Menu Preview";
        ClientSize = new Size(760, 540);
        Theme.Form(this);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        SettingsIsolation.EnsureDesktopState();
        Controls.Add(Theme.Label("UNOFFICIAL FAR FAR WEST PCVR MOD",32,28,600,35,11,true,true));
        Controls.Add(Theme.Label("Far Far VR",30,68,690,57,32,true));
        Controls.Add(Theme.Label("Mod by PR_VR  |  Powered by UEVR created by praydog",33,130,690,32,11,false,true));
        var card = new Panel {Bounds=new Rectangle(32,200,696,178),BackColor=Theme.Surface,Padding=new Padding(24)};
        card.Controls.Add(Theme.Label("PCVR  /  FAR FAR WEST",24,20,640,24,9,true,true));
        card.Controls.Add(Theme.Label("One click to the West.",22,51,640,40,20,true));
        status.Text=File.Exists(setup.GamePath)?"Game found. Connect your headset to PCVR, then start.":"Choose your game folder in Settings to get started.";
        status.Bounds=new Rectangle(24,105,642,47);status.ForeColor=Theme.Muted;
        card.Controls.Add(status);Controls.Add(card);
        progress.Bounds=new Rectangle(32,388,696,4);progress.Style=ProgressBarStyle.Marquee;progress.Visible=false;
        start.Text="START IN VR";start.Bounds=new Rectangle(32,415,360,54);Theme.Button(start,true);
        start.Click+=async (_,_)=>{
            if(sessionProcess!=null){SetMenuVisible(session==null||!session.MenuVisible);return;}
            if(cancellation!=null)cancellation.Cancel();else await LaunchAsync();
        };
        options.Text="Settings";options.Bounds=new Rectangle(408,415,160,54);Theme.Button(options);
        options.Click+=(_,_)=>{
            if(sessionProcess!=null){SetMenuVisible(true);return;}
            using var dialog=new SetupForm(setup);
            if(dialog.ShowDialog(this)==DialogResult.OK)status.Text="Settings saved. Ready for your next session.";
        };
        var log=new LinkLabel {Text="View log",Bounds=new Rectangle(601,432,120,30),LinkColor=Theme.Muted,ActiveLinkColor=Theme.Accent};
        log.LinkClicked+=(_,_)=>{LauncherLog.Write("Log opened.");Process.Start(new ProcessStartInfo("notepad.exe"){ArgumentList={LauncherLog.Path},UseShellExecute=false});};
        var footer = new FlowLayoutPanel
        {
            Bounds = new Rectangle(34, 492, 690, 26),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        var settingsLabel = Theme.Label("VR SETTINGS   Hold Start or press Insert", 0, 0, 0, 0, 9, false, true);
        settingsLabel.AutoSize = true;
        settingsLabel.Margin = new Padding(0, 0, 10, 0);

        var sep1 = Theme.Label("•", 0, 0, 0, 0, 9, false, true);
        sep1.AutoSize = true;
        sep1.Margin = new Padding(0, 0, 10, 0);

        var yt = new LinkLabel
        {
            Text = "YouTube (@PR_VR)",
            Font = new Font("Segoe UI", 9),
            LinkColor = Theme.Accent,
            ActiveLinkColor = Theme.Text,
            LinkBehavior = LinkBehavior.HoverUnderline,
            AutoSize = true,
            Margin = new Padding(0, 0, 10, 0),
            UseMnemonic = false
        };
        var toolTip = new ToolTip();
        toolTip.SetToolTip(yt, "Support me by subscribing to my channel: https://www.youtube.com/@PR_VR");
        yt.LinkClicked += (_, _) => {
            LauncherLog.Write("Opening YouTube link.");
            Process.Start(new ProcessStartInfo("https://www.youtube.com/@PR_VR") { UseShellExecute = true });
        };

        var sep2 = Theme.Label("•", 0, 0, 0, 0, 9, false, true);
        sep2.AutoSize = true;
        sep2.Margin = new Padding(0, 0, 10, 0);

        var discord = new LinkLabel
        {
            Text = "VR Buddies Discord",
            Font = new Font("Segoe UI", 9),
            LinkColor = Theme.Accent,
            ActiveLinkColor = Theme.Text,
            LinkBehavior = LinkBehavior.HoverUnderline,
            AutoSize = true,
            Margin = new Padding(0),
            UseMnemonic = false
        };
        toolTip.SetToolTip(discord, "For questions and support for this mod, join the VR Buddies Discord: https://discord.gg/PcsYNa6MS9");
        discord.LinkClicked += (_, _) => {
            LauncherLog.Write("Opening Discord link.");
            Process.Start(new ProcessStartInfo("https://discord.gg/PcsYNa6MS9") { UseShellExecute = true });
        };

        var sep3 = Theme.Label("•", 0, 0, 0, 0, 9, false, true);
        sep3.AutoSize = true;
        sep3.Margin = new Padding(0, 0, 10, 0);

        var uevr = new LinkLabel
        {
            Text = "UEVR",
            Font = new Font("Segoe UI", 9),
            LinkColor = Theme.Accent,
            ActiveLinkColor = Theme.Text,
            LinkBehavior = LinkBehavior.HoverUnderline,
            AutoSize = true,
            Margin = new Padding(0, 0, 10, 0),
            UseMnemonic = false
        };
        toolTip.SetToolTip(uevr, "Powered by UEVR created by praydog: https://github.com/praydog/UEVR");
        uevr.LinkClicked += (_, _) => {
            LauncherLog.Write("Opening UEVR repository link.");
            Process.Start(new ProcessStartInfo("https://github.com/praydog/UEVR") { UseShellExecute = true });
        };

        var sep4 = Theme.Label("•", 0, 0, 0, 0, 9, false, true);
        sep4.AutoSize = true;
        sep4.Margin = new Padding(0, 0, 10, 0);

        var flat2vr = new LinkLabel
        {
            Text = "Flat2VR",
            Font = new Font("Segoe UI", 9),
            LinkColor = Theme.Accent,
            ActiveLinkColor = Theme.Text,
            LinkBehavior = LinkBehavior.HoverUnderline,
            AutoSize = true,
            Margin = new Padding(0),
            UseMnemonic = false
        };
        toolTip.SetToolTip(flat2vr, "Flat2VR Community: https://beacons.ai/flat2vr");
        flat2vr.LinkClicked += (_, _) => {
            LauncherLog.Write("Opening Flat2VR link.");
            Process.Start(new ProcessStartInfo("https://beacons.ai/flat2vr") { UseShellExecute = true });
        };

        footer.Controls.AddRange([settingsLabel, sep1, yt, sep2, discord, sep3, uevr, sep4, flat2vr]);
        Controls.Add(footer);
        Controls.AddRange([start,options,progress,log]);
        sessionTimer.Tick+=(_,_)=>{PollSession();PollUpdateCommand();};sessionTimer.Start();
        FormClosing += (_, _) => { closing = true; cancellation?.Cancel(); sessionTimer.Stop(); SettingsIsolation.ExitVrSession(); };
        FormClosed += (_,_)=>sessionTimer.Dispose();
    }

    private async Task LaunchAsync()
    {
        cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        start.Text = "CANCEL";
        progress.Visible = true;
        options.Enabled = false;
        TopMost = true;
        var updates = new Progress<string>(message => { if (!closing) status.Text = message; LauncherLog.Write(message); });
        try
        {
            SettingsIsolation.EnterVrSession();
            int processId=await Task.Run(
                () => GameLaunch.RunAsync(setup, updates, token, SettingsIsolation.UpdateSessionProcessId),
                token);
            if (closing) return;
            sessionProcess=processId;handedOff=false;session=null;
            status.Text="Changing to VR mode… Put on your headset. Far Far VR will appear in front of you.";
            LauncherLog.Write("VR hooks confirmed. Waiting for the live Far Far VR menu handoff.");
            start.Text="WAITING FOR HEADSET";start.Enabled=false;
            PollSession();
        }
        catch (OperationCanceledException)
        {
            if (!closing) status.Text = "Stopped waiting. The game stays open.";
            LauncherLog.Write("Waiting canceled; no game process was terminated.");
        }
        catch (Exception ex)
        {
            LauncherLog.Write(ex.ToString());
            if (!closing) status.Text = ex is Win32Exception { NativeErrorCode: 5 }
                ? "Access denied. Start Steam and the game normally, without Run as administrator."
                : ex.Message;
        }
        finally
        {
            cancellation.Dispose();
            cancellation = null;
            if (!closing && sessionProcess==null)
            {
                if (!SettingsIsolation.IsGameRunning()) SettingsIsolation.ExitVrSession();
                TopMost=false;start.Enabled=true;options.Enabled=true;start.Text="START IN VR";progress.Visible=false;
            }
            else if(!closing) PollSession();
        }
    }
    private void SetMenuVisible(bool visible)
    {
        try
        {
            if(session==null)throw new IOException("Waiting for your headset. Hold Start once VR is ready.");
            sessionLink.SetVisible(session,visible);
            status.Text=visible?"Opening Far Far VR in your headset…":"Returning to your game… Hold Start to show Far Far VR again.";
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){status.Text=ex.Message;}
    }
    private void PollSession()
    {
        if(closing||sessionProcess is not int processId)return;
        try
        {
            using var game=Process.GetProcessById(processId);
            if(game.HasExited||game.ProcessName!="FarFarWest-Win64-Shipping"){EndSession();return;}
        }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException){EndSession();return;}
        catch(Win32Exception){session=null;start.Enabled=false;options.Enabled=false;status.Text="Game status is unavailable. The headset menu is still available with Start.";return;}
        session=sessionLink.Read(processId,DateTimeOffset.UtcNow);
        if(session is not {RuntimeReady:true,MenuReady:true})
        {
            start.Enabled=false;options.Enabled=false;progress.Visible=!handedOff;
            status.Text=handedOff?"Waiting for your headset to reconnect. Your game stays open.":"Changing to VR mode… Connect to PCVR and put on your headset.";
            return;
        }
        if(!handedOff)
        {
            handedOff=true;TopMost=false;
            if(!session.MenuVisible)SetMenuVisible(true);
            LauncherLog.Write("Live Far Far VR menu is ready. Launcher remains open; headset menu owns VR input.");
        }
        status.Text="Far Far VR is ready. Hold Start to show or hide the menu in your headset.";
        start.Enabled=true;start.Text=session.MenuVisible?"RETURN TO GAME":"SHOW VR MENU";
        options.Enabled=true;options.Text="VR Settings";progress.Visible=false;
    }
    private void PollUpdateCommand()
    {
        if (updateTask is { IsCompleted: false }) return;
        string commandPath = Path.Combine(Setup.ProfileFolder, "farfarvr-update-command.txt");
        string command;
        try
        {
            if (!File.Exists(commandPath)) return;
            command = File.ReadAllText(commandPath).Trim();
            if (command is not ("check" or "stage")) return;
            File.Delete(commandPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LauncherLog.Write("Update command could not be read: " + ex.Message);
            return;
        }

        updateTask = Task.Run(async () =>
        {
            try
            {
                if (command == "check")
                {
                    updateService.WriteStatus("checking", "v" + UpdateService.CurrentVersion, "");
                    ReleaseUpdate? release = await updateService.CheckAsync();
                    updateService.WriteStatus(release is null ? "up-to-date" : "available",
                        release?.Version ?? "v" + UpdateService.CurrentVersion,
                        release?.Notes ?? "You are running the latest published Far Far VR release.");
                }
                else
                {
                    updateService.WriteStatus("downloading", "v" + UpdateService.CurrentVersion, "Downloading the update package...");
                    await updateService.StageLatestAsync();
                }
            }
            catch (Exception ex)
            {
                LauncherLog.Write("Update request failed: " + ex);
                try { updateService.WriteStatus("error", "v" + UpdateService.CurrentVersion, ex.Message); }
                catch (Exception statusError) { LauncherLog.Write("Update status could not be saved: " + statusError.Message); }
            }
        });
    }
    private void EndSession()
    {
        SettingsIsolation.ExitVrSession();
        sessionProcess=null;session=null;handedOff=false;TopMost=false;
        status.Text="Your game has closed. Ready for your next session.";
        start.Enabled=true;start.Text="START IN VR";options.Enabled=true;options.Text="Settings";progress.Visible=false;
    }

}

internal static class LauncherLog
{
    public static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FarFarWestVRLauncher", "launcher.log");
    private static readonly object Gate = new();
    public static void Write(string text)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.AppendAllText(Path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {text}{Environment.NewLine}");
        }
    }
}

internal static class GameLaunch
{
    private static readonly string GameLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FarFarWest", "Saved", "Logs", "FarFarWest.log");
    private static readonly string VrLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UnrealVRMod", "FarFarWest-Win64-Shipping", "log.txt");

    public static async Task<int> RunAsync(Setup setup, IProgress<string> status, CancellationToken token, Action<int> onGameFound)
    {
        await Setup.EnsureRuntimeAsync(setup, status, token);
        setup.Validate();
        Setup.EnsureProfile();
        string GamePath = setup.GamePath;
        string BackendPath = setup.BackendPath;
        using var identity = WindowsIdentity.GetCurrent();
        bool admin = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        LauncherLog.Write($"Launch requested. Administrator={admin}; backend={BackendPath}");
        foreach (string file in new[] { GamePath, BackendPath, Path.Combine(Path.GetDirectoryName(BackendPath)!, "LuaVR.dll"), Path.Combine(Path.GetDirectoryName(BackendPath)!, "openxr_loader.dll") })
            if (!File.Exists(file)) throw new IOException("A required game or VR file is missing. See the launcher log for its location: " + file);

        string gameBin = Path.GetDirectoryName(GamePath)!;
        string gameOpenXr = Path.Combine(gameBin, "openxr_loader.dll");
        string runtimeOpenXr = Path.Combine(Path.GetDirectoryName(BackendPath)!, "openxr_loader.dll");
        if (File.Exists(runtimeOpenXr) && !File.Exists(gameOpenXr))
        {
            try
            {
                File.Copy(runtimeOpenXr, gameOpenXr, overwrite: true);
                LauncherLog.Write($"Deployed openxr_loader.dll to game binary folder: {gameOpenXr}");
            }
            catch (Exception ex)
            {
                LauncherLog.Write($"Warning: Could not copy openxr_loader.dll to game folder: {ex.Message}");
            }
        }

        Process? game = FindGame(GamePath);
        if (game == null)
        {
            status.Report("Starting Far Far West through Steam…");
            using var activation = Process.Start(new ProcessStartInfo("steam://rungameid/3124540") { UseShellExecute = true });
            var deadline = DateTime.UtcNow.AddSeconds(150);
            while (game == null && DateTime.UtcNow < deadline)
            {
                await Task.Delay(700, token);
                game = FindGame(GamePath);
            }
            if (game == null) throw new TimeoutException("The game did not start. Open Steam, then try again.");
        }

        onGameFound(game.Id);
        using (game)
        {
            LauncherLog.Write($"Target PID={game.Id}; started={game.StartTime:O}");
            string? loaded = LoadedBackend(game);
            if (loaded != null)
            {
                if (!SamePath(loaded, BackendPath))
                    throw new InvalidOperationException("Another VR build is already loaded. Close the game, then use this launcher.");
                status.Report("VR is already loaded in the running game.");
                return game.Id;
            }

            status.Report("Waiting for the game to launch… Loading the title screen.");
            DateTime deadline = DateTime.UtcNow.AddMinutes(3);
            while (DateTime.UtcNow < deadline)
            {
                token.ThrowIfCancellationRequested();
                EnsureAlive(game);
                string log = ReadCurrentLog(GameLog, game.StartTime.ToUniversalTime());
                if (log.Contains("Load map complete /Game/Levels/L_Map_TitleScreen", StringComparison.Ordinal)) break;
                await Task.Delay(600, token);
            }
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("The title screen did not finish loading. The game is still open.");
            await Task.Delay(3000, token);
            EnsureAlive(game);
            token.ThrowIfCancellationRequested();

            // A second click or another injector must not load the backend twice.
            loaded = LoadedBackend(game);
            if (loaded != null)
            {
                if (!SamePath(loaded, BackendPath)) throw new InvalidOperationException("Another VR build was loaded while waiting. Close the game and try again.");
                return game.Id;
            }
            status.Report("Changing to VR mode…");
            DateTime injected = DateTime.UtcNow;
            if (File.Exists(runtimeOpenXr))
            {
                try { NativeLoader.Load(game, runtimeOpenXr); }
                catch (Exception ex) { LauncherLog.Write($"Pre-loading openxr_loader.dll: {ex.Message}"); }
            }
            NativeLoader.Load(game, BackendPath);
            LauncherLog.Write("Backend module confirmed in the game.");
            status.Report("Changing to VR mode… Finishing startup.");
            deadline = DateTime.UtcNow.AddSeconds(90);
            while (DateTime.UtcNow < deadline)
            {
                token.ThrowIfCancellationRequested();
                EnsureAlive(game);
                string log = ReadCurrentLog(VrLog, injected);
                if (log.Contains("Exception occurred:", StringComparison.Ordinal))
                    throw new InvalidOperationException("The game reported an error during VR startup. Details are in the VR log.");
                bool frameworkReady = log.Contains("Framework initialized", StringComparison.Ordinal);
                bool graphicsHooked = log.Contains("Hooked DirectX", StringComparison.Ordinal) ||
                    log.Contains("FFakeStereoRenderingHook", StringComparison.Ordinal);
                bool runtimeReady = new SessionLink(Setup.ProfileFolder)
                    .Read(game.Id, DateTimeOffset.UtcNow) is { RuntimeReady: true };
                if ((frameworkReady && graphicsHooked) || runtimeReady)
                    return game.Id;
                await Task.Delay(500, token);
            }
            throw new TimeoutException("VR was loaded, but startup was not confirmed. Check the game before trying again.");
        }
    }

    private static Process? FindGame(string gamePath)
    {
        var matches = new List<Process>();
        try
        {
            foreach (var process in Process.GetProcessesByName("FarFarWest-Win64-Shipping"))
            {
                try
                {
                    if (process.HasExited) { process.Dispose(); continue; }
                    if (SamePath(process.MainModule!.FileName, gamePath)) matches.Add(process);
                    else { process.Dispose(); throw new InvalidOperationException("Far Far West is running from a different folder. Close it or update Setup & options."); }
                }
                catch { process.Dispose(); throw; }
            }
            if (matches.Count > 1) throw new InvalidOperationException("More than one game instance is running. Keep one open and try again.");
            return matches.SingleOrDefault();
        }
        catch { foreach (var process in matches) process.Dispose(); throw; }
    }

    internal static string? LoadedBackend(Process game)
    {
        game.Refresh();
        foreach (ProcessModule module in game.Modules)
            if (module.ModuleName.Equals("UEVRBackend.dll", StringComparison.OrdinalIgnoreCase)) return module.FileName;
        return null;
    }

    private static string ReadCurrentLog(string path, DateTime minimumDate)
    {
        try
        {
            if (File.GetLastWriteTimeUtc(path) < minimumDate.AddSeconds(-1)) return "";
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (file.Length > 8 * 1024 * 1024) return "";
            using var reader = new StreamReader(file);
            return reader.ReadToEnd();
        }
        catch (IOException) { return ""; }
    }
    private static void EnsureAlive(Process game)
    {
        if (game.HasExited) throw new InvalidOperationException("The game closed during startup. You can try again.");
    }
    private static bool SamePath(string a, string b) => Path.GetFullPath(a).Equals(Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
}

internal static class NativeLoader
{
    public static void Load(Process game, string path)
    {
        IntPtr process = OpenProcess(0x43A, false, game.Id);
        if (process == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        IntPtr memory = IntPtr.Zero, thread = IntPtr.Zero;
        bool finished = true;
        try
        {
            byte[] bytes = Encoding.Unicode.GetBytes(path + '\0');
            memory = VirtualAllocEx(process, IntPtr.Zero, (nuint)bytes.Length, 0x3000, 4);
            if (memory == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (!WriteProcessMemory(process, memory, bytes, (nuint)bytes.Length, out nuint written) || written != (nuint)bytes.Length)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            IntPtr localLoad = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
            if (localLoad == IntPtr.Zero || !GetModuleHandleEx(6, localLoad, out IntPtr localModule))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            var name = new StringBuilder(1024);
            if (GetModuleFileName(localModule, name, name.Capacity) == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            string moduleName = Path.GetFileName(name.ToString());
            game.Refresh();
            ProcessModule remoteModule = game.Modules.Cast<ProcessModule>().Single(m => m.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
            IntPtr remoteLoad = new(remoteModule.BaseAddress.ToInt64() + localLoad.ToInt64() - localModule.ToInt64());
            thread = CreateRemoteThread(process, IntPtr.Zero, 0, remoteLoad, memory, 0, IntPtr.Zero);
            if (thread == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            finished = false;
            uint result = WaitForSingleObject(thread, 60000);
            if (result == 0) finished = true;
            else if (result == 0xFFFFFFFF) throw new Win32Exception(Marshal.GetLastWin32Error());
            else throw new TimeoutException("VR is taking longer to load. Wait before trying again; the game is still open.");
            if (game.HasExited) throw new InvalidOperationException("The game closed while VR was loading.");
            if (GameLaunch.LoadedBackend(game) is not string loaded || !Path.GetFullPath(loaded).Equals(Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Windows could not load the VR build. Check the game and launcher logs.");
        }
        finally
        {
            if (thread != IntPtr.Zero) CloseHandle(thread);
            // Never release the path while a timed-out loader thread may still read it.
            if (memory != IntPtr.Zero && finished) VirtualFreeEx(process, memory, 0, 0x8000);
            CloseHandle(process);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, bool inherit, int id);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, nuint size, uint allocation, uint protection);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool VirtualFreeEx(IntPtr process, IntPtr address, nuint size, uint type);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool WriteProcessMemory(IntPtr process, IntPtr address, byte[] buffer, nuint size, out nuint written);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr GetModuleHandle(string name);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)] private static extern IntPtr GetProcAddress(IntPtr module, string name);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool GetModuleHandleEx(uint flags, IntPtr address, out IntPtr module);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern uint GetModuleFileName(IntPtr module, StringBuilder name, int size);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateRemoteThread(IntPtr process, IntPtr attributes, nuint stack, IntPtr start, IntPtr argument, uint flags, IntPtr id);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);
}
