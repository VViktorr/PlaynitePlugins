$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'SteamGridDBGallery.csproj'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Brak dotnet SDK. Zainstaluj Visual Studio z obsługą .NET Framework 4.6.2.'
}
& dotnet build $project --configuration Release
if ($LASTEXITCODE -ne 0) { throw 'Kompilacja nie powiodła się.' }
$source = Join-Path $PSScriptRoot 'bin\Release\net462'
$dll = Join-Path $source 'SteamGridDBGallery.dll'
if (-not (Test-Path $dll)) { throw "Nie znaleziono $dll" }
$target = Join-Path $PSScriptRoot 'install\SteamGridDBGallery'
New-Item -ItemType Directory -Path $target -Force | Out-Null
Copy-Item $dll $target -Force
Copy-Item (Join-Path $PSScriptRoot 'extension.yaml') $target -Force
Copy-Item (Join-Path $PSScriptRoot 'icon.png') $target -Force
Write-Host "Gotowy folder: $target"
