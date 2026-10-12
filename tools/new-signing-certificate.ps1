<#
.SYNOPSIS
    Erzeugt ein eigenes (selbstsigniertes) Code-Signing-Zertifikat fuer die interne Verteilung.

.DESCRIPTION
    Legt im Ausgabeordner an:
      - NTB-PW-Codesigning.pfx      privater Schluessel + Zertifikat (GEHEIM, nur fuer GitHub-Secret)
      - NTB-PW-Codesigning.cer      oeffentliches Zertifikat (per GPO/Intune an die Firmen-PCs verteilen)
      - NTB-PW-Codesigning.pfx.base64.txt   Inhalt fuer das GitHub-Secret SIGNING_PFX_BASE64

    Das Zertifikat wird nach dem Export aus dem Zertifikatspeicher des Benutzers entfernt.
    Anleitung: docs/CODE-SIGNING.md, Abschnitt "Kostenlos: eigenes Zertifikat".

.EXAMPLE
    .\tools\new-signing-certificate.ps1 -OutputPath C:\Sicher\Codesigning
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$OutputPath,
    [string]$Publisher = 'Netz, Technik, Büro GbR',
    [int]$Years = 5
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
$password = Read-Host -AsSecureString 'Passwort für die PFX-Datei (wird auch als GitHub-Secret SIGNING_PFX_PASSWORD benötigt)'

# Kommas im Namen muessen im Distinguished Name in Anfuehrungszeichen stehen.
$cert = New-SelfSignedCertificate -Type CodeSigningCert `
    -Subject "CN=""$Publisher"", O=""$Publisher""" `
    -KeyAlgorithm RSA -KeyLength 3072 -HashAlgorithm SHA256 `
    -KeyExportPolicy Exportable -CertStoreLocation Cert:\CurrentUser\My `
    -NotAfter (Get-Date).AddYears($Years)

try {
    $pfx = Join-Path $OutputPath 'NTB-PW-Codesigning.pfx'
    $cer = Join-Path $OutputPath 'NTB-PW-Codesigning.cer'
    Export-PfxCertificate -Cert $cert -FilePath $pfx -Password $password | Out-Null
    Export-Certificate -Cert $cert -FilePath $cer | Out-Null
    [Convert]::ToBase64String([IO.File]::ReadAllBytes($pfx)) | Set-Content -Path "$pfx.base64.txt" -Encoding ascii
} finally {
    Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -Force
}

Write-Host ''
Write-Host "Erstellt in $OutputPath :"
Write-Host "  NTB-PW-Codesigning.pfx             -> sicher aufbewahren (Backup!), NICHT weitergeben"
Write-Host "  NTB-PW-Codesigning.pfx.base64.txt  -> Inhalt als GitHub-Secret SIGNING_PFX_BASE64 eintragen, danach löschen"
Write-Host "  NTB-PW-Codesigning.cer             -> per GPO/Intune als 'Vertrauenswürdige Stammzertifizierungsstelle'"
Write-Host "                                        und 'Vertrauenswürdiger Herausgeber' verteilen"
Write-Host "Fingerabdruck: $($cert.Thumbprint)   gültig bis $($cert.NotAfter.ToString('dd.MM.yyyy'))"
