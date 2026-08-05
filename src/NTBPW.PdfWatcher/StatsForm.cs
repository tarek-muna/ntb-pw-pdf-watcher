namespace NTBPW.PdfWatcher;

internal sealed class StatsForm : Form
{
    public StatsForm(IEnumerable<ProcessingRecord> records)
    {
        Text = "Statistik"; Width = 720; Height = 460; StartPosition = FormStartPosition.CenterParent;
        var list = records.ToList(); var today = DateTime.Today; var week = today.AddDays(-6); var month = today.AddDays(-29);
        var summary = new Label { Dock = DockStyle.Top, Height = 90, Font = new Font("Segoe UI", 13, FontStyle.Bold), Padding = new Padding(15),
            Text = $"Heute: {list.Count(x => x.Timestamp >= today)}     Letzte 7 Tage: {list.Count(x => x.Timestamp >= week)}     Letzte 30 Tage: {list.Count(x => x.Timestamp >= month)}" };
        var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, DataSource = list.OrderByDescending(x => x.Timestamp).ToList(), AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        Controls.Add(grid); Controls.Add(summary);
    }
}
