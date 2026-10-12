using System.Text.Json;

namespace NTBPW.PdfWatcher;

// Allgemeine Einstellungen. Scanner-Profile werden ausschliesslich im Bereich "Scanner" (ProfilesForm) bearbeitet.
internal sealed class SettingsForm : Form
{
    private const string DefaultRepository = "tarek-muna/ntb-pw-pdf-watcher";

    private readonly AppConfig _working;

    private readonly CheckBox _autoStart = new() { Text = "Mit Windows starten", AutoSize = true };
    private readonly CheckBox _startMinimized = new() { Text = "Beim Autostart minimiert starten", AutoSize = true };

    private readonly CheckBox _enableEmail = new() { Text = "E-Mail-Benachrichtigungen aktivieren", AutoSize = true };
    private readonly TextBox _smtpHost = new() { Width = 300 };
    private readonly NumericUpDown _smtpPort = new() { Minimum = 1, Maximum = 65535, Width = 100 };
    private readonly TextBox _smtpUser = new() { Width = 300 };
    private readonly TextBox _smtpPassword = new() { Width = 300, UseSystemPasswordChar = true };
    private readonly TextBox _emailFrom = new() { Width = 300 };
    private readonly TextBox _emailTo = new() { Width = 300 };

    private readonly CheckBox _enableCloud = new() { Text = "Jede neue PDF zusätzlich in einen Ordner kopieren", AutoSize = true };
    private readonly TextBox _cloudFolder = new() { Width = 430 };

    private readonly CheckBox _checkUpdates = new() { Text = "Automatisch nach Updates suchen", AutoSize = true };
    private readonly TextBox _updateRepository = new() { Width = 430 };

    public AppConfig Result => _working;

    public SettingsForm(AppConfig config, Icon appIcon)
    {
        _working = Clone(config);
        Text = "NTB-PW – Einstellungen";
        Icon = (Icon)appIcon.Clone();
        StartPosition = FormStartPosition.CenterParent;
        Width = 720;
        Height = 520;
        MinimumSize = new Size(680, 480);

        BuildUi();
        LoadValues();
    }

