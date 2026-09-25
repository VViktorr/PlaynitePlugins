param(
    [Parameter(Mandatory = $true)]
    [string]$PlayniteDirectory
)
$ErrorActionPreference = 'Stop'
$toolbox = Join-Path $PlayniteDirectory 'Toolbox.exe'
if (-not (Test-Path $toolbox)) { throw "Nie znaleziono Toolbox.exe w: $PlayniteDirectory" }
& (Join-Path $PSScriptRoot 'build-install-folder.ps1')
if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw 'Budowanie wtyczki nie powiodlo sie.' }
$extension = Join-Path $PSScriptRoot 'install\GryOnlineMetadata'
foreach ($name in @('extension.yaml', 'GryOnlineMetadata.dll', 'icon.png')) {
    if (-not (Test-Path (Join-Path $extension $name))) { throw "Brak pliku do spakowania: $name" }
}
$destination = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
& $toolbox pack $extension $destination
if ($LASTEXITCODE -ne 0) { throw 'Toolbox.exe nie mogl utworzyc paczki .pext.' }
$package = Join-Path $destination 'GryOnlineMetadata.pext'
if (-not (Test-Path $package)) { throw "Nie znaleziono paczki $package po zakonczeniu pakowania." }
Write-Host "Gotowa paczka: $package"
