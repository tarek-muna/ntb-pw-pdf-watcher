namespace NTBPW.PdfWatcher;

internal sealed class ProfilesForm : Form
{
    private readonly BindingSource _source = new();
    public ProfilesForm(List<WatchProfile> profiles)
    {
        Text = "Profile / Scanner"; Width = 950; Height = 480; StartPosition = FormStartPosition.CenterParent;
        _source.DataSource = profiles;
        var grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true, DataSource = _source, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(8) };
        var add = new Button { Text = "Profil hinzufügen", Width = 150 }; var remove = new Button { Text = "Profil löschen", Width = 130 }; var ok = new Button { Text = "Übernehmen", Width = 120 };
        add.Click += (_, _) => _source.Add(new WatchProfile { Name = $"Scanner {profiles.Count + 1}" });
        remove.Click += (_, _) => { if (_source.Current is WatchProfile p && profiles.Count > 1) _source.Remove(p); };
        ok.Click += (_, _) => { grid.EndEdit(); DialogResult = DialogResult.OK; Close(); };
        bar.Controls.AddRange([add, remove, ok]); Controls.Add(grid); Controls.Add(bar);
    }
}
