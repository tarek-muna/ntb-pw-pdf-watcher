namespace NTBPW.PdfWatcher;

internal sealed class ProfilesForm : Form
{
    private readonly List<WatchProfile> _profiles;
    private readonly ListBox _profileList = new();
    private readonly TextBox _name = new();
    private readonly TextBox _folder = new();
    private readonly CheckBox _enabled = new() { Text = "Profil aktiv" };
    private readonly NumericUpDown _interval = new() { Minimum = 1, Maximum = 300, Value = 1 };
    private readonly NumericUpDown _timeout = new() { Minimum = 1, Maximum = 300, Value = 5 };
    private readonly CheckBox _openPdf = new() { Text = "PDF automatisch öffnen" };
    private readonly CheckBox _desktopNotification = new() { Text = "Windows-Benachrichtigung anzeigen" };
    private readonly CheckBox _emailNotification = new() { Text = "E-Mail-Benachrichtigung senden" };
    private bool _loading;

    public ProfilesForm(List<WatchProfile> profiles)
    {
        _profiles = profiles;

        Text = "Scanner und Profile";
        Width = 760;
        Height = 520;
        MinimumSize = new Size(700, 470);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 9F);

        BuildUi();
        RefreshProfileList();

        if (_profiles.Count > 0)
            _profileList.SelectedIndex = 0;
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 2,
            BackColor = BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        Controls.Add(root);

