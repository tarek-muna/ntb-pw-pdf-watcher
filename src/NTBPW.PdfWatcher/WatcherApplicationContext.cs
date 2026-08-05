using System.Drawing.Drawing2D;
using System.Threading;

namespace NTBPW.PdfWatcher;

internal sealed class WatcherApplicationContext : ApplicationContext
{
    private readonly MainForm _mainForm;
    private readonly NotifyIcon _trayIcon;
    private readonly EventWaitHandle _showEvent;
    private readonly System.Windows.Forms.Timer _activationTimer;
    private readonly Icon _icon;
    private bool _exiting;

    public WatcherApplicationContext(string showEventName)
    {
        _icon = LoadApplicationIcon();
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, showEventName);

        _mainForm = new MainForm(_icon);
        _mainForm.RequestHideToTray += (_, _) => HideMainWindow(true);
        _mainForm.RequestExit += (_, _) => ExitApplication();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Dashboard öffnen", null, (_, _) => ShowMainWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Überwachung starten", null, (_, _) => _mainForm.StartMonitoring());
        menu.Items.Add("Überwachung stoppen", null, (_, _) => _mainForm.StopMonitoring());
        menu.Items.Add("Protokoll anzeigen", null, (_, _) => _mainForm.ShowLogViewer());
        menu.Items.Add("Nach Updates suchen", null, (_, _) => _mainForm.ShowUpdateDialog());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => ExitApplication());

        _trayIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "NTB-PW PDF-Watcher",
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) ShowMainWindow();
        };

        _activationTimer = new System.Windows.Forms.Timer { Interval = 400 };
        _activationTimer.Tick += (_, _) =>
        {
            if (_showEvent.WaitOne(0)) ShowMainWindow();
        };
        _activationTimer.Start();

        AppLogger.Write("Version 4.2.0 gestartet. Tray-Icon initialisiert.");

        var minimized = Environment.GetCommandLineArgs()
            .Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));
        if (minimized)
            HideMainWindow(false);
        else
            ShowMainWindow();
    }

    public void ShowMainWindow()
    {
        if (_mainForm.IsDisposed) return;
        if (!_mainForm.Visible) _mainForm.Show();
        _mainForm.ShowInTaskbar = true;
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.BringToFront();
        _mainForm.Activate();
        _mainForm.Focus();
    }

    private void HideMainWindow(bool notification)
    {
        _mainForm.Hide();
        _mainForm.ShowInTaskbar = false;
        if (notification)
        {
            _trayIcon.ShowBalloonTip(
                2500,
                "NTB-PW PDF-Watcher",
                "Die Überwachung läuft im Infobereich weiter.",
                ToolTipIcon.Info);
        }
    }

    private void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;
        _activationTimer.Stop();
        _trayIcon.Visible = false;
        _mainForm.Shutdown();
        _mainForm.Dispose();
        _trayIcon.Dispose();
        _showEvent.Dispose();
        _icon.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_exiting) ExitApplication();
        base.Dispose(disposing);
    }

    private static Icon LoadApplicationIcon()
    {
        try
        {
            var extracted = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (extracted is not null) return (Icon)extracted.Clone();
        }
        catch (Exception ex)
        {
            AppLogger.Write("EXE-Icon konnte nicht geladen werden: " + ex.Message);
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "NTB-PW.ico");
        try
        {
            if (File.Exists(iconPath)) return new Icon(iconPath);
        }
        catch (Exception ex)
        {
            AppLogger.Write("Externes Icon konnte nicht geladen werden: " + ex.Message);
        }

        return CreateFallbackNtbIcon();
    }

    private static Icon CreateFallbackNtbIcon()
    {
        using var bitmap = new Bitmap(64, 64);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new LinearGradientBrush(
            new Rectangle(0, 0, 64, 64),
            Color.FromArgb(27, 88, 180),
            Color.FromArgb(32, 166, 91),
            35f);
        graphics.FillEllipse(brush, 2, 2, 60, 60);
        using var font = new Font("Segoe UI", 17, FontStyle.Bold, GraphicsUnit.Pixel);
        var text = "NTB";
        var size = graphics.MeasureString(text, font);
        graphics.DrawString(text, font, Brushes.White, (64 - size.Width) / 2, (64 - size.Height) / 2);
        var handle = bitmap.GetHicon();
        try { return (Icon)Icon.FromHandle(handle).Clone(); }
        finally { NativeMethods.DestroyIcon(handle); }
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern bool DestroyIcon(IntPtr handle);
    }
}
