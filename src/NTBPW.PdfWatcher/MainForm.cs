using System.Diagnostics;

namespace NTBPW.PdfWatcher;

internal sealed class MainForm : Form
{
    private AppConfig _config = ConfigStore.Load();
    private readonly List<ProcessingRecord> _history = ConfigStore.LoadHistory();
    private MultiWatcherService _watcher;
    private readonly Label _status = new() { AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
    private readonly Label _last = new() { AutoSize = true, Text = "–" };
    private readonly ListBox _profiles = new() { Dock = DockStyle.Fill };
    private readonly TextBox _log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly Icon _appIcon;
    private bool _reallyExit;
    private bool _automaticUpdateCheckStarted;

    public event EventHandler? RequestHideToTray;
    public event EventHandler? RequestExit;

    public MainForm(Icon appIcon)
    {
        Text = "NTB-PW PDF-Watcher Professional 4.2.0";
        Width = 800;
        Height = 540;
        MinimumSize = new Size(720, 480);
        StartPosition = FormStartPosition.CenterScreen;

        _appIcon = (Icon)appIcon.Clone();
        Icon = _appIcon;

        _watcher = NewWatcher();
        BuildUi();
        RefreshProfiles();
        RefreshStats();

        AppLogger.Message += line =>
        {
            if (!IsDisposed && IsHandleCreated)
                BeginInvoke(() => _log.AppendText(line + Environment.NewLine));
        };

        Shown += async (_, _) =>
        {
            _watcher.Start();
            if (_config.CheckForUpdates && !_automaticUpdateCheckStarted)
            {
                _automaticUpdateCheckStarted = true;
                await CheckForUpdatesSilentlyAsync();
            }
        };

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
                RequestHideToTray?.Invoke(this, EventArgs.Empty);
        };

        FormClosing += (_, e) =>
        {
            if (!_reallyExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                RequestHideToTray?.Invoke(this, EventArgs.Empty);
            }
        };
    }

    private MultiWatcherService NewWatcher()
    {
        var watcher = new MultiWatcherService(_config);
        watcher.StatusChanged += state => BeginInvoke(() => SetStatus(state));
        watcher.Processed += record => BeginInvoke(() => OnProcessed(record));
        return watcher;
    }

    private void BuildUi()
    {
        Font = new Font("Segoe UI", 9F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            RowCount = 4,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        var head = new Panel { Dock = DockStyle.Fill };
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "NTB-Logo.png");
        var logo = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(0, 2),
            Size = new Size(92, 56)
        };
        if (File.Exists(logoPath))
        {
            using var source = Image.FromFile(logoPath);
            logo.Image = new Bitmap(source);
        }
        head.Controls.Add(logo);
        head.Controls.Add(new Label
        {
            Text = "NTB-PW PDF-Watcher",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(104, 7)
        });
        head.Controls.Add(new Label
        {
            Text = "Netz, Technik, Büro GbR | Tarek Muna 2026",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(106, 37)
        });
        root.Controls.Add(head);

