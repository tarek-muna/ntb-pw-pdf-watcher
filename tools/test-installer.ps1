<#
.SYNOPSIS
    Installations- und Upgrade-Test fuer das Setup (wird in GitHub Actions ausgefuehrt).

.DESCRIPTION
    1. Installiert das zuletzt veroeffentlichte Release (falls vorhanden) still.
    2. Legt eine Test-Konfiguration an.
    3. Installiert das neue Setup darueber und prueft Version und Erhalt der Konfiguration.
    4. Startet die Anwendung minimiert und prueft den Start-Eintrag im Protokoll.
    5. Deinstalliert wieder.

    ACHTUNG: Veraendert die Installation und den Datenordner des aktuellen Benutzers.
    Nur auf Build-Servern oder Test-VMs ausfuehren.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$NewSetup,
    [Parameter(Mandatory)] [string]$ExpectedVersion,
    [string]$Repository = 'tarek-muna/ntb-pw-pdf-watcher',
    # Signierter Build: installierte EXE und Deinstallationsprogramm muessen gueltig signiert sein.
    [switch]$ExpectSigned
)

$ErrorActionPreference = 'Stop'
$UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{C8C5093C-0A92-47BD-A845-NTBPW3000001}_is1'
$ExeName      = 'NTB-PW PDF-Watcher.exe'
$DataDir      = Join-Path $env:LOCALAPPDATA 'NTB-PW\PDF-Watcher-3'
$ConfigPath   = Join-Path $DataDir 'config.json'
$LogPath      = Join-Path $DataDir 'PDF-Watcher.log'

function Get-InstallInfo {
    foreach ($root in 'HKCU:', 'HKLM:') {
        $key = Join-Path $root $UninstallKey
        if (Test-Path $key) { return Get-ItemProperty $key }
    }
    return $null
}

function Install-Setup([string]$Path, [string]$Label, [switch]$ProbeTempFiles) {
    Write-Host "Installiere $Label ..."
    $before = @(Get-ChildItem $env:TEMP -Directory -Filter 'is-*.tmp' -ErrorAction SilentlyContinue | ForEach-Object FullName)
    $p = Start-Process -FilePath $Path -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/SP-' -PassThru
    $probed = @{}
    while (-not $p.HasExited) {
        if ($ProbeTempFiles) {
            # Inno Setup entpackt waehrend der Installation eine Hilfs-EXE (setup.tmp) nach %TEMP%\is-*.tmp.
            # Die intelligente App-Steuerung prueft auch diese Datei; hier wird ihre Signatur protokolliert.
            Get-ChildItem $env:TEMP -Directory -Filter 'is-*.tmp' -ErrorAction SilentlyContinue |
                Where-Object { $before -notcontains $_.FullName } |
                Get-ChildItem -Filter '*.tmp' -File -ErrorAction SilentlyContinue |
                Where-Object { -not $probed.ContainsKey($_.FullName) } |
                ForEach-Object {
                    try { $probed[$_.FullName] = (Get-AuthenticodeSignature $_.FullName).Status } catch { }
                }
        }
        Start-Sleep -Milliseconds 100
    }
    $p.WaitForExit()
    foreach ($entry in $probed.GetEnumerator()) { Write-Host "  Hilfsdatei $(Split-Path $entry.Key -Leaf): Signatur $($entry.Value)" }
    if ($ProbeTempFiles -and $probed.Count -eq 0) { Write-Host '  Hilfsdatei setup.tmp konnte nicht erfasst werden.' }
    if ($p.ExitCode -ne 0) { throw "$Label`: Setup endete mit Code $($p.ExitCode)." }
    $info = Get-InstallInfo
    if (-not $info) { throw "$Label`: Kein Deinstallationseintrag gefunden." }
    Write-Host "  installiert: Version $($info.DisplayVersion) in $($info.InstallLocation)"
    return $info
}

# 1. Vorherige Version
$headers = @{ 'User-Agent' = 'ntbpw-installer-test' }
if ($env:GITHUB_TOKEN) { $headers.Authorization = "Bearer $env:GITHUB_TOKEN" }
$previous = $null
try { $previous = Invoke-RestMethod "https://api.github.com/repos/$Repository/releases/latest" -Headers $headers } catch { Write-Host 'Kein vorheriges Release gefunden.' }

