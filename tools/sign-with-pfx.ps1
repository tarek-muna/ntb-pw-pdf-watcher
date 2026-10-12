<#
.SYNOPSIS
    Signiert Dateien mit dem eigenen Code-Signing-Zertifikat (PFX) – fuer den Release-Workflow.

.DESCRIPTION
    Erwartet das Zertifikat als Base64 in $env:SIGNING_PFX_BASE64 und das Passwort in
    $env:SIGNING_PFX_PASSWORD (beides GitHub-Secrets). Mit -TrustOnThisMachine wird der
    oeffentliche Teil zusaetzlich als vertrauenswuerdig eingetragen (nur auf dem Build-Server),
    damit die anschliessende Pruefung und der Installer-Test wie auf einem Firmen-PC laufen.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string[]]$Files,
    [switch]$TrustOnThisMachine
)

$ErrorActionPreference = 'Stop'
if (-not $env:SIGNING_PFX_BASE64) { throw 'SIGNING_PFX_BASE64 ist nicht gesetzt.' }

$pfxPath = Join-Path ([IO.Path]::GetTempPath()) "ntbpw-signing-$([guid]::NewGuid().ToString('N')).pfx"
[IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($env:SIGNING_PFX_BASE64))
try {
    $flags = [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable
    $cert = New-Object Security.Cryptography.X509Certificates.X509Certificate2($pfxPath, $env:SIGNING_PFX_PASSWORD, $flags)
    if (-not $cert.HasPrivateKey) { throw 'Das PFX enthält keinen privaten Schlüssel.' }
    Write-Host "Zertifikat: $($cert.Subject) (gültig bis $($cert.NotAfter.ToString('yyyy-MM-dd')), Fingerabdruck $($cert.Thumbprint))"

    if ($TrustOnThisMachine) {
        foreach ($storeName in 'Root', 'TrustedPublisher') {
            $store = New-Object Security.Cryptography.X509Certificates.X509Store($storeName, 'LocalMachine')
            $store.Open('ReadWrite')
            $store.Add((New-Object Security.Cryptography.X509Certificates.X509Certificate2(,$cert.RawData)))
            $store.Close()
        }
    }

    $signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
    if (-not $signtool) { throw 'signtool.exe (Windows SDK) wurde nicht gefunden.' }

    foreach ($file in $Files) {
        Write-Host "Signiere $file ..."
        & $signtool.FullName sign /fd SHA256 /f $pfxPath /p $env:SIGNING_PFX_PASSWORD `
            /tr 'http://timestamp.digicert.com' /td SHA256 /d 'NTB-PW PDF-Watcher' $file
        if ($LASTEXITCODE -ne 0) { throw "Signieren von $file fehlgeschlagen (Code $LASTEXITCODE)." }
    }
} finally {
    Remove-Item $pfxPath -Force -ErrorAction SilentlyContinue
}
