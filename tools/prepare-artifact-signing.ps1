<#
.SYNOPSIS
    Laedt die Artifact-Signing-Clientbibliothek (NuGet: Microsoft.ArtifactSigning.Client) fuer signtool
    und setzt ARTIFACT_SIGNING_DLIB fuer die folgenden Workflow-Schritte.
#>
[CmdletBinding()]
param([string]$Version = '')

$ErrorActionPreference = 'Stop'
$package = 'microsoft.artifactsigning.client'
$target = Join-Path $env:RUNNER_TEMP 'artifact-signing-client'

if (-not $Version) {
    $index = Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/$package/index.json"
    $Version = $index.versions | Where-Object { $_ -notmatch '-' } | Select-Object -Last 1
}
Write-Host "Microsoft.ArtifactSigning.Client $Version"

$nupkg = Join-Path $env:RUNNER_TEMP "$package.$Version.zip"
Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/$package/$Version/$package.$Version.nupkg" -OutFile $nupkg
Expand-Archive -Path $nupkg -DestinationPath $target -Force

$dlib = Get-ChildItem $target -Recurse -Filter 'Azure.CodeSigning.Dlib.dll' |
    Where-Object { $_.FullName -match '\\x64\\' } | Select-Object -First 1
if (-not $dlib) { throw 'Azure.CodeSigning.Dlib.dll (x64) wurde im Paket nicht gefunden.' }
Write-Host "dlib: $($dlib.FullName)"
"ARTIFACT_SIGNING_DLIB=$($dlib.FullName)" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
