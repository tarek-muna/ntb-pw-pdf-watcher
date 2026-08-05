namespace NTBPW.PdfWatcher;
internal sealed class LogForm : Form
{
    private readonly TextBox _box = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Font = new Font("Consolas", 9) };
    public LogForm()
    {
        Text = "Protokoll"; Width = 900; Height = 560; StartPosition = FormStartPosition.CenterParent;
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(6) };
        var refresh = new Button { Text = "Aktualisieren" }; var clear = new Button { Text = "Löschen" }; var export = new Button { Text = "Exportieren" };
        refresh.Click += (_, _) => LoadLog(); clear.Click += (_, _) => { File.WriteAllText(AppConfig.LogPath, ""); LoadLog(); };
        export.Click += (_, _) => { using var d = new SaveFileDialog { Filter = "Textdatei|*.txt", FileName = "PDF-Watcher-Log.txt" }; if (d.ShowDialog(this) == DialogResult.OK) File.Copy(AppConfig.LogPath, d.FileName, true); };
        bar.Controls.AddRange([refresh, clear, export]); Controls.Add(_box); Controls.Add(bar); LoadLog();
    }
    private void LoadLog() { try { _box.Text = File.Exists(AppConfig.LogPath) ? File.ReadAllText(AppConfig.LogPath) : "Noch keine Einträge."; _box.SelectionStart = _box.TextLength; _box.ScrollToCaret(); } catch { } }
}
