using System.Diagnostics;

namespace NTBPW.PdfWatcher;

internal sealed class UpdateForm : Form
{
    private readonly string _repository;
    private readonly Action _exitApplication;
    private readonly Label _current = new() { AutoSize = true };
    private readonly Label _available = new() { AutoSize = true };
    private readonly Label _status = new() { AutoSize = true };
    private readonly TextBox _notes = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill
    };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly Button _check = new() { Text = "Jetzt prüfen", Width = 110 };
    private readonly Button _download = new() { Text = "Herunterladen und installieren", Width = 210, Enabled = false };
    private readonly Button _releasePage = new() { Text = "GitHub-Release öffnen", Width = 150, Enabled = false };
    private readonly CancellationTokenSource _cancellation = new();
    private GitHubRelease? _release;

    public UpdateForm(string repository, Icon icon, Action exitApplication)
    {
        _repository = UpdateService.NormalizeRepository(repository);
        _exitApplication = exitApplication;
        Text = "NTB-PW – Updates";
        Icon = (Icon)icon.Clone();
        StartPosition = FormStartPosition.CenterParent;
        Width = 650;
        Height = 500;
        MinimumSize = new Size(590, 430);
        BuildUi();
        FormClosed += (_, _) => _cancellation.Cancel();
        Shown += async (_, _) => await CheckAsync();
    }

    private void BuildUi()
    {
        Font = new Font("Segoe UI", 9F);
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            RowCount = 5,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        Controls.Add(root);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.Controls.Add(new Label { Text = "Installierte Version", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(0, 8, 8, 2) }, 0, 0);
        _current.Text = UpdateService.CurrentVersionText;
        _current.Margin = new Padding(0, 8, 0, 2);
        header.Controls.Add(_current, 1, 0);
        header.Controls.Add(new Label { Text = "Verfügbare Version", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(0, 5, 8, 2) }, 0, 1);
        _available.Text = "–";
        _available.Margin = new Padding(0, 5, 0, 2);
        header.Controls.Add(_available, 1, 1);
        root.Controls.Add(header, 0, 0);

        _status.Text = $"Repository: {_repository}";
        _status.Dock = DockStyle.Fill;
        _status.AutoEllipsis = true;
        root.Controls.Add(_status, 0, 1);

        _notes.Text = "Release Notes werden nach der Prüfung angezeigt.";
        root.Controls.Add(_notes, 0, 2);
        root.Controls.Add(_progress, 0, 3);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        var close = new Button { Text = "Schließen", Width = 100 };
        close.Click += (_, _) => Close();
        _check.Click += async (_, _) => await CheckAsync();
        _download.Click += async (_, _) => await DownloadAndInstallAsync();
        _releasePage.Click += (_, _) => OpenReleasePage();
        buttons.Controls.Add(close);
        buttons.Controls.Add(_download);
        buttons.Controls.Add(_releasePage);
        buttons.Controls.Add(_check);
        root.Controls.Add(buttons, 0, 4);
    }

    private async Task CheckAsync()
    {
        SetBusy(true, "GitHub-Release wird geprüft …");
        try
        {
            _release = await UpdateService.GetLatestReleaseAsync(_repository, _cancellation.Token);
            if (_release is null)
            {
                _available.Text = "Kein Release vorhanden";
                _notes.Text = "Im Repository wurde noch kein veröffentlichter GitHub-Release gefunden.";
                _status.Text = "Noch kein Release verfügbar.";
                return;
            }

            _available.Text = string.IsNullOrWhiteSpace(_release.TagName) ? "Unbekannt" : _release.TagName;
            _notes.Text = string.IsNullOrWhiteSpace(_release.Notes) ? "Keine Release Notes vorhanden." : _release.Notes;
            _releasePage.Enabled = !string.IsNullOrWhiteSpace(_release.WebUrl);

            if (_release.Version is null)
            {
                _status.Text = "Die Release-Version konnte nicht ausgewertet werden.";
                return;
            }

            if (!UpdateService.IsNewer(_release))
            {
                _status.Text = "Die installierte Version ist aktuell.";
                return;
            }

            if (_release.Installer is null)
            {
                _status.Text = "Update gefunden, aber kein Setup-Asset vorhanden.";
                return;
            }

            if (_release.Checksum is null)
            {
                _status.Text = "Update gefunden, aber die SHA-256-Datei fehlt.";
                return;
            }

            _status.Text = $"Update {_release.TagName} ist verfügbar.";
            _download.Enabled = true;
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _status.Text = "Update-Prüfung fehlgeschlagen.";
            _notes.Text = ex.Message;
            AppLogger.Write("Update-Prüfung fehlgeschlagen: " + ex);
        }
        finally
        {
            SetBusy(false, _status.Text);
        }
    }

    private async Task DownloadAndInstallAsync()
    {
        if (_release is null) return;
        _progress.Visible = true;
        _progress.Value = 0;
        _download.Enabled = false;
        _check.Enabled = false;
        _status.Text = "Update wird heruntergeladen und geprüft …";

        try
        {
            var progress = new Progress<int>(value => _progress.Value = Math.Clamp(value, 0, 100));
            var installer = await UpdateService.DownloadAndVerifyAsync(_release, progress, _cancellation.Token);
            _status.Text = "Download abgeschlossen. Der Installer wird gestartet.";

            if (MessageBox.Show(
                    this,
                    "Die Prüfsumme ist gültig. Jetzt den Installer starten und die Anwendung schließen?",
                    "NTB-PW Update",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                _download.Enabled = true;
                _check.Enabled = true;
                return;
            }

            UpdateService.StartInstaller(installer);
            _exitApplication();
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _status.Text = "Update konnte nicht installiert werden.";
            MessageBox.Show(this, ex.Message, "NTB-PW Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppLogger.Write("Update fehlgeschlagen: " + ex);
            _download.Enabled = true;
            _check.Enabled = true;
        }
    }

    private void OpenReleasePage()
    {
        if (_release is null || string.IsNullOrWhiteSpace(_release.WebUrl)) return;
        Process.Start(new ProcessStartInfo(_release.WebUrl) { UseShellExecute = true });
    }

    private void SetBusy(bool busy, string status)
    {
        _check.Enabled = !busy;
        if (busy) _download.Enabled = false;
        _status.Text = status;
        UseWaitCursor = busy;
    }
}
