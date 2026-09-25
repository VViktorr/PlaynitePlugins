$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'GryOnlineMetadata.csproj'
$build = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $build) { throw 'Brak dotnet SDK. Zainstaluj Visual Studio z obsluga .NET Framework 4.6.2 i uruchom skrypt ponownie.' }
& dotnet build $project --configuration Release
if ($LASTEXITCODE -ne 0) { throw 'Kompilacja nie powiodla sie. Przeslij pelny blad kompilatora.' }
$source = Join-Path $PSScriptRoot 'bin\Release\net462'
$dll = Join-Path $source 'GryOnlineMetadata.dll'
if (-not (Test-Path $dll)) { throw "Nie znaleziono $dll" }
$target = Join-Path $PSScriptRoot 'install\GryOnlineMetadata'
New-Item -ItemType Directory -Path $target -Force | Out-Null
Copy-Item $dll $target -Force
Copy-Item (Join-Path $PSScriptRoot 'extension.yaml') $target -Force
Copy-Item (Join-Path $PSScriptRoot 'icon.png') $target -Force
Write-Host "Gotowy folder: $target"
Write-Host 'Skopiuj ten folder do katalogu Extensions Playnite i uruchom Playnite ponownie.'
