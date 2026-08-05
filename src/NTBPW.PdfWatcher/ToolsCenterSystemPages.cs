namespace NTBPW.PdfWatcher;

internal sealed partial class ToolsCenterForm
{
    private readonly ComboBox _repairCommand = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _repairLog = LogBox();
    private readonly Label _repairStatus = new() { AutoSize = true, Text = "Bereit" };
    private readonly TextBox _systemReport = LogBox();

    private TabPage BuildWindowsPage()
    {
        foreach (var command in SystemToolsService.RepairCommands)
            _repairCommand.Items.Add(command);
        _repairCommand.DisplayMember = nameof(ToolCommand.Name);
        _repairCommand.SelectedIndex = 0;

        var page = NewPage("Windows-Reparatur");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var input = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.White, Padding = new Padding(18) };
        input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(input, "Windows-Werkzeug", _repairCommand, 0);
        input.Controls.Add(_repairStatus, 1, 1);
        root.Controls.Add(input, 0, 0);
        root.Controls.Add(BuildCard("Ausgabe", _repairLog), 0, 1);

        var actions = ActionBar();
        var run = CreateButton("Werkzeug ausführen", true, 160);
        var cancel = CreateButton("Abbrechen", false, 100);
        var clear = CreateButton("Ausgabe löschen", false, 130);
        run.Click += async (_, _) => await RunRepairAsync();
        cancel.Click += (_, _) => _operation?.Cancel();
        clear.Click += (_, _) => _repairLog.Clear();
        actions.Controls.Add(run);
        actions.Controls.Add(cancel);
        actions.Controls.Add(clear);
        root.Controls.Add(actions, 0, 2);
        page.Controls.Add(root);
        return page;
    }

    private TabPage BuildSystemPage()
    {
        var page = NewPage("Systeminformationen");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        _systemReport.Text = SystemToolsService.BuildSystemReport();
        root.Controls.Add(BuildCard("Systemübersicht", _systemReport), 0, 0);

        var actions = ActionBar();
        var refresh = CreateButton("Aktualisieren", true, 120);
        var copy = CreateButton("Kopieren", false, 110);
        refresh.Click += (_, _) => _systemReport.Text = SystemToolsService.BuildSystemReport();
        copy.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(_systemReport.Text)) Clipboard.SetText(_systemReport.Text); };
        actions.Controls.Add(refresh);
        actions.Controls.Add(copy);
        root.Controls.Add(actions, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private async Task RunRepairAsync()
    {
        if (_repairCommand.SelectedItem is not ToolCommand command) return;
        if (command.RequiresAdmin && MessageBox.Show(this,
                "Dieses Werkzeug benötigt Administratorrechte. Die Anwendung sollte als Administrator gestartet sein. Fortfahren?",
                "Windows-Reparatur", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _operation = new CancellationTokenSource();
        _repairLog.Clear();
        _repairStatus.Text = command.Name + " läuft …";
        _repairStatus.ForeColor = Color.DarkOrange;
        try
        {
            var result = await SystemToolsService.RunAsync(command, _operation.Token,
                line => BeginInvoke(() => _repairLog.AppendText(line + Environment.NewLine)));
            _repairStatus.Text = result.Success ? "Vorgang abgeschlossen." : $"Fehlercode {result.ExitCode}";
            _repairStatus.ForeColor = result.Success ? Color.ForestGreen : Color.Firebrick;
        }
        catch (OperationCanceledException)
        {
            _repairStatus.Text = "Vorgang abgebrochen.";
            _repairStatus.ForeColor = Color.DarkOrange;
        }
        catch (Exception ex)
        {
            _repairStatus.Text = ex.Message;
            _repairStatus.ForeColor = Color.Firebrick;
            Append(_repairLog, ex.Message);
        }
    }
}