    private static AppConfig Clone(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        Controls.Add(root);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildGeneralPage());
        tabs.TabPages.Add(BuildNotificationsPage());
        tabs.TabPages.Add(BuildCopyPage());
        tabs.TabPages.Add(BuildAdvancedPage());
        root.Controls.Add(tabs, 0, 0);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 6, 0, 0) };
        var cancel = new Button { Text = "Abbrechen", Width = 110, Height = 36, DialogResult = DialogResult.Cancel };
        var save = new Button { Text = "Speichern", Width = 110, Height = 36 };
        save.Click += (_, _) => SaveAndClose();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);
        root.Controls.Add(buttons, 0, 1);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private TabPage BuildGeneralPage()
    {
        var page = NewPage("Allgemein");
        var panel = NewSettingsPanel();
        AddRow(panel, "Autostart", _autoStart);
        AddRow(panel, "Startverhalten", _startMinimized);
        AddRow(panel, "Scanner-Profile", new Label
        {
            Text = "Überwachungsordner und Profile werden in der Seitenleiste unter „Scanner“ verwaltet.",
            AutoSize = true,
            MaximumSize = new Size(430, 0),
            ForeColor = Color.FromArgb(103, 116, 133)
        });
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildNotificationsPage()
    {
        var page = NewPage("E-Mail");
        var panel = NewSettingsPanel();
        AddRow(panel, "Aktivierung", _enableEmail);
        AddRow(panel, "SMTP-Server", _smtpHost);
        AddRow(panel, "SMTP-Port", _smtpPort);
        AddRow(panel, "Benutzer", _smtpUser);
        AddRow(panel, "Passwort", _smtpPassword);
        AddRow(panel, "Absender", _emailFrom);
        AddRow(panel, "Empfänger", _emailTo);
        AddRow(panel, "Hinweis", new Label
        {
            Text = "Das Passwort wird verschlüsselt für den aktuellen Windows-Benutzer gespeichert.",
            AutoSize = true,
            MaximumSize = new Size(430, 0),
            ForeColor = Color.FromArgb(103, 116, 133)
        });
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildCopyPage()
    {
        var page = NewPage("Kopie");
        var panel = NewSettingsPanel();
        AddRow(panel, "Kopie", _enableCloud);
        var cloudPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        cloudPanel.Controls.Add(_cloudFolder);
        var cloudBrowse = new Button { Text = "…", Width = 42, Height = 28 };
        cloudBrowse.Click += (_, _) => BrowseFolder(_cloudFolder);
        cloudPanel.Controls.Add(cloudBrowse);
        AddRow(panel, "Zielordner", cloudPanel);
        AddRow(panel, "Hinweis", new Label
        {
            Text = "Geeignet z. B. für einen OneDrive-, SharePoint- oder Netzwerkordner.",
            AutoSize = true,
            MaximumSize = new Size(430, 0),
            ForeColor = Color.FromArgb(103, 116, 133)
        });
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildAdvancedPage()
    {
        var page = NewPage("Erweitert");
        var panel = NewSettingsPanel();
        AddRow(panel, "Updates", _checkUpdates);
        AddRow(panel, "GitHub-Repository", _updateRepository);

        var actions = new FlowLayoutPanel { AutoSize = true };
        var openData = new Button { Text = "Datenordner öffnen", Width = 150, Height = 34 };
        openData.Click += (_, _) =>
        {
            Directory.CreateDirectory(AppConfig.DataDirectory);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppConfig.DataDirectory) { UseShellExecute = true });
        };
        var reset = new Button { Text = "Standardwerte", Width = 130, Height = 34 };
        reset.Click += (_, _) => ResetDefaults();
        actions.Controls.Add(openData);
        actions.Controls.Add(reset);
        AddRow(panel, "Werkzeuge", actions);
        page.Controls.Add(panel);
        return page;
    }

    private static TabPage NewPage(string title) => new(title) { Padding = new Padding(8) };

    private static TableLayoutPanel NewSettingsPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(9) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return panel;
    }

    private static void AddRow(TableLayoutPanel panel, string label, Control control)
    {
        var row = panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Margin = new Padding(3, 7, 10, 7)
        }, 0, row);
        control.Margin = new Padding(3, 3, 3, 3);
        panel.Controls.Add(control, 1, row);
    }

    private void LoadValues()
    {
        _autoStart.Checked = _working.AutoStart;
        _startMinimized.Checked = _working.StartMinimized;
        _enableEmail.Checked = _working.EnableEmail;
        _smtpHost.Text = _working.SmtpHost;
        _smtpPort.Value = Math.Clamp(_working.SmtpPort, 1, 65535);
        _smtpUser.Text = _working.SmtpUser;
        _smtpPassword.Text = _working.SmtpPassword;
        _emailFrom.Text = _working.EmailFrom;
        _emailTo.Text = _working.EmailTo;
        _enableCloud.Checked = _working.EnableCloudSync;
        _cloudFolder.Text = _working.CloudSyncFolder;
        _checkUpdates.Checked = _working.CheckForUpdates;
        _updateRepository.Text = _working.GitHubRepository;
    }

    private void SaveAndClose()
    {
        if (_enableCloud.Checked && string.IsNullOrWhiteSpace(_cloudFolder.Text))
        {
            MessageBox.Show(this, "Bitte einen Zielordner für die Kopie angeben.", "Einstellungen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_enableEmail.Checked && (string.IsNullOrWhiteSpace(_smtpHost.Text) || string.IsNullOrWhiteSpace(_emailFrom.Text) || string.IsNullOrWhiteSpace(_emailTo.Text)))
        {
            MessageBox.Show(this, "Für E-Mail-Benachrichtigungen werden SMTP-Server, Absender und Empfänger benötigt.", "Einstellungen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _working.AutoStart = _autoStart.Checked;
        _working.StartMinimized = _startMinimized.Checked;
        _working.EnableEmail = _enableEmail.Checked;
        _working.SmtpHost = _smtpHost.Text.Trim();
        _working.SmtpPort = (int)_smtpPort.Value;
        _working.SmtpUser = _smtpUser.Text.Trim();
        _working.SmtpPassword = _smtpPassword.Text;
        _working.EmailFrom = _emailFrom.Text.Trim();
        _working.EmailTo = _emailTo.Text.Trim();
        _working.EnableCloudSync = _enableCloud.Checked;
        _working.CloudSyncFolder = _cloudFolder.Text.Trim();
        _working.CheckForUpdates = _checkUpdates.Checked;
        _working.GitHubRepository = UpdateService.NormalizeRepository(_updateRepository.Text);
        if (string.IsNullOrWhiteSpace(_working.GitHubRepository))
            _working.GitHubRepository = DefaultRepository;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ResetDefaults()
    {
        if (MessageBox.Show(this, "Alle Einstellungen in diesem Dialog auf Standardwerte zurücksetzen?\nScanner-Profile und Ablageregeln bleiben erhalten.", "Standardwerte", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var defaults = new AppConfig();
        _working.AutoStart = defaults.AutoStart;
        _working.StartMinimized = defaults.StartMinimized;
        _working.EnableEmail = defaults.EnableEmail;
        _working.SmtpHost = defaults.SmtpHost;
        _working.SmtpPort = defaults.SmtpPort;
        _working.SmtpUser = defaults.SmtpUser;
        _working.SmtpPassword = "";
        _working.EmailFrom = defaults.EmailFrom;
        _working.EmailTo = defaults.EmailTo;
        _working.EnableCloudSync = defaults.EnableCloudSync;
        _working.CloudSyncFolder = defaults.CloudSyncFolder;
        _working.CheckForUpdates = defaults.CheckForUpdates;
        _working.GitHubRepository = defaults.GitHubRepository;
        LoadValues();
    }

    private static void BrowseFolder(TextBox target)
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = target.Text, ShowNewFolderButton = true };
        if (dialog.ShowDialog() == DialogResult.OK) target.Text = dialog.SelectedPath;
    }
}
