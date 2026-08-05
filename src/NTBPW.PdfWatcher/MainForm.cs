using System.Diagnostics;

namespace NTBPW.PdfWatcher;

internal sealed class MainForm : Form
{
    private AppConfig _config = ConfigStore.Load();
    private readonly List<ProcessingRecord> _history = ConfigStore.LoadHistory();
    private MultiWatcherService _watcher;
    private readonly Label _status = new() { AutoSize = true };
    private readonly Label _last = new() { AutoSize = true, Text = "–" };
    private readonly Label _today = new() { AutoSize = true, Text = "0" };
    private readonly Label _profileCount = new() { AutoSize = true, Text = "0" };
    private readonly ListBox _profiles = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
    private readonly TextBox _log = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None
    };
    private readonly Icon _appIcon;
    private bool _reallyExit;
    private bool _automaticUpdateCheckStarted;

    public event EventHandler? RequestHideToTray;
    public event EventHandler? RequestExit;

    public MainForm(Icon appIcon)
    {
        Text = AppIdentity.WindowTitle;
        Width = 860;
        Height = 560;
        MinimumSize = new Size(780, 500);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 247, 250);

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

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 188));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        shell.Controls.Add(BuildSidebar(), 0, 0);
        shell.Controls.Add(BuildDashboard(), 1, 0);
    }

    private Control BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(25, 43, 67),
            Padding = new Padding(14, 18, 14, 14)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 10,
            BackColor = sidebar.BackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        for (var i = 1; i <= 7; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        sidebar.Controls.Add(layout);

        var brand = new Panel { Dock = DockStyle.Fill };
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "NTB-Logo.png");
        var logo = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(0, 0),
            Size = new Size(54, 54)
        };
        if (File.Exists(logoPath))
        {
            using var source = Image.FromFile(logoPath);
            logo.Image = new Bitmap(source);
        }
        brand.Controls.Add(logo);
        brand.Controls.Add(new Label
        {
            Text = "NTB-PW",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(62, 5)
        });
        brand.Controls.Add(new Label
        {
            Text = "PDF-Watcher",
            ForeColor = Color.FromArgb(174, 192, 214),
            AutoSize = true,
            Location = new Point(64, 34)
        });
        layout.Controls.Add(brand, 0, 0);

        layout.Controls.Add(CreateNavButton("⌂  Dashboard", null, true), 0, 1);
        layout.Controls.Add(CreateNavButton("▣  Scanner", EditProfiles), 0, 2);
        layout.Controls.Add(CreateNavButton("▤  Historie", ShowHistory), 0, 3);
        layout.Controls.Add(CreateNavButton("▥  Statistik", () => new StatsForm(_history).ShowDialog(this)), 0, 4);
        layout.Controls.Add(CreateNavButton("⚙  Einstellungen", OpenSettings), 0, 5);
        layout.Controls.Add(CreateNavButton("≡  Protokoll", ShowLogViewer), 0, 6);
        layout.Controls.Add(CreateNavButton("↻  Updates", OpenUpdateDialog), 0, 7);

        var footer = new Label
        {
            Text = $"{AppIdentity.DisplayVersion}\n{AppIdentity.Copyright}",
            ForeColor = Color.FromArgb(145, 164, 188),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Font = new Font("Segoe UI", 8F)
        };
        layout.Controls.Add(footer, 0, 8);

        var exit = CreateNavButton("⏻  Beenden", ExitApp);
        exit.ForeColor = Color.FromArgb(255, 205, 205);
        layout.Controls.Add(exit, 0, 9);
        return sidebar;
    }

    private Button CreateNavButton(string text, Action? action, bool selected = false)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = selected ? Color.FromArgb(45, 76, 112) : Color.Transparent,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 0, 2)
        };
        if (action is not null)
            button.Click += (_, _) => action();
        return button;
    }

    private Control BuildDashboard()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 102));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var heading = new Panel { Dock = DockStyle.Fill };
        heading.Controls.Add(new Label
        {
            Text = "Dashboard",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 45, 61),
            AutoSize = true,
            Location = new Point(0, 0)
        });
        heading.Controls.Add(new Label
        {
            Text = $"{AppIdentity.Company} | Tarek Muna 2026",
            ForeColor = Color.FromArgb(103, 116, 133),
            AutoSize = true,
            Location = new Point(2, 38)
        });
        root.Controls.Add(heading, 0, 0);

        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(0, 4, 0, 12)
        };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        cards.Controls.Add(CreateStatusCard("STATUS", _status, "Überwachung wird gestartet …"), 0, 0);
        cards.Controls.Add(CreateStatusCard("HEUTE GEÖFFNET", _today, "PDF-Dateien"), 1, 0);
        cards.Controls.Add(CreateStatusCard("AKTIVE PROFILE", _profileCount, "Scanner / Ordner"), 2, 0);
        root.Controls.Add(cards, 0, 1);

        var content = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 245,
            FixedPanel = FixedPanel.Panel1,
            BackColor = BackColor
        };
        content.Panel1.Padding = new Padding(0, 0, 8, 0);
        content.Panel2.Padding = new Padding(8, 0, 0, 0);
        content.Panel1.Controls.Add(CreateSection("Scanner / Profile", _profiles));
        content.Panel2.Controls.Add(CreateSection("Live-Protokoll", _log));
        root.Controls.Add(content, 0, 2);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 7, 0, 0)
        };
        actions.Controls.Add(CreateActionButton("Einstellungen", OpenSettings, true));
        actions.Controls.Add(CreateActionButton("Stop", () => _watcher.Stop()));
        actions.Controls.Add(CreateActionButton("Start", () => _watcher.Start()));

        var lastPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0)
        };
        lastPanel.Controls.Add(new Label { Text = "Letzte PDF:", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
        _last.MaximumSize = new Size(260, 22);
        _last.AutoEllipsis = true;
        _last.Margin = new Padding(6, 0, 0, 0);
        lastPanel.Controls.Add(_last);
        actions.Controls.Add(lastPanel);
        root.Controls.Add(actions, 0, 3);
        return root;
    }

    private Panel CreateStatusCard(string caption, Label value, string subtitle)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(14, 10, 14, 10)
        };
        card.Controls.Add(new Label
        {
            Text = caption,
            ForeColor = Color.FromArgb(112, 126, 144),
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(14, 10)
        });
        value.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        value.ForeColor = Color.FromArgb(31, 45, 61);
        value.Location = new Point(14, 32);
        value.MaximumSize = new Size(260, 30);
        value.AutoEllipsis = true;
        card.Controls.Add(value);
        card.Controls.Add(new Label
        {
            Text = subtitle,
            ForeColor = Color.FromArgb(130, 142, 158),
            AutoSize = true,
            Location = new Point(15, 67)
        });
        return card;
    }

    private static Panel CreateSection(string title, Control content)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(14, 42, 14, 14)
        };
        panel.Controls.Add(content);
        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 32,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 58, 74),
            Padding = new Padding(0, 7, 0, 0)
        });
        return panel;
    }

    private static Button CreateActionButton(string text, Action action, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Width = primary ? 120 : 82,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(36, 111, 187) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(45, 58, 74),
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = primary ? button.BackColor : Color.FromArgb(200, 208, 218);
        button.Click += (_, _) => action();
        return button;
    }

    private void SetStatus(string state)
    {
        _status.Text = state == "active" ? "● Aktiv" : state == "disconnected" ? "● Getrennt" : "● Gestoppt";
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
            _profiles.Items.Add($"{(profile.Enabled ? "●" : "○")} {profile.Name}\r\n   {profile.Folder}");
        _profileCount.Text = _config.Profiles.Count(p => p.Enabled).ToString();
    }

    private void RefreshStats()
    {
        var today = _history.Count(x => x.Timestamp >= DateTime.Today);
        _today.Text = today.ToString();
        Text = $"{AppIdentity.WindowTitle} – Heute: {today}";
    }

    private void ShowHistory()
    {
        using var form = new Form
        {
            Text = "PDF-Historie",
            Width = 820,
            Height = 480,
            StartPosition = FormStartPosition.CenterParent,
            Icon = _appIcon
        };
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
        form.Controls.Add(grid);
        form.ShowDialog(this);
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
