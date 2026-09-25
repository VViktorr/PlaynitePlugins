param(
    [Parameter(Mandatory = $true)]
    [string]$PlayniteDirectory
)
$ErrorActionPreference = 'Stop'
$toolbox = Join-Path $PlayniteDirectory 'Toolbox.exe'
if (-not (Test-Path $toolbox)) { throw "Nie znaleziono Toolbox.exe w: $PlayniteDirectory" }
& (Join-Path $PSScriptRoot 'build-install-folder.ps1')
if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw 'Kompilacja nie powiodła się.' }
$extension = Join-Path $PSScriptRoot 'install\SteamGridDBGallery'
$destination = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
& $toolbox pack $extension $destination
if ($LASTEXITCODE -ne 0) { throw 'Pakowanie .pext nie powiodło się.' }
$package = Join-Path $destination 'SteamGridDBGallery.pext'
if (-not (Test-Path $package)) { throw "Nie znaleziono paczki $package" }
Write-Host "Gotowa paczka: $package"
