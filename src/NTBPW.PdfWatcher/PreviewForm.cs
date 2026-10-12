using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace NTBPW.PdfWatcher;

// PDF-Vorschau ueber WebView2 (Edge-PDF-Anzeige). Die WebView2-Runtime ist unter Windows 10/11
// in der Regel vorinstalliert; fehlt sie, wird ein Hinweis angezeigt.
internal sealed class PreviewForm : Form
{
    private readonly string _path;
    private readonly WebView2 _view = new() { Dock = DockStyle.Fill };

    public PreviewForm(string path)
    {
        _path = path;
        Text = "PDF-Vorschau – " + Path.GetFileName(path);
        Width = 950;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(_view);
        Shown += async (_, _) => await LoadPreviewAsync();
    }

    private async Task LoadPreviewAsync()
    {
        try
        {
            // Eigener Datenordner, damit WebView2 nicht neben der EXE schreiben muss.
            var userData = Path.Combine(AppConfig.DataDirectory, "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, userData);
            await _view.EnsureCoreWebView2Async(environment);
            _view.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _view.CoreWebView2.Navigate(new Uri(_path).AbsoluteUri);
        }
        catch (Exception ex)
        {
            AppLogger.Write("PDF-Vorschau nicht verfügbar: " + ex.Message);
            if (IsDisposed) return;
            Controls.Clear();
            Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Die eingebettete PDF-Vorschau ist auf diesem System nicht verfügbar\n" +
                       "(Microsoft Edge WebView2-Runtime fehlt).\n\nDie Datei kann über „PDF öffnen“ angezeigt werden."
            });
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _view.Dispose();
        base.Dispose(disposing);
    }
}