        var statusPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(2, 4, 2, 2)
        };
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        _status.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        _status.Anchor = AnchorStyles.Left;
        statusPanel.Controls.Add(_status, 0, 0);
        var lastPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        lastPanel.Controls.Add(new Label
        {
            Text = "Letzte PDF:",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Margin = new Padding(0, 5, 6, 0)
        });
        _last.Margin = new Padding(0, 5, 0, 0);
        _last.AutoEllipsis = true;
        _last.MaximumSize = new Size(290, 22);
        lastPanel.Controls.Add(_last);
        statusPanel.Controls.Add(lastPanel, 1, 0);
        root.Controls.Add(statusPanel);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var dashboard = new TabPage("Übersicht") { Padding = new Padding(6) };
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 275,
            FixedPanel = FixedPanel.Panel1
        };
        split.Panel1.Controls.Add(_profiles);
        split.Panel1.Controls.Add(new Label
        {
            Text = "Scanner / Profile",
            Dock = DockStyle.Top,
            Height = 27,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        });
        split.Panel2.Controls.Add(_log);
        split.Panel2.Controls.Add(new Label
        {
            Text = "Aktuelles Protokoll",
            Dock = DockStyle.Top,
            Height = 27,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        });
        dashboard.Controls.Add(split);

        var history = new TabPage("Historie") { Padding = new Padding(6) };
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            DataSource = _history.OrderByDescending(x => x.Timestamp).ToList(),
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            AllowUserToAddRows = false
        };
        history.Controls.Add(grid);
        tabs.TabPages.Add(dashboard);
        tabs.TabPages.Add(history);
        root.Controls.Add(tabs);

        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            Padding = new Padding(0, 5, 0, 0)
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));

        AddCompactButton(bar, "Start", () => _watcher.Start(), 0);
        AddCompactButton(bar, "Stop", () => _watcher.Stop(), 1);
        AddCompactButton(bar, "Profile", EditProfiles, 2);

        var more = new Button { Text = "Mehr ▾", Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 0) };
        var menu = new ContextMenuStrip();
        menu.Items.Add("Statistik", null, (_, _) => new StatsForm(_history).ShowDialog(this));
        menu.Items.Add("PDF-Vorschau", null, (_, _) => PreviewLast());
        menu.Items.Add("Test-PDF erstellen", null, (_, _) => CreateTestPdf());
        menu.Items.Add("Protokoll öffnen", null, (_, _) => new LogForm().ShowDialog(this));
        menu.Items.Add("Nach Updates suchen", null, (_, _) => OpenUpdateDialog());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => ExitApp());
        more.Click += (_, _) => menu.Show(more, new Point(0, more.Height));
        bar.Controls.Add(more, 4, 0);

        AddCompactButton(bar, "⚙ Einstellungen", OpenSettings, 5);
        root.Controls.Add(bar);
    }

    private static void AddCompactButton(TableLayoutPanel parent, string text, Action action, int column)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 0, 4, 0)
        };
        button.Click += (_, _) => action();
        parent.Controls.Add(button, column, 0);
    }

    private void SetStatus(string state)
    {
        _status.Text = state == "active" ? "● Überwachung aktiv" : state == "disconnected" ? "● Keine Verbindung zu Netzwerk/RDP" : "● Überwachung gestoppt";
        _status.ForeColor = state == "active" ? Color.ForestGreen : state == "disconnected" ? Color.DarkOrange : Color.Firebrick;
    }

    private void OnProcessed(ProcessingRecord record)
    {
        _history.Add(record);
        ConfigStore.SaveHistory(_history);
        _last.Text = Path.GetFileName(record.SourceFile);
        if (_config.Profiles.FirstOrDefault(p => p.Name == record.Profile)?.DesktopNotification == true)
            AppLogger.Write($"Desktop-Hinweis: PDF verarbeitet: {Path.GetFileName(record.SourceFile)}");
        RefreshStats();
    }

    private void RefreshProfiles()
    {
        _profiles.Items.Clear();
        foreach (var profile in _config.Profiles)
            _profiles.Items.Add($"{(profile.Enabled ? "●" : "○")} {profile.Name}  |  {profile.Folder}");
    }

    private void RefreshStats()
    {
        Text = $"NTB-PW PDF-Watcher Professional 4.2.0 – Heute: {_history.Count(x => x.Timestamp >= DateTime.Today)}";
    }

    private void EditProfiles()
    {
        var copy = _config.Profiles.Select(p => new WatchProfile
        {
            Id = p.Id,
            Name = p.Name,
            Folder = p.Folder,
            Enabled = p.Enabled,
            ScanIntervalSeconds = p.ScanIntervalSeconds,
            FileReadyTimeoutSeconds = p.FileReadyTimeoutSeconds,
            OpenPdf = p.OpenPdf,
            DesktopNotification = p.DesktopNotification,
            EmailNotification = p.EmailNotification
        }).ToList();
        using var form = new ProfilesForm(copy);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _config.Profiles = copy;
            SaveAndRestart();
        }
    }

    private void PreviewLast()
    {
        var path = _history.LastOrDefault()?.SourceFile;
        if (path is null || !File.Exists(path))
        {
            MessageBox.Show("Noch keine PDF verfügbar.");
            return;
        }
        new PreviewForm(path).ShowDialog(this);
    }

    private void CreateTestPdf()
    {
        var profile = _config.Profiles.FirstOrDefault(x => x.Enabled);
        if (profile is null || !Directory.Exists(profile.Folder))
        {
            MessageBox.Show("Kein erreichbares Profil vorhanden.");
            return;
        }
        var file = Path.Combine(profile.Folder, $"NTB-Test-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        File.WriteAllBytes(file, Convert.FromBase64String("JVBERi0xLjQKMSAwIG9iajw8L1R5cGUvQ2F0YWxvZy9QYWdlcyAyIDAgUj4+ZW5kb2JqCjIgMCBvYmo8PC9UeXBlL1BhZ2VzL0tpZHNbMyAwIFJdL0NvdW50IDE+PmVuZG9iagozIDAgb2JqPDwvVHlwZS9QYWdlL1BhcmVudCAyIDAgUi9NZWRpYUJveFswIDAgNTk1IDg0Ml0+PmVuZG9iagp4cmVmCjAgNAowMDAwMDAwMDAwIDY1NTM1IGYgCjAwMDAwMDAwMDkgMDAwMDAgbiAKMDAwMDAwMDA1OCAwMDAwMCBuIAowMDAwMDAwMTE1IDAwMDAwIG4gCnRyYWlsZXI8PC9TaXplIDQvUm9vdCAxIDAgUj4+CnN0YXJ0eHJlZgoxODUKJSVFT0Y="));
    }

    public void ShowUpdateDialog() => OpenUpdateDialog();

    private void OpenUpdateDialog()
    {
        using var form = new UpdateForm(_config.GitHubRepository, _appIcon, ExitApp);
        form.ShowDialog(this);
    }

    private async Task CheckForUpdatesSilentlyAsync()
    {
        try
        {
            var release = await UpdateService.GetLatestReleaseAsync(_config.GitHubRepository);
            if (release is null || !UpdateService.IsNewer(release)) return;

            if (MessageBox.Show(
                    this,
                    $"Eine neue Version ({release.TagName}) ist verfügbar. Jetzt anzeigen?",
                    "NTB-PW Update",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information) == DialogResult.Yes)
            {
                OpenUpdateDialog();
            }
        }
        catch (Exception ex)
        {
            AppLogger.Write("Automatische Update-Prüfung fehlgeschlagen: " + ex.Message);
        }
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_config, _appIcon);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        _config = form.Result;
        SaveAndRestart();
    }

    private void SaveAndRestart()
    {
        ConfigStore.Save(_config);
        _watcher.UpdateConfig(_config);
        RefreshProfiles();
    }

    public void StartMonitoring() => _watcher.Start();

    public void StopMonitoring() => _watcher.Stop();

    public void ShowLogViewer() => new LogForm().ShowDialog(this);

    public void Shutdown()
    {
        _reallyExit = true;
        _watcher.Dispose();
        _appIcon.Dispose();
    }

    private void ExitApp() => RequestExit?.Invoke(this, EventArgs.Empty);
}
