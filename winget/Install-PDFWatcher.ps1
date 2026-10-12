<#
.SYNOPSIS
    Installiert den NTB-PW PDF-Watcher intern per winget aus einem GitHub-Release.

.DESCRIPTION
    Liest das gewuenschte Release (Standard: neuestes) aus GitHub, erzeugt daraus
    passende winget-Manifeste (Setup-URL + SHA-256 aus der .sha256-Datei des Releases)
    und installiert mit "winget install --manifest".

    Voraussetzung: Lokale Manifeste muessen einmalig von einem Administrator erlaubt werden:
        winget settings --enable LocalManifestFiles

.PARAMETER Version
    Bestimmte Version installieren, z. B. 5.0.1. Ohne Angabe wird das neueste Release verwendet.

.PARAMETER OutputPath
    Nur die Manifeste in diesen Ordner schreiben (z. B. fuer Intune oder "winget validate"),
    ohne zu installieren.

.EXAMPLE
    .\Install-PDFWatcher.ps1
.EXAMPLE
    .\Install-PDFWatcher.ps1 -Version 5.0.1 -OutputPath .\manifest
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

$Repository        = 'tarek-muna/ntb-pw-pdf-watcher'
$PackageIdentifier = 'NetzTechnikBuero.NTBPWPDFWatcher'
# Inno Setup registriert die Deinstallation unter "<AppId>_is1" (AppId aus installer\NTB-PW-PDF-Watcher.iss).
$ProductCode       = '{C8C5093C-0A92-47BD-A845-NTBPW3000001}_is1'
$ManifestVersion   = '1.6.0'

$headers = @{ 'User-Agent' = 'NTB-PW-winget-installer'; 'Accept' = 'application/vnd.github+json' }
$releaseUrl = if ($Version) {
    "https://api.github.com/repos/$Repository/releases/tags/v$($Version.TrimStart('v'))"
} else {
    "https://api.github.com/repos/$Repository/releases/latest"
}

Write-Host "Lese Release von GitHub ..."
$release = Invoke-RestMethod -Uri $releaseUrl -Headers $headers
$packageVersion = $release.tag_name.TrimStart('v')

$installer = $release.assets | Where-Object { $_.name -like 'NTB-PW-PDF-Watcher-Setup-*.exe' } | Select-Object -First 1
$checksum  = $release.assets | Where-Object { $_.name -eq "$($installer.name).sha256" } | Select-Object -First 1
if (-not $installer) { throw "Release $($release.tag_name) enthaelt kein Setup." }
if (-not $checksum)  { throw "Release $($release.tag_name) enthaelt keine SHA-256-Datei zum Setup." }

$hash = ((Invoke-RestMethod -Uri $checksum.browser_download_url -Headers $headers) -split '\s+' |
    Where-Object { $_ } | Select-Object -First 1).ToUpperInvariant()
if ($hash -notmatch '^[0-9A-F]{64}$') { throw "Ungueltige SHA-256-Pruefsumme im Release." }

$target = if ($OutputPath) { $OutputPath } else { Join-Path ([IO.Path]::GetTempPath()) "ntbpw-winget-$packageVersion" }
New-Item -ItemType Directory -Force -Path $target | Out-Null

$common = @"
PackageIdentifier: $PackageIdentifier
PackageVersion: $packageVersion
"@

$versionManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.$ManifestVersion.schema.json
$common
DefaultLocale: de-DE
ManifestType: version
ManifestVersion: $ManifestVersion
"@

$installerManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.$ManifestVersion.schema.json
$common
InstallerType: inno
Scope: user
InstallModes:
  - interactive
  - silent
  - silentWithProgress
UpgradeBehavior: install
ProductCode: '$ProductCode'
Installers:
  - Architecture: x64
    InstallerUrl: $($installer.browser_download_url)
    InstallerSha256: $hash
ManifestType: installer
ManifestVersion: $ManifestVersion
"@

$localeManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.$ManifestVersion.schema.json
$common
PackageLocale: de-DE
Publisher: Netz, Technik, Büro GbR
PublisherUrl: https://github.com/tarek-muna
PackageName: NTB-PW PDF-Watcher Professional
PackageUrl: https://github.com/$Repository
License: Proprietär
Copyright: © Tarek Muna 2026
ShortDescription: Überwacht Scanner- und Eingangsordner und öffnet neue PDF-Dateien automatisch.
ReleaseNotesUrl: $($release.html_url)
ManifestType: defaultLocale
ManifestVersion: $ManifestVersion
"@

$utf8 = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $target "$PackageIdentifier.yaml"), $versionManifest, $utf8)
[IO.File]::WriteAllText((Join-Path $target "$PackageIdentifier.installer.yaml"), $installerManifest, $utf8)
[IO.File]::WriteAllText((Join-Path $target "$PackageIdentifier.locale.de-DE.yaml"), $localeManifest, $utf8)
Write-Host "Manifeste fuer Version $packageVersion erstellt: $target"

if ($OutputPath) { return }

if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    throw 'winget wurde nicht gefunden. Bitte "App Installer" aus dem Microsoft Store installieren.'
}

Write-Host "Installiere NTB-PW PDF-Watcher $packageVersion ..."
winget install --manifest $target --silent --disable-interactivity
$code = $LASTEXITCODE
Remove-Item -Recurse -Force $target -ErrorAction SilentlyContinue

if ($code -ne 0) {
    Write-Warning "winget wurde mit Code $code beendet."
    Write-Warning 'Falls lokale Manifeste gesperrt sind, einmalig als Administrator ausfuehren:'
    Write-Warning '    winget settings --enable LocalManifestFiles'
    exit $code
}
Write-Host 'Installation abgeschlossen.'
