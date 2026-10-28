using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace FarFarWestVR;

internal static class Theme
{
    internal static readonly Color Background = Color.FromArgb(17, 21, 27);
    internal static readonly Color Surface = Color.FromArgb(26, 33, 42);
    internal static readonly Color Border = Color.FromArgb(46, 57, 70);
    internal static readonly Color Text = Color.FromArgb(239, 243, 247);
    internal static readonly Color Muted = Color.FromArgb(154, 167, 183);
    internal static readonly Color Accent = Color.FromArgb(238, 187, 117);
    internal static Label Label(string text, int x, int y, int w, int h, float size = 10, bool bold = false, bool muted = false) =>
        new() { Text = text, Bounds = new Rectangle(x,y,w,h), ForeColor = muted ? Muted : Text,
            Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), UseMnemonic = false };
    internal static void Button(Button button, bool primary = false)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = primary ? Accent : Surface;
        button.ForeColor = primary ? Background : Text;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.UseMnemonic = false;
        button.Resize += (_, _) => Round(button, 12);
        Round(button, 12);
    }
    private static void Round(Control control, int radius)
    {
        if (control.Width < radius || control.Height < radius) return;
        using var path = new GraphicsPath();
        path.AddArc(0,0,radius,radius,180,90); path.AddArc(control.Width-radius,0,radius,radius,270,90);
        path.AddArc(control.Width-radius,control.Height-radius,radius,radius,0,90); path.AddArc(0,control.Height-radius,radius,radius,90,90);
        path.CloseFigure();
        var previous = control.Region; control.Region = new Region(path); previous?.Dispose();
    }
    internal static void Form(Form form)
    {
        form.BackColor = Background; form.ForeColor = Text; form.Font = new Font("Segoe UI",10);
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.HandleCreated += (_, _) => { int enabled=1; DwmSetWindowAttribute(form.Handle,20,ref enabled,sizeof(int)); };
    }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
}

internal sealed class SettingsField
{
    public string group { get; set; } = "";
    public string key { get; set; } = "";
    public string label { get; set; } = "";
    public string kind { get; set; } = "";
    public decimal min { get; set; }
    public decimal max { get; set; }
    public string unit { get; set; } = "";
    public string[] choices { get; set; } = [];
}
