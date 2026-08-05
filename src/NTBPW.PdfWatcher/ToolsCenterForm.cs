namespace NTBPW.PdfWatcher;

internal sealed partial class ToolsCenterForm : Form
{
    private readonly CheckedListBox _packages = new() { Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly TextBox _softwareLog = LogBox();
    private readonly ComboBox _driveLetter = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _uncPath = new() { PlaceholderText = @"\\SERVER\Freigabe" };
    private readonly TextBox _userName = new() { PlaceholderText = @"DOMÃ„NE\Benutzer (optional)" };
    private readonly TextBox _password = new() { UseSystemPasswordChar = true, PlaceholderText = "Passwort (wird nicht gespeichert)" };
    private readonly CheckBox _persistent = new() { Text = "Bei Anmeldung wiederherstellen", Checked = true, AutoSize = true };
    private readonly Label _driveStatus = new() { AutoSize = true, Text = "Bereit" };
    private readonly TextBox _driveLog = LogBox();
    private readonly ComboBox _diagnosticCommand = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _diagnosticTarget = new() { PlaceholderText = "Ziel, z. B. server01, 192.168.1.1 oder ntb.local" };
    private readonly TextBox _diagnosticLog = LogBox();
    private readonly Label _diagnosticStatus = new() { AutoSize = true, Text = "Bereit" };
    private CancellationTokenSource? _operation;

    public ToolsCenterForm(Icon icon)
    {
        Text = "NTB Tools Center";
        Width = 940;
        Height = 650;
        MinimumSize = new Size(820, 560);
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

        foreach (var command in NetworkDiagnosticsService.Commands)
            _diagnosticCommand.Items.Add(command);
        _diagnosticCommand.DisplayMember = nameof(DiagnosticCommand.Name);
        _diagnosticCommand.SelectedIndex = 0;
        _diagnosticCommand.SelectedIndexChanged += (_, _) => UpdateDiagnosticTargetState();

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 6) };
        tabs.TabPages.Add(BuildSoftwarePage());
        tabs.TabPages.Add(BuildNetworkDrivePage());
        tabs.TabPages.Add(BuildDiagnosticsPage());
tabs.TabPages.Add(BuildWindowsPage());
tabs.TabPages.Add(BuildSystemPage());
        Controls.Add(tabs);
        UpdateDiagnosticTargetState();
    }

    private TabPage BuildSoftwarePage()
    {
        var page = NewPage("Software Center");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.Controls.Add(BuildCard("Apps auswÃ¤hlen", _packages), 0, 0);
        root.Controls.Add(BuildCard("Installationsprotokoll", _softwareLog), 1, 0);

        var actions = ActionBar();
        var install = CreateButton("AusgewÃ¤hlte installieren", true, 180);
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
        var page = NewPage("Netzlaufwerke");
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
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
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

    private TabPage BuildDiagnosticsPage()
    {
        var page = NewPage("Netzwerkdiagnose");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var input = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, BackColor = Color.White, Padding = new Padding(18) };
        input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(input, "Diagnose", _diagnosticCommand, 0);
        AddField(input, "Ziel", _diagnosticTarget, 1);
        input.Controls.Add(_diagnosticStatus, 1, 2);
        root.Controls.Add(input, 0, 0);
        root.Controls.Add(BuildCard("Ausgabe", _diagnosticLog), 0, 1);

        var actions = ActionBar();
        var run = CreateButton("AusfÃ¼hren", true, 120);
        var flushDns = CreateButton("DNS-Cache leeren", false, 150);
        var clear = CreateButton("Ausgabe lÃ¶schen", false, 130);
        var cancel = CreateButton("Abbrechen", false, 100);
        run.Click += async (_, _) => await RunDiagnosticAsync();
        flushDns.Click += async (_, _) => await FlushDnsAsync();
        clear.Click += (_, _) => _diagnosticLog.Clear();
        cancel.Click += (_, _) => _operation?.Cancel();
        actions.Controls.Add(run);
        actions.Controls.Add(flushDns);
        actions.Controls.Add(clear);
        actions.Controls.Add(cancel);
        root.Controls.Add(actions, 0, 2);
        page.Controls.Add(root);
        return page;
    }

    private async Task InstallSelectedAsync()
    {
        var selected = _packages.CheckedItems.Cast<SoftwarePackage>().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Bitte mindestens eine App auswÃ¤hlen.", "Software Center", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _operation = new CancellationTokenSource();
        _softwareLog.Clear();
        if (!await WingetService.IsAvailableAsync(_operation.Token))
        {
            Append(_softwareLog, "Winget wurde nicht gefunden. Bitte den Microsoft App Installer installieren oder aktualisieren.");
            return;
        }

        foreach (var package in selected)
        {
            try
            {
                Append(_softwareLog, $"> Installiere {package.Name} ({package.Id}) â€¦");
                var result = await WingetService.InstallAsync(package.Id, _operation.Token);
                Append(_softwareLog, result.Success ? $"âœ“ {package.Name} wurde verarbeitet." : $"âœ— {package.Name}: {result.StandardError}\r\n{result.StandardOutput}");
            }
            catch (OperationCanceledException)
            {
                Append(_softwareLog, "Vorgang abgebrochen.");
                break;
            }
        }
    }

    private async Task UpgradeAllAsync()
    {
        _operation = new CancellationTokenSource();
        Append(_softwareLog, "> Aktualisiere alle verfÃ¼gbaren Apps â€¦");
        var result = await WingetService.UpgradeAllAsync(_operation.Token);
        Append(_softwareLog, result.Success ? "âœ“ Aktualisierung abgeschlossen." : $"âœ— Aktualisierung fehlgeschlagen: {result.StandardError}\r\n{result.StandardOutput}");
    }

    private async Task TestDriveAsync()
    {
        _driveStatus.Text = "Teste Verbindung â€¦";
        var result = await NetworkDriveService.TestPathAsync(_uncPath.Text);
        _driveStatus.Text = result.Message;
        _driveStatus.ForeColor = result.Success ? Color.ForestGreen : Color.Firebrick;
        Append(_driveLog, result.Message);
    }

    private async Task ConnectDriveAsync()
    {
        try
        {
            _driveStatus.Text = "Verbinde â€¦";
            var result = await NetworkDriveService.ConnectAsync(_driveLetter.Text, _uncPath.Text, _persistent.Checked,
                string.IsNullOrWhiteSpace(_userName.Text) ? null : _userName.Text,
                string.IsNullOrWhiteSpace(_password.Text) ? null : _password.Text);
            _password.Clear();
            _driveStatus.Text = result.Success ? "Netzlaufwerk verbunden." : "Verbindung fehlgeschlagen.";
            _driveStatus.ForeColor = result.Success ? Color.ForestGreen : Color.Firebrick;
            Append(_driveLog, result.StandardOutput + Environment.NewLine + result.StandardError);
        }
        catch (Exception ex)
        {
            _driveStatus.Text = ex.Message;
            _driveStatus.ForeColor = Color.Firebrick;
            Append(_driveLog, ex.Message);
        }
    }

    private async Task DisconnectDriveAsync()
    {
        var result = await NetworkDriveService.DisconnectAsync(_driveLetter.Text);
        _driveStatus.Text = result.Success ? "Netzlaufwerk getrennt." : "Trennen fehlgeschlagen.";
        _driveStatus.ForeColor = result.Success ? Color.ForestGreen : Color.Firebrick;
        Append(_driveLog, result.StandardOutput + Environment.NewLine + result.StandardError);
    }

    private async Task RunDiagnosticAsync()
    {
        if (_diagnosticCommand.SelectedItem is not DiagnosticCommand command) return;
        _operation = new CancellationTokenSource();
        _diagnosticLog.Clear();
        _diagnosticStatus.Text = $"{command.Name} lÃ¤uft â€¦";
        _diagnosticStatus.ForeColor = Color.DarkOrange;
        try
        {
            var result = await NetworkDiagnosticsService.RunAsync(command, _diagnosticTarget.Text, _operation.Token,
                line => BeginInvoke(() => _diagnosticLog.AppendText(line + Environment.NewLine)));
            _diagnosticStatus.Text = result.Success ? "Diagnose abgeschlossen." : $"Fehlercode {result.ExitCode}";
            _diagnosticStatus.ForeColor = result.Success ? Color.ForestGreen : Color.Firebrick;
        }
        catch (OperationCanceledException)
        {
            _diagnosticStatus.Text = "Diagnose abgebrochen.";
            _diagnosticStatus.ForeColor = Color.DarkOrange;
        }
        catch (Exception ex)
        {
            _diagnosticStatus.Text = ex.Message;
            _diagnosticStatus.ForeColor = Color.Firebrick;
            Append(_diagnosticLog, ex.Message);
        }
    }

    private async Task FlushDnsAsync()
    {
        _operation = new CancellationTokenSource();
        _diagnosticLog.Clear();
        try
        {
            var result = await NetworkDiagnosticsService.FlushDnsAsync(_operation.Token,
                line => BeginInvoke(() => _diagnosticLog.AppendText(line + Environment.NewLine)));
            _diagnosticStatus.Text = result.Success ? "DNS-Cache wurde geleert." : "DNS-Cache konnte nicht geleert werden.";
            _diagnosticStatus.ForeColor = result.Success ? Color.ForestGreen : Color.Firebrick;
        }
        catch (Exception ex)
        {
            _diagnosticStatus.Text = ex.Message;
            _diagnosticStatus.ForeColor = Color.Firebrick;
        }
    }

    private void UpdateDiagnosticTargetState()
    {
        var requiresTarget = (_diagnosticCommand.SelectedItem as DiagnosticCommand)?.RequiresTarget == true;
        _diagnosticTarget.Enabled = requiresTarget;
        if (!requiresTarget) _diagnosticTarget.Clear();
    }

    private TabPage NewPage(string title) => new(title) { BackColor = BackColor, Padding = new Padding(14) };
    private static TextBox LogBox() => new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 9F), WordWrap = false };
    private static FlowLayoutPanel ActionBar() => new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };

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
        form.SetColumnSpan(control, Math.Max(1, form.ColumnCount - 1));
    }

    private static Button CreateButton(string text, bool primary, int width)
    {
        var button = new Button { Text = text, Width = width, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = primary ? Color.FromArgb(36, 111, 187) : Color.White, ForeColor = primary ? Color.White : Color.FromArgb(45, 58, 74), Margin = new Padding(6, 0, 0, 0) };
        button.FlatAppearance.BorderColor = primary ? button.BackColor : Color.FromArgb(200, 208, 218);
        return button;
    }

    private static void Append(TextBox box, string text) => box.AppendText(text.Trim() + Environment.NewLine + Environment.NewLine);
}

