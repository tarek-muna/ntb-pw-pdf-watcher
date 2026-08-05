namespace NTBPW.PdfWatcher;

internal sealed class ToolsCenterForm : Form
{
    private readonly CheckedListBox _packages = new() { Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly TextBox _softwareLog = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox _driveLetter = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _uncPath = new() { PlaceholderText = @"\\SERVER\Freigabe" };
    private readonly TextBox _userName = new() { PlaceholderText = @"DOMÄNE\Benutzer (optional)" };
    private readonly TextBox _password = new() { UseSystemPasswordChar = true, PlaceholderText = "Passwort (wird nicht gespeichert)" };
    private readonly CheckBox _persistent = new() { Text = "Bei Anmeldung wiederherstellen", Checked = true, AutoSize = true };
    private readonly Label _driveStatus = new() { AutoSize = true, Text = "Bereit" };
    private readonly TextBox _driveLog = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private CancellationTokenSource? _operation;

    public ToolsCenterForm(Icon icon)
    {
        Text = "NTB Tools Center";
        Width = 900;
        Height = 620;
        MinimumSize = new Size(780, 540);
        StartPosition = FormStartPosition.CenterParent;
        Icon = icon;
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(245, 247, 250);

        foreach (var package in WingetService.DefaultPackages)
            _packages.Items.Add(package);
        _packages.DisplayMember = nameof(SoftwarePackage.Name);

        for (var letter = 'D'; letter <= 'Z'; letter++)
            _driveLetter.Items.Add(letter + ":");
        _driveLetter.SelectedItem = "Z:";

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 6) };
        tabs.TabPages.Add(BuildSoftwarePage());
        tabs.TabPages.Add(BuildNetworkDrivePage());
        Controls.Add(tabs);
    }

    private TabPage BuildSoftwarePage()
    {
        var page = new TabPage("Software Center") { BackColor = BackColor, Padding = new Padding(14) };
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        root.Controls.Add(BuildCard("Apps auswählen", _packages), 0, 0);
        root.Controls.Add(BuildCard("Installationsprotokoll", _softwareLog), 1, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
        var install = CreateButton("Ausgewählte installieren", true, 180);
        var upgrade = CreateButton("Alle Apps aktualisieren", false, 170);
        var cancel = CreateButton("Abbrechen", false, 100);
        install.Click += async (_, _) => await InstallSelectedAsync();
        upgrade.Click += async (_, _) => await UpgradeAllAsync();
        cancel.Click += (_, _) => _operation?.Cancel();
        actions.Controls.Add(install);
        actions.Controls.Add(upgrade);
        actions.Controls.Add(cancel);
        root.SetColumnSpan(actions, 2);
        root.Controls.Add(actions, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private TabPage BuildNetworkDrivePage()
    {
        var page = new TabPage("Netzlaufwerke") { BackColor = BackColor, Padding = new Padding(14) };
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 7, BackColor = Color.White, Padding = new Padding(18) };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        AddField(form, "Laufwerksbuchstabe", _driveLetter, 0);
        AddField(form, "Netzwerkpfad", _uncPath, 1);
        AddField(form, "Benutzername", _userName, 2);
        AddField(form, "Passwort", _password, 3);
        form.Controls.Add(_persistent, 1, 4);
        form.Controls.Add(_driveStatus, 1, 5);

        var test = CreateButton("Verbindung testen", false, 150);
        var connect = CreateButton("Verbinden", true, 130);
        var disconnect = CreateButton("Trennen", false, 110);
        test.Click += async (_, _) => await TestDriveAsync();
        connect.Click += async (_, _) => await ConnectDriveAsync();
        disconnect.Click += async (_, _) => await DisconnectDriveAsync();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        buttons.Controls.Add(test);
        buttons.Controls.Add(connect);
        buttons.Controls.Add(disconnect);
        form.Controls.Add(buttons, 1, 6);
        form.SetColumnSpan(buttons, 2);

        root.Controls.Add(form, 0, 0);
        root.Controls.Add(BuildCard("Protokoll", _driveLog), 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private async Task InstallSelectedAsync()
    {
        var selected = _packages.CheckedItems.Cast<SoftwarePackage>().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Bitte mindestens eine App auswählen.", "Software Center", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _operation = new CancellationTokenSource();
        _softwareLog.Clear();
        if (!await WingetService.IsAvailableAsync(_operation.Token))
        {
            AppendSoftware("Winget wurde nicht gefunden. Bitte zuerst den Microsoft App Installer installieren oder aktualisieren.");
            return;
        }

        foreach (var package in selected)
        {
            try
            {
                AppendSoftware($"> Installiere {package.Name} ({package.Id}) …");
                var result = await WingetService.InstallAsync(package.Id, _operation.Token);
                AppendSoftware(result.Success ? $"✓ {package.Name} wurde verarbeitet." : $"✗ {package.Name}: {result.StandardError}\r\n{result.StandardOutput}");
            }
            catch (OperationCanceledException)
            {
                AppendSoftware("Vorgang abgebrochen.");
                break;
            }
        }
    }

    private async Task UpgradeAllAsync()
    {
        _operation = new CancellationTokenSource();
        AppendSoftware("> Aktualisiere alle verfügbaren Apps …");
        var result = await WingetService.UpgradeAllAsync(_operation.Token);
        AppendSoftware(result.Success ? "✓ Aktualisierung abgeschlossen." : $"✗ Aktualisierung fehlgeschlagen: {result.StandardError}\r\n{result.StandardOutput}");
    }

    private async Task TestDriveAsync()
    {
        _driveStatus.Text = "Teste Verbindung …";
        var result = await NetworkDriveService.TestPathAsync(_uncPath.Text);
        _driveStatus.Text = result.Message;
        _driveStatus.ForeColor = result.Success ? Color.ForestGreen : Color.Firebrick;
        AppendDrive(result.Message);
    }

    private async Task ConnectDriveAsync()
    {
        try
        {
            _driveStatus.Text = "Verbinde …";
            var result = await NetworkDriveService.ConnectAsync(
                _driveLetter.Text,
                _uncPath.Text,
                _persistent.Checked,
                string.IsNullOrWhiteSpace(_userName.Text) ? null : _userName.Text,
                string.IsNullOrWhiteSpace(_password.Text) ? null : _password.Text);
            _password.Clear();
            _driveStatus.Text = result.Success ? "Netzlaufwerk verbunden." : "Verbindung fehlgeschlagen.";
            _driveStatus.ForeColor = result.Success ? Color.ForestGreen : Color.Firebrick;
            AppendDrive(result.StandardOutput + Environment.NewLine + result.StandardError);
        }
        catch (Exception ex)
        {
            _driveStatus.Text = ex.Message;
            _driveStatus.ForeColor = Color.Firebrick;
            AppendDrive(ex.Message);
        }
    }

    private async Task DisconnectDriveAsync()
    {
        try
        {
            var result = await NetworkDriveService.DisconnectAsync(_driveLetter.Text);
            _driveStatus.Text = result.Success ? "Netzlaufwerk getrennt." : "Trennen fehlgeschlagen.";
            _driveStatus.ForeColor = result.Success ? Color.ForestGreen : Color.Firebrick;
            AppendDrive(result.StandardOutput + Environment.NewLine + result.StandardError);
        }
        catch (Exception ex)
        {
            _driveStatus.Text = ex.Message;
            _driveStatus.ForeColor = Color.Firebrick;
            AppendDrive(ex.Message);
        }
    }

    private static Panel BuildCard(string title, Control content)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 40, 12, 12), Margin = new Padding(6) };
        card.Controls.Add(content);
        card.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Padding = new Padding(0, 6, 0, 0) });
        return card;
    }

    private static void AddField(TableLayoutPanel form, string caption, Control control, int row)
    {
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        form.Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(0, 9, 8, 0) }, 0, row);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 5, 8, 5);
        form.Controls.Add(control, 1, row);
        form.SetColumnSpan(control, 2);
    }

    private static Button CreateButton(string text, bool primary, int width)
    {
        var button = new Button { Text = text, Width = width, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = primary ? Color.FromArgb(36, 111, 187) : Color.White, ForeColor = primary ? Color.White : Color.FromArgb(45, 58, 74), Margin = new Padding(6, 0, 0, 0) };
        button.FlatAppearance.BorderColor = primary ? button.BackColor : Color.FromArgb(200, 208, 218);
        return button;
    }

    private void AppendSoftware(string text) => _softwareLog.AppendText(text.Trim() + Environment.NewLine + Environment.NewLine);
    private void AppendDrive(string text) => _driveLog.AppendText(text.Trim() + Environment.NewLine + Environment.NewLine);
}
