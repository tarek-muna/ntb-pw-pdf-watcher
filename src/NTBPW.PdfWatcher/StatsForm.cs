namespace NTBPW.PdfWatcher;

// Statistik: Kennzahlen, Auswertung je Profil und Verlauf der letzten 14 Tage.
internal sealed class StatsForm : Form
{
    private const int DaysShown = 14;

    public StatsForm(IEnumerable<ProcessingRecord> records)
    {
        Text = "Statistik";
        Width = 820;
        Height = 560;
        MinimumSize = new Size(720, 480);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 9F);

        var list = records.ToList();
        var today = DateTime.Today;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 2, BackColor = BackColor };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(0, 0, 0, 12) };
        for (var i = 0; i < 5; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        cards.Controls.Add(Card("HEUTE", list.Count(r => r.Timestamp >= today)), 0, 0);
        cards.Controls.Add(Card("7 TAGE", list.Count(r => r.Timestamp >= today.AddDays(-6))), 1, 0);
        cards.Controls.Add(Card("30 TAGE", list.Count(r => r.Timestamp >= today.AddDays(-29))), 2, 0);
        cards.Controls.Add(Card("GESAMT", list.Count), 3, 0);
        cards.Controls.Add(Card("FEHLER", list.Count(Statistics.IsError), list.Any(Statistics.IsError) ? Color.Firebrick : null), 4, 0);
        root.Controls.Add(cards, 0, 0);

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 470, BackColor = BackColor };
        split.Panel1.Padding = new Padding(0, 0, 8, 0);
        split.Panel2.Padding = new Padding(8, 0, 0, 0);
        split.Panel1.Controls.Add(Section("Je Profil", ProfileGrid(Statistics.ByProfile(list, today))));
        split.Panel2.Controls.Add(Section($"Letzte {DaysShown} Tage", DayGrid(Statistics.ByDay(list, today, DaysShown))));
        root.Controls.Add(split, 0, 1);
    }

    private static Panel Card(string caption, int value, Color? valueColor = null)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 10, 0) };
        card.Controls.Add(new Label { Text = caption, AutoSize = true, Location = new Point(12, 10), ForeColor = Color.FromArgb(112, 126, 144), Font = new Font("Segoe UI", 8F, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = value.ToString("N0"), AutoSize = true, Location = new Point(12, 32), ForeColor = valueColor ?? Color.FromArgb(31, 45, 61), Font = new Font("Segoe UI", 18F, FontStyle.Bold) });
        return card;
    }

    private static Panel Section(string title, Control content)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 40, 12, 12) };
        panel.Controls.Add(content);
        panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(45, 58, 74) });
        return panel;
    }

    private static DataGridView Grid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AutoGenerateColumns = false
        };
    }

    private static DataGridViewTextBoxColumn NumberColumn(string header, float weight)
    {
        var column = new DataGridViewTextBoxColumn { HeaderText = header, FillWeight = weight };
        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        return column;
    }

    private static DataGridView ProfileGrid(IReadOnlyList<ProfileStatistics> rows)
    {
        var grid = Grid();
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Profil", FillWeight = 40 });
        foreach (var header in new[] { "Heute", "7 Tage", "30 Tage", "Gesamt", "Fehler" })
            grid.Columns.Add(NumberColumn(header, 12));
        foreach (var r in rows)
            grid.Rows.Add(r.Profile, r.Today, r.Last7Days, r.Last30Days, r.Total, r.Errors);
        return grid;
    }

    private static DataGridView DayGrid(IReadOnlyList<DayStatistics> rows)
    {
        var grid = Grid();
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tag", FillWeight = 45 });
        grid.Columns.Add(NumberColumn("PDFs", 15));
        var bars = new DataGridViewTextBoxColumn { HeaderText = "", FillWeight = 40 };
        bars.DefaultCellStyle.ForeColor = Color.FromArgb(36, 111, 187);
        grid.Columns.Add(bars);
        var max = rows.Count == 0 ? 1 : Math.Max(1, rows.Max(r => r.Count));
        foreach (var r in rows)
        {
            // Einfacher Balken aus Blockzeichen als Verlaufsanzeige.
            var bar = new string('█', (int)Math.Round(20.0 * r.Count / max));
            grid.Rows.Add(r.Day.ToString("ddd, dd.MM."), r.Count, bar);
        }
        return grid;
    }
}