        root.Controls.Add(BuildProfilePanel(), 0, 0);
        root.Controls.Add(BuildEditorPanel(), 1, 0);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0)
        };
        var save = CreateButton("Übernehmen", true, 112);
        var cancel = CreateButton("Abbrechen", false, 100);
        save.Click += (_, _) =>
        {
            SaveCurrentProfile();
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        footer.Controls.Add(save);
        footer.Controls.Add(cancel);
        root.SetColumnSpan(footer, 2);
        root.Controls.Add(footer, 0, 1);
    }

    private Control BuildProfilePanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Text = "Scanner / Profile",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 45, 61),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        _profileList.Dock = DockStyle.Fill;
        _profileList.BorderStyle = BorderStyle.None;
        _profileList.Font = new Font("Segoe UI", 9.5F);
        _profileList.SelectedIndexChanged += (_, _) => LoadSelectedProfile();
        layout.Controls.Add(_profileList, 0, 1);

        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var add = CreateButton("+ Hinzufügen", false, 0);
        var remove = CreateButton("– Entfernen", false, 0);
        add.Dock = DockStyle.Fill;
        remove.Dock = DockStyle.Fill;
        add.Click += (_, _) => AddProfile();
        remove.Click += (_, _) => RemoveProfile();
        buttons.Controls.Add(add, 0, 0);
        buttons.Controls.Add(remove, 1, 0);
        layout.Controls.Add(buttons, 0, 2);

        return panel;
    }

    private Control BuildEditorPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(12, 0, 0, 0),
            Padding = new Padding(18)
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 9
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        panel.Controls.Add(form);

        AddSectionTitle(form, "Profil-Einstellungen", 0);
        AddField(form, "Name", _name, 1);
        AddField(form, "Überwachungsordner", _folder, 2);

        var browse = CreateButton("…", false, 34);
        browse.Click += (_, _) => BrowseFolder();
        form.Controls.Add(browse, 2, 2);

        AddField(form, "Prüfintervall (Sek.)", _interval, 3);
        AddField(form, "Datei-Timeout (Sek.)", _timeout, 4);

        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };
        options.Controls.Add(_enabled);
        options.Controls.Add(_openPdf);
        options.Controls.Add(_desktopNotification);
        options.Controls.Add(_emailNotification);
        form.Controls.Add(new Label
        {
            Text = "Optionen",
            AutoSize = true,
            ForeColor = Color.FromArgb(75, 88, 104),
            Margin = new Padding(0, 10, 8, 0)
        }, 0, 5);
        form.Controls.Add(options, 1, 5);
        form.SetColumnSpan(options, 2);

        var hint = new Label
        {
            Text = "Netzwerk- und RDP-Pfade wie \\server\scan oder \\tsclient\C\Scan werden unterstützt.",
            AutoSize = true,
            MaximumSize = new Size(430, 0),
            ForeColor = Color.FromArgb(112, 126, 144),
            Padding = new Padding(0, 14, 0, 0)
        };
        form.Controls.Add(hint, 0, 6);
        form.SetColumnSpan(hint, 3);

        return panel;
    }

    private static void AddSectionTitle(TableLayoutPanel form, string text, int row)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 45, 61),
            Margin = new Padding(0, 0, 0, 16)
        };
        form.Controls.Add(label, 0, row);
        form.SetColumnSpan(label, 3);
    }

    private static void AddField(TableLayoutPanel form, string labelText, Control control, int row)
    {
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        form.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            ForeColor = Color.FromArgb(75, 88, 104),
            Margin = new Padding(0, 9, 8, 0)
        }, 0, row);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 5, 6, 5);
        form.Controls.Add(control, 1, row);
    }

    private static Button CreateButton(string text, bool primary, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(36, 111, 187) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(45, 58, 74),
            Margin = new Padding(6, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = primary ? button.BackColor : Color.FromArgb(200, 208, 218);
        return button;
    }

    private void RefreshProfileList()
    {
        var selected = _profileList.SelectedIndex;
        _profileList.Items.Clear();
        foreach (var profile in _profiles)
            _profileList.Items.Add($"{(profile.Enabled ? "●" : "○")}  {profile.Name}");

        if (_profiles.Count > 0)
            _profileList.SelectedIndex = Math.Clamp(selected, 0, _profiles.Count - 1);
    }

    private void LoadSelectedProfile()
    {
        if (_loading || _profileList.SelectedIndex < 0 || _profileList.SelectedIndex >= _profiles.Count)
            return;

        _loading = true;
        var profile = _profiles[_profileList.SelectedIndex];
        _name.Text = profile.Name;
        _folder.Text = profile.Folder;
        _enabled.Checked = profile.Enabled;
        _interval.Value = Math.Clamp(profile.ScanIntervalSeconds, (int)_interval.Minimum, (int)_interval.Maximum);
        _timeout.Value = Math.Clamp(profile.FileReadyTimeoutSeconds, (int)_timeout.Minimum, (int)_timeout.Maximum);
        _openPdf.Checked = profile.OpenPdf;
        _desktopNotification.Checked = profile.DesktopNotification;
        _emailNotification.Checked = profile.EmailNotification;
        _loading = false;
    }

    private void SaveCurrentProfile()
    {
        if (_loading || _profileList.SelectedIndex < 0 || _profileList.SelectedIndex >= _profiles.Count)
            return;

        var profile = _profiles[_profileList.SelectedIndex];
        profile.Name = string.IsNullOrWhiteSpace(_name.Text) ? "Scanner" : _name.Text.Trim();
        profile.Folder = _folder.Text.Trim();
        profile.Enabled = _enabled.Checked;
        profile.ScanIntervalSeconds = (int)_interval.Value;
        profile.FileReadyTimeoutSeconds = (int)_timeout.Value;
        profile.OpenPdf = _openPdf.Checked;
        profile.DesktopNotification = _desktopNotification.Checked;
        profile.EmailNotification = _emailNotification.Checked;
    }

    private void AddProfile()
    {
        SaveCurrentProfile();
        _profiles.Add(new WatchProfile { Name = $"Scanner {_profiles.Count + 1}" });
        RefreshProfileList();
        _profileList.SelectedIndex = _profiles.Count - 1;
    }

    private void RemoveProfile()
    {
        if (_profiles.Count <= 1)
        {
            MessageBox.Show(this, "Mindestens ein Profil muss vorhanden bleiben.", "Scanner-Profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var index = _profileList.SelectedIndex;
        if (index < 0) return;

        var name = _profiles[index].Name;
        if (MessageBox.Show(this, $"Profil '{name}' wirklich entfernen?", "Scanner-Profile", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _profiles.RemoveAt(index);
        RefreshProfileList();
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Überwachungsordner auswählen",
            ShowNewFolderButton = true,
            SelectedPath = Directory.Exists(_folder.Text) ? _folder.Text : string.Empty
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _folder.Text = dialog.SelectedPath;
    }
}
