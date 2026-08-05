namespace NTBPW.PdfWatcher;

internal sealed class PreviewForm : Form
{
    public PreviewForm(string path)
    {
        Text = "PDF-Vorschau – " + Path.GetFileName(path); Width = 950; Height = 700; StartPosition = FormStartPosition.CenterParent;
        var browser = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true };
        Controls.Add(browser);
        try { browser.Navigate(new Uri(path)); }
        catch { Controls.Clear(); Controls.Add(new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Text = "Auf diesem System ist keine eingebettete PDF-Vorschau verfügbar.\nDie Datei kann über 'Öffnen' angezeigt werden." }); }
    }
}
