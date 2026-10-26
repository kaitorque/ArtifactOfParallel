# Builds the plugin, installs it into the VP r2modman profile, and zips the package folder next to this repo.
$ErrorActionPreference = "Stop"
$name = "ArtifactOfParallel"
$dir = $PSScriptRoot

dotnet build (Join-Path $dir "$name.csproj") -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$package = Join-Path $dir "package"
Copy-Item (Join-Path $dir "bin\Release\netstandard2.1\$name.dll") $package -Force

$install = Join-Path $env:APPDATA "r2modmanPlus-local\RiskOfRain2\profiles\VP\BepInEx\plugins\Kaitorque-$name"
New-Item -ItemType Directory -Force $install | Out-Null
Copy-Item (Join-Path $package "*") $install -Force
Write-Host "Installed to $install"

$version = (Get-Content (Join-Path $package "manifest.json") -Raw | ConvertFrom-Json).version_number
$zip = Join-Path (Split-Path $dir -Parent) "Kaitorque-$name-$version.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $package "*") -DestinationPath $zip
Write-Host "Packaged $zip"
