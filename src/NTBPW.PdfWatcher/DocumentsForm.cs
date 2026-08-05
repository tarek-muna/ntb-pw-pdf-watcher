using System.Diagnostics;

namespace NTBPW.PdfWatcher;

internal sealed class DocumentsForm : Form
{
    private readonly IReadOnlyList<ProcessingRecord> _source;
    private readonly Icon _appIcon;
    private readonly TextBox _search = new() { PlaceholderText = "Dateiname, Profil, Ergebnis oder Klassifikation suchen …" };
    private readonly ComboBox _period = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView _grid = new();
    private readonly Label _count = new() { AutoSize = true };
    private readonly Label _details = new() { AutoSize = false, Dock = DockStyle.Fill };

    public DocumentsForm(IReadOnlyList<ProcessingRecord> history, Icon appIcon)
    {
        _source = history;
        _appIcon = (Icon)appIcon.Clone();

        Text = "Dokumente und PDF-Historie";
        Icon = _appIcon;
        Width = 980;
        Height = 620;
        MinimumSize = new Size(820, 520);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 9F);

        BuildUi();
        ApplyFilter();

        FormClosed += (_, _) => _appIcon.Dispose();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        Controls.Add(root);

        var heading = new Panel { Dock = DockStyle.Fill };
        heading.Controls.Add(new Label
        {
            Text = "Dokumente",
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 45, 61),
            AutoSize = true,
            Location = new Point(0, 0)
        });
        heading.Controls.Add(new Label
        {
            Text = "Gespeicherte PDF-Vorgänge durchsuchen und öffnen",
            ForeColor = Color.FromArgb(103, 116, 133),
            AutoSize = true,
            Location = new Point(2, 36)
        });
        root.Controls.Add(heading, 0, 0);

        var filter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            Padding = new Padding(0, 6, 0, 6)
        };
        filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filter.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        filter.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        filter.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

        _search.Dock = DockStyle.Fill;
        _search.Margin = new Padding(0, 0, 10, 0);
        _search.TextChanged += (_, _) => ApplyFilter();
        filter.Controls.Add(_search, 0, 0);

        _period.Items.AddRange(["Alle", "Heute", "Letzte 7 Tage", "Letzte 30 Tage"]);
        _period.SelectedIndex = 0;
        _period.Dock = DockStyle.Fill;
        _period.Margin = new Padding(0, 0, 10, 0);
        _period.SelectedIndexChanged += (_, _) => ApplyFilter();
        filter.Controls.Add(_period, 1, 0);

        var refresh = CreateButton("Aktualisieren", false, 0);
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = new Padding(0, 0, 10, 0);
        refresh.Click += (_, _) => ApplyFilter();
        filter.Controls.Add(refresh, 2, 0);

        _count.Dock = DockStyle.Fill;
        _count.TextAlign = ContentAlignment.MiddleRight;
        _count.ForeColor = Color.FromArgb(103, 116, 133);
        filter.Controls.Add(_count, 3, 0);
        root.Controls.Add(filter, 0, 1);

        ConfigureGrid();
        root.Controls.Add(_grid, 0, 2);

        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(0, 10, 0, 0)
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));

        _details.BackColor = Color.White;
        _details.BorderStyle = BorderStyle.FixedSingle;
        _details.Padding = new Padding(12);
        _details.ForeColor = Color.FromArgb(55, 68, 84);
        _details.Text = "Kein Dokument ausgewählt.";
        bottom.Controls.Add(_details, 0, 0);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(8, 18, 0, 0)
        };
        var open = CreateButton("PDF öffnen", true, 110);
        var folder = CreateButton("Ordner öffnen", false, 110);
        var preview = CreateButton("Vorschau", false, 90);
        open.Click += (_, _) => OpenSelectedFile();
        folder.Click += (_, _) => OpenSelectedFolder();
        preview.Click += (_, _) => PreviewSelectedFile();
        actions.Controls.Add(open);
        actions.Controls.Add(folder);
        actions.Controls.Add(preview);
        bottom.Controls.Add(actions, 1, 0);
        root.Controls.Add(bottom, 0, 3);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.ColumnHeadersHeight = 36;
        _grid.RowTemplate.Height = 32;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "Zeit", FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "Datei", FillWeight = 42 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Profile", HeaderText = "Profil", FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Result", HeaderText = "Ergebnis", FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Classification", HeaderText = "Klassifikation", FillWeight = 22 });

        _grid.SelectionChanged += (_, _) => RefreshDetails();
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) OpenSelectedFile();
        };
    }

    private void ApplyFilter()
    {
        var query = _search.Text.Trim();
        var cutoff = _period.SelectedIndex switch
        {
            1 => DateTime.Today,
            2 => DateTime.Today.AddDays(-6),
            3 => DateTime.Today.AddDays(-29),
            _ => DateTime.MinValue
        };

        var filtered = _source
            .Where(x => x.Timestamp >= cutoff)
            .Where(x => string.IsNullOrWhiteSpace(query) || Matches(x, query))
            .OrderByDescending(x => x.Timestamp)
            .ToList();

        _grid.Rows.Clear();
        foreach (var item in filtered)
        {
            var row = _grid.Rows[_grid.Rows.Add(
                item.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"),
                Path.GetFileName(item.SourceFile),
                item.Profile,
                item.Result,
                item.Classification)];
            row.Tag = item;
        }

        _count.Text = $"{filtered.Count} Dokument(e)";
        RefreshDetails();
    }

    private static bool Matches(ProcessingRecord record, string query)
    {
        return Path.GetFileName(record.SourceFile).Contains(query, StringComparison.OrdinalIgnoreCase)
               || record.SourceFile.Contains(query, StringComparison.OrdinalIgnoreCase)
               || record.Profile.Contains(query, StringComparison.OrdinalIgnoreCase)
               || record.Result.Contains(query, StringComparison.OrdinalIgnoreCase)
               || record.Classification.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private ProcessingRecord? SelectedRecord()
    {
        return _grid.SelectedRows.Count == 1
            ? _grid.SelectedRows[0].Tag as ProcessingRecord
            : null;
    }

    private void RefreshDetails()
    {
        var item = SelectedRecord();
        if (item is null)
        {
            _details.Text = "Kein Dokument ausgewählt.";
            return;
        }

        var availability = File.Exists(item.SourceFile) ? "Datei vorhanden" : "Datei nicht erreichbar";
        _details.Text = $"{Path.GetFileName(item.SourceFile)}\r\n{item.Profile} · {item.Timestamp:dd.MM.yyyy HH:mm:ss}\r\n{availability}";
    }

    private void OpenSelectedFile()
    {
        var item = SelectedRecord();
        if (item is null) return;
        if (!File.Exists(item.SourceFile))
        {
            MessageBox.Show(this, "Die PDF-Datei ist nicht mehr erreichbar.", "Dokumente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo(item.SourceFile) { UseShellExecute = true });
    }

    private void OpenSelectedFolder()
    {
        var item = SelectedRecord();
        if (item is null) return;
        var folder = Path.GetDirectoryName(item.SourceFile);
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            MessageBox.Show(this, "Der Ordner ist nicht erreichbar.", "Dokumente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{item.SourceFile}\"") { UseShellExecute = true });
    }

    private void PreviewSelectedFile()
    {
        var item = SelectedRecord();
        if (item is null) return;
        if (!File.Exists(item.SourceFile))
        {
            MessageBox.Show(this, "Die PDF-Datei ist nicht mehr erreichbar.", "Dokumente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var preview = new PreviewForm(item.SourceFile);
        preview.ShowDialog(this);
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
}
