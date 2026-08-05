$ErrorActionPreference = 'Stop'

$path = Join-Path $PSScriptRoot '..\src\NTBPW.PdfWatcher\MainForm.cs'
$content = Get-Content -Path $path -Raw -Encoding UTF8

$old = @'
    private void ShowHistory()
    {
        using var form = new Form
        {
            Text = "PDF-Historie",
            Width = 820,
            Height = 480,
            StartPosition = FormStartPosition.CenterParent,
            Icon = _appIcon
        };
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            DataSource = _history.OrderByDescending(x => x.Timestamp).ToList(),
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            AllowUserToAddRows = false
        };
        form.Controls.Add(grid);
        form.ShowDialog(this);
    }
'@

$new = @'
    private void ShowHistory()
    {
        using var form = new DocumentsForm(_history, _appIcon);
        form.ShowDialog(this);
    }
'@

if ($content.Contains($new.Trim())) {
    Write-Host 'MainForm ist bereits auf DocumentsForm umgestellt.'
    exit 0
}

if (-not $content.Contains($old.Trim())) {
    throw 'Der erwartete ShowHistory-Block wurde nicht gefunden.'
}

$content = $content.Replace($old.Trim(), $new.Trim())
Set-Content -Path $path -Value $content -Encoding UTF8
Write-Host 'MainForm wurde auf DocumentsForm umgestellt.'
