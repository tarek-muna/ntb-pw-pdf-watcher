<#
.SYNOPSIS
    Baut das Inno-Setup (dist\NTB-PW-PDF-Watcher-Setup-<Version>.exe) aus dem publish-Ordner.

.PARAMETER Sign
    Setup und Deinstallationsprogramm waehrend des Kompilierens mit tools\sign-file.ps1 signieren
    (Umgebungsvariablen fuer Azure Artifact Signing oder eigenes Zertifikat muessen gesetzt sein).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Version,
    [switch]$Sign
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$iscc = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 (ISCC.exe) wurde nicht gefunden.' }

$arguments = @("/DMyAppVersion=$Version")
if ($Sign) {
    $signScript = Join-Path $PSScriptRoot 'sign-file.ps1'
    # $q = Anfuehrungszeichen, $f = zu signierende Datei (von Inno Setup ersetzt).
    $command = 'ntbpw=powershell.exe -NoProfile -ExecutionPolicy Bypass -File $q' + $signScript + '$q -Files $f'
    $arguments += '/DUseSignTool', "/S$command"
}
$arguments += (Join-Path $root 'installer\NTB-PW-PDF-Watcher.iss')

& $iscc @arguments
if ($LASTEXITCODE -ne 0) { throw "ISCC endete mit Code $LASTEXITCODE." }