$previousVersion = if ($previous) { $previous.tag_name.TrimStart('v') } else { $null }
if ($previous -and $previousVersion -ne $ExpectedVersion) {
    $asset = $previous.assets | Where-Object { $_.name -like 'NTB-PW-PDF-Watcher-Setup-*.exe' } | Select-Object -First 1
    if (-not $asset) { throw "Release $($previous.tag_name) enthält kein Setup." }
    $oldSetup = Join-Path $env:RUNNER_TEMP $asset.name
    if (-not $env:RUNNER_TEMP) { $oldSetup = Join-Path ([IO.Path]::GetTempPath()) $asset.name }
    Invoke-WebRequest $asset.browser_download_url -OutFile $oldSetup -Headers $headers
    $old = Install-Setup $oldSetup "Vorversion $previousVersion"
    if ($old.DisplayVersion -ne $previousVersion) { Write-Warning "Vorversion meldet $($old.DisplayVersion) statt $previousVersion." }

    # 2. Konfiguration, die das Upgrade ueberstehen muss
    New-Item -ItemType Directory -Force -Path $DataDir | Out-Null
    '{ "AutoStart": false, "Profiles": [ { "Name": "Upgrade-Test", "Folder": "C:\\Upgrade-Test" } ] }' |
        Set-Content -Path $ConfigPath -Encoding utf8
} else {
    Write-Host 'Upgrade-Teil übersprungen (keine oder gleiche Vorversion).'
}

# 3. Neue Version
$new = Install-Setup $NewSetup "Neue Version $ExpectedVersion" -ProbeTempFiles:$ExpectSigned
if ($new.DisplayVersion -ne $ExpectedVersion) { throw "Installierte Version $($new.DisplayVersion), erwartet $ExpectedVersion." }
$exe = Join-Path $new.InstallLocation $ExeName
if (-not (Test-Path $exe)) { throw "$exe fehlt nach der Installation." }
$fileVersion = (Get-Item $exe).VersionInfo.ProductVersion
if (-not $fileVersion.StartsWith($ExpectedVersion)) { throw "EXE meldet Version $fileVersion, erwartet $ExpectedVersion." }
if ((Test-Path $ConfigPath) -and -not ((Get-Content $ConfigPath -Raw) -match 'Upgrade-Test')) {
    throw 'Die Konfiguration wurde beim Upgrade verändert oder überschrieben.'
}
Write-Host '  Version und Konfiguration geprüft.'

if ($ExpectSigned) {
    foreach ($file in @($exe, (Join-Path $new.InstallLocation 'unins000.exe'))) {
        $sig = Get-AuthenticodeSignature $file
        if ($sig.Status -ne 'Valid') { throw "$(Split-Path $file -Leaf) ist nicht gültig signiert ($($sig.Status))." }
        Write-Host "  $(Split-Path $file -Leaf): signiert von $($sig.SignerCertificate.Subject)"
    }
}

# 4. Starttest
$logBefore = if (Test-Path $LogPath) { (Get-Item $LogPath).Length } else { 0 }
$process = Start-Process -FilePath $exe -ArgumentList '--minimized' -PassThru
Start-Sleep -Seconds 10
try {
    if ($process.HasExited) { throw "Die Anwendung hat sich nach dem Start beendet (Code $($process.ExitCode))." }
    if (-not (Test-Path $LogPath)) { throw 'Die Anwendung hat kein Protokoll geschrieben.' }
    $stream = [IO.File]::Open($LogPath, 'Open', 'Read', 'ReadWrite')
    try { $stream.Seek($logBefore, 'Begin') | Out-Null; $newLog = (New-Object IO.StreamReader($stream)).ReadToEnd() } finally { $stream.Dispose() }
    if ($newLog -notmatch "Version $([regex]::Escape($ExpectedVersion)) gestartet") { throw "Start-Eintrag fehlt im Protokoll:`n$newLog" }
    if ($newLog -match 'SCHWERER FEHLER') { throw "Schwerer Fehler beim Start:`n$newLog" }
    Write-Host '  Anwendung startet fehlerfrei.'
} finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
}

# 5. Deinstallation
$uninstaller = Join-Path $new.InstallLocation 'unins000.exe'
$p = Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -Wait -PassThru
if ($p.ExitCode -ne 0) { throw "Deinstallation endete mit Code $($p.ExitCode)." }
Start-Sleep -Seconds 3
if (Test-Path $exe) { throw 'Die EXE ist nach der Deinstallation noch vorhanden.' }
Write-Host 'Installer-Test erfolgreich.'
