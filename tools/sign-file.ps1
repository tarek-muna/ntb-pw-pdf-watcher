<#
.SYNOPSIS
    Signiert Dateien fuer den Release-Workflow – mit Azure Artifact Signing oder eigenem Zertifikat.

.DESCRIPTION
    Wird direkt im Workflow und von Inno Setup (SignTool, je Datei) aufgerufen.

    Azure Artifact Signing (hat Vorrang), wenn gesetzt:
      ARTIFACT_SIGNING_ENDPOINT, ARTIFACT_SIGNING_ACCOUNT, ARTIFACT_SIGNING_PROFILE
      ARTIFACT_SIGNING_DLIB   Pfad zu Azure.CodeSigning.Dlib.dll (siehe prepare-artifact-signing.ps1)
      Anmeldung vorher per azure/login (Azure CLI).

    Eigenes Zertifikat, sonst:
      SIGNING_PFX_BASE64, SIGNING_PFX_PASSWORD

    Mit -TrustOnThisMachine wird ein eigenes Zertifikat zusaetzlich als vertrauenswuerdig eingetragen
    (nur auf dem Build-Server), damit Pruefung und Installer-Test wie auf einem Firmen-PC laufen.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string[]]$Files,
    [switch]$TrustOnThisMachine
)

$ErrorActionPreference = 'Stop'

function Find-SignTool {
    $tool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
        Where-Object { $_.Directory.Parent.Name -match '^\d+(\.\d+)+$' } |
        Sort-Object { [version]$_.Directory.Parent.Name } -Descending | Select-Object -First 1
    if (-not $tool) { throw 'signtool.exe (Windows SDK) wurde nicht gefunden.' }
    return $tool.FullName
}

$signtool = Find-SignTool

if ($env:ARTIFACT_SIGNING_ACCOUNT) {
    if (-not $env:ARTIFACT_SIGNING_DLIB -or -not (Test-Path $env:ARTIFACT_SIGNING_DLIB)) {
        throw 'ARTIFACT_SIGNING_DLIB fehlt – zuerst tools\prepare-artifact-signing.ps1 ausführen.'
    }
    $metadata = Join-Path ([IO.Path]::GetTempPath()) 'ntbpw-artifact-signing.json'
    @{
        Endpoint               = $env:ARTIFACT_SIGNING_ENDPOINT
        CodeSigningAccountName = $env:ARTIFACT_SIGNING_ACCOUNT
        CertificateProfileName = $env:ARTIFACT_SIGNING_PROFILE
    } | ConvertTo-Json | Set-Content -Path $metadata -Encoding utf8

    foreach ($file in $Files) {
        Write-Host "Signiere (Azure Artifact Signing) $file ..."
        & $signtool sign /v /fd SHA256 /tr 'http://timestamp.acs.microsoft.com' /td SHA256 `
            /dlib $env:ARTIFACT_SIGNING_DLIB /dmdf $metadata $file
        if ($LASTEXITCODE -ne 0) { throw "Signieren von $file fehlgeschlagen (Code $LASTEXITCODE)." }
    }
    return
}

if (-not $env:SIGNING_PFX_BASE64) { throw 'Weder Azure Artifact Signing noch SIGNING_PFX_BASE64 ist eingerichtet.' }

$pfxPath = Join-Path ([IO.Path]::GetTempPath()) "ntbpw-signing-$([guid]::NewGuid().ToString('N')).pfx"
[IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($env:SIGNING_PFX_BASE64))
try {
    $cert = New-Object Security.Cryptography.X509Certificates.X509Certificate2($pfxPath, $env:SIGNING_PFX_PASSWORD)
    if (-not $cert.HasPrivateKey) { throw 'Das PFX enthält keinen privaten Schlüssel.' }

    if ($TrustOnThisMachine) {
        Write-Host "Zertifikat: $($cert.Subject) (gültig bis $($cert.NotAfter.ToString('yyyy-MM-dd')))"
        foreach ($storeName in 'Root', 'TrustedPublisher') {
            $store = New-Object Security.Cryptography.X509Certificates.X509Store($storeName, 'LocalMachine')
            $store.Open('ReadWrite')
            $store.Add((New-Object Security.Cryptography.X509Certificates.X509Certificate2(,$cert.RawData)))
            $store.Close()
        }
    }

    foreach ($file in $Files) {
        Write-Host "Signiere (eigenes Zertifikat) $file ..."
        & $signtool sign /fd SHA256 /f $pfxPath /p $env:SIGNING_PFX_PASSWORD `
            /tr 'http://timestamp.digicert.com' /td SHA256 /d 'NTB-PW PDF-Watcher' $file
        if ($LASTEXITCODE -ne 0) { throw "Signieren von $file fehlgeschlagen (Code $LASTEXITCODE)." }
    }
} finally {
    Remove-Item $pfxPath -Force -ErrorAction SilentlyContinue
}
