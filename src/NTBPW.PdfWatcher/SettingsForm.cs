using System.Text.Json;

namespace NTBPW.PdfWatcher;

internal sealed class SettingsForm : Form
{
    private readonly AppConfig _working;
    private readonly ComboBox _profileSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly TextBox _profileName = new() { Width = 300 };
    private readonly TextBox _folder = new() { Width = 430 };
    private readonly CheckBox _profileEnabled = new() { Text = "Profil aktiviert", AutoSize = true };
    private readonly NumericUpDown _interval = new() { Minimum = 1, Maximum = 3600, Width = 100 };
    private readonly NumericUpDown _timeout = new() { Minimum = 1, Maximum = 3600, Width = 100 };
    private readonly CheckBox _openPdf = new() { Text = "PDF automatisch öffnen", AutoSize = true };
    private readonly CheckBox _desktopNotification = new() { Text = "Windows-Benachrichtigung anzeigen", AutoSize = true };
    private readonly CheckBox _emailNotification = new() { Text = "E-Mail für dieses Profil senden", AutoSize = true };

    private readonly CheckBox _autoStart = new() { Text = "Mit Windows starten", AutoSize = true };
    private readonly CheckBox _startMinimized = new() { Text = "Beim Autostart minimiert starten", AutoSize = true };
    private readonly ComboBox _theme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };

    private readonly CheckBox _enableOcr = new() { Text = "OCR aktivieren", AutoSize = true };
    private readonly TextBox _tesseractExe = new() { Width = 430 };
    private readonly TextBox _tesseractLanguage = new() { Width = 180 };
    private readonly CheckBox _enableAi = new() { Text = "KI-Klassifizierung aktivieren", AutoSize = true };
    private readonly TextBox _aiEndpoint = new() { Width = 430 };
    private readonly TextBox _aiApiKey = new() { Width = 430, UseSystemPasswordChar = true };

    private readonly CheckBox _enableEmail = new() { Text = "E-Mail-Benachrichtigungen aktivieren", AutoSize = true };
    private readonly TextBox _smtpHost = new() { Width = 300 };
    private readonly NumericUpDown _smtpPort = new() { Minimum = 1, Maximum = 65535, Width = 100 };
    private readonly TextBox _smtpUser = new() { Width = 300 };
    private readonly TextBox _smtpPassword = new() { Width = 300, UseSystemPasswordChar = true };
    private readonly TextBox _emailFrom = new() { Width = 300 };
    private readonly TextBox _emailTo = new() { Width = 300 };

    private readonly CheckBox _enableCloud = new() { Text = "Cloud-Synchronisation aktivieren", AutoSize = true };
    private readonly TextBox _cloudFolder = new() { Width = 430 };
    private readonly CheckBox _checkUpdates = new() { Text = "Automatisch nach Updates suchen", AutoSize = true };
    private readonly TextBox _updateRepository = new() { Width = 430 };

    private int _loadedProfileIndex = -1;

    public AppConfig Result => _working;

    public SettingsForm(AppConfig config, Icon appIcon)
    {
        _working = Clone(config);
        Text = "NTB-PW – Einstellungen";
        Icon = (Icon)appIcon.Clone();
        StartPosition = FormStartPosition.CenterParent;
        Width = 720;
        Height = 560;
        MinimumSize = new Size(680, 520);

        BuildUi();
        LoadGeneralValues();
        LoadProfileList();
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
        tabs.TabPages.Add(BuildMonitoringPage());
        tabs.TabPages.Add(BuildNotificationsPage());
        tabs.TabPages.Add(BuildModulesPage());
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
        _theme.Items.AddRange(["System", "Hell", "Dunkel"]);
        AddRow(panel, "Design", _theme);
        _language.Items.AddRange(["Deutsch", "English"]);
        AddRow(panel, "Sprache", _language);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildMonitoringPage()
    {
        var page = NewPage("Überwachung");
        var outer = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(10) };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var selectorBar = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(4, 8, 4, 4) };
        selectorBar.Controls.Add(new Label { Text = "Scanner-Profil:", AutoSize = true, Padding = new Padding(0, 7, 10, 0), Font = new Font("Segoe UI", 10, FontStyle.Bold) });
        selectorBar.Controls.Add(_profileSelector);
        var profilesButton = new Button { Text = "Profile verwalten…", Width = 150, Height = 32 };
        profilesButton.Click += (_, _) => EditProfiles();
        selectorBar.Controls.Add(profilesButton);
        outer.Controls.Add(selectorBar, 0, 0);

        var panel = NewSettingsPanel();
        AddRow(panel, "Name", _profileName);
        var folderPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        folderPanel.Controls.Add(_folder);
        var browse = new Button { Text = "…", Width = 42, Height = 28 };
        browse.Click += (_, _) => BrowseFolder(_folder);
        folderPanel.Controls.Add(browse);
        AddRow(panel, "Überwachungsordner", folderPanel);
        AddRow(panel, "Status", _profileEnabled);
        AddRow(panel, "Prüfintervall (Sek.)", _interval);
        AddRow(panel, "Datei-Timeout (Sek.)", _timeout);
        AddRow(panel, "PDF", _openPdf);
        AddRow(panel, "Desktop", _desktopNotification);
        AddRow(panel, "E-Mail", _emailNotification);
        outer.Controls.Add(panel, 0, 1);
        page.Controls.Add(outer);

        _profileSelector.SelectedIndexChanged += (_, _) => SwitchProfile();
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
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildModulesPage()
    {
        var page = NewPage("OCR / KI / Cloud");
        var panel = NewSettingsPanel();
        AddRow(panel, "OCR", _enableOcr);
        var tessPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        tessPanel.Controls.Add(_tesseractExe);
        var tessBrowse = new Button { Text = "…", Width = 42, Height = 28 };
        tessBrowse.Click += (_, _) => BrowseFile(_tesseractExe, "Programme (*.exe)|*.exe|Alle Dateien (*.*)|*.*");
        tessPanel.Controls.Add(tessBrowse);
        AddRow(panel, "Tesseract.exe", tessPanel);
        AddRow(panel, "OCR-Sprachen", _tesseractLanguage);
        AddRow(panel, "KI", _enableAi);
        AddRow(panel, "KI-Endpunkt", _aiEndpoint);
        AddRow(panel, "KI-API-Schlüssel", _aiApiKey);
        AddRow(panel, "Cloud", _enableCloud);
        var cloudPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        cloudPanel.Controls.Add(_cloudFolder);
        var cloudBrowse = new Button { Text = "…", Width = 42, Height = 28 };
        cloudBrowse.Click += (_, _) => BrowseFolder(_cloudFolder);
        cloudPanel.Controls.Add(cloudBrowse);
        AddRow(panel, "Cloud-Ordner", cloudPanel);
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

    private void LoadGeneralValues()
    {
        _autoStart.Checked = _working.AutoStart;
        _startMinimized.Checked = _working.StartMinimized;
        _theme.SelectedItem = _working.Theme switch { "Light" => "Hell", "Dark" => "Dunkel", _ => "System" };
        if (_theme.SelectedIndex < 0) _theme.SelectedIndex = 0;
        _language.SelectedItem = _working.Language.Equals("en", StringComparison.OrdinalIgnoreCase) ? "English" : "Deutsch";
        if (_language.SelectedIndex < 0) _language.SelectedIndex = 0;

        _enableOcr.Checked = _working.EnableOcr;
        _tesseractExe.Text = _working.TesseractExe;
        _tesseractLanguage.Text = _working.TesseractLanguage;
        _enableAi.Checked = _working.EnableAiClassification;
        _aiEndpoint.Text = _working.AiEndpoint;
        _aiApiKey.Text = _working.AiApiKey;
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

    private void LoadProfileList()
    {
        _profileSelector.Items.Clear();
        foreach (var profile in _working.Profiles) _profileSelector.Items.Add(profile.Name);
        if (_profileSelector.Items.Count > 0) _profileSelector.SelectedIndex = 0;
    }

    private void SwitchProfile()
    {
        SaveCurrentProfile();
        _loadedProfileIndex = _profileSelector.SelectedIndex;
        if (_loadedProfileIndex < 0 || _loadedProfileIndex >= _working.Profiles.Count) return;
        var profile = _working.Profiles[_loadedProfileIndex];
        _profileName.Text = profile.Name;
        _folder.Text = profile.Folder;
        _profileEnabled.Checked = profile.Enabled;
        _interval.Value = Math.Clamp(profile.ScanIntervalSeconds, 1, 3600);
        _timeout.Value = Math.Clamp(profile.FileReadyTimeoutSeconds, 1, 3600);
        _openPdf.Checked = profile.OpenPdf;
        _desktopNotification.Checked = profile.DesktopNotification;
        _emailNotification.Checked = profile.EmailNotification;
    }

    private void SaveCurrentProfile()
    {
        if (_loadedProfileIndex < 0 || _loadedProfileIndex >= _working.Profiles.Count) return;
        var profile = _working.Profiles[_loadedProfileIndex];
        profile.Name = string.IsNullOrWhiteSpace(_profileName.Text) ? $"Scanner {_loadedProfileIndex + 1}" : _profileName.Text.Trim();
        profile.Folder = _folder.Text.Trim();
        profile.Enabled = _profileEnabled.Checked;
        profile.ScanIntervalSeconds = (int)_interval.Value;
        profile.FileReadyTimeoutSeconds = (int)_timeout.Value;
        profile.OpenPdf = _openPdf.Checked;
        profile.DesktopNotification = _desktopNotification.Checked;
        profile.EmailNotification = _emailNotification.Checked;
    }

    private void EditProfiles()
    {
        SaveCurrentProfile();
        using var form = new ProfilesForm(_working.Profiles);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _loadedProfileIndex = -1;
            LoadProfileList();
        }
    }

    private void SaveAndClose()
    {
        SaveCurrentProfile();
        if (_working.Profiles.Count == 0)
        {
            MessageBox.Show(this, "Mindestens ein Scanner-Profil ist erforderlich.", "Einstellungen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_working.Profiles.Any(p => p.Enabled && string.IsNullOrWhiteSpace(p.Folder)))
        {
            MessageBox.Show(this, "Für jedes aktivierte Profil muss ein Überwachungsordner angegeben sein.", "Einstellungen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _working.AutoStart = _autoStart.Checked;
        _working.StartMinimized = _startMinimized.Checked;
        _working.Theme = _theme.SelectedItem?.ToString() switch { "Hell" => "Light", "Dunkel" => "Dark", _ => "System" };
        _working.Language = _language.SelectedItem?.ToString() == "English" ? "en" : "de";
        _working.EnableOcr = _enableOcr.Checked;
        _working.TesseractExe = _tesseractExe.Text.Trim();
        _working.TesseractLanguage = _tesseractLanguage.Text.Trim();
        _working.EnableAiClassification = _enableAi.Checked;
        _working.AiEndpoint = _aiEndpoint.Text.Trim();
        _working.AiApiKey = _aiApiKey.Text;
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
            _working.GitHubRepository = "tarek-muna/ntb-pw-pdf-watcher";

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ResetDefaults()
    {
        if (MessageBox.Show(this, "Alle Einstellungen im Dialog auf Standardwerte zurücksetzen?", "Standardwerte", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var defaults = new AppConfig();
        var json = JsonSerializer.Serialize(defaults);
        var reset = JsonSerializer.Deserialize<AppConfig>(json) ?? defaults;
        _working.Profiles = reset.Profiles;
        _working.Rules = reset.Rules;
        _working.AutoStart = reset.AutoStart;
        _working.StartMinimized = reset.StartMinimized;
        _working.Theme = reset.Theme;
        _working.Language = reset.Language;
        _working.EnableOcr = reset.EnableOcr;
        _working.TesseractExe = reset.TesseractExe;
        _working.TesseractLanguage = reset.TesseractLanguage;
        _working.EnableAiClassification = reset.EnableAiClassification;
        _working.AiEndpoint = reset.AiEndpoint;
        _working.AiApiKey = reset.AiApiKey;
        _working.EnableEmail = reset.EnableEmail;
        _working.SmtpHost = reset.SmtpHost;
        _working.SmtpPort = reset.SmtpPort;
        _working.SmtpUser = reset.SmtpUser;
        _working.SmtpPassword = reset.SmtpPassword;
        _working.EmailFrom = reset.EmailFrom;
        _working.EmailTo = reset.EmailTo;
        _working.EnableCloudSync = reset.EnableCloudSync;
        _working.CloudSyncFolder = reset.CloudSyncFolder;
        _working.CheckForUpdates = reset.CheckForUpdates;
        _working.GitHubRepository = reset.GitHubRepository;
        _working.UpdateManifestUrl = reset.UpdateManifestUrl;
        LoadGeneralValues();
        _loadedProfileIndex = -1;
        LoadProfileList();
    }

    private static void BrowseFolder(TextBox target)
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = target.Text, ShowNewFolderButton = true };
        if (dialog.ShowDialog() == DialogResult.OK) target.Text = dialog.SelectedPath;
    }

    private static void BrowseFile(TextBox target, string filter)
    {
        using var dialog = new OpenFileDialog { FileName = target.Text, Filter = filter, CheckFileExists = true };
        if (dialog.ShowDialog() == DialogResult.OK) target.Text = dialog.FileName;
    }
}
