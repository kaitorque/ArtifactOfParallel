# Builds the plugin and zips the package folder next to this repo. With -Install, also copies it into the VP r2modman profile.
param([switch]$Install)
$ErrorActionPreference = "Stop"
$name = "ArtifactOfParallel"
$dir = $PSScriptRoot

dotnet build (Join-Path $dir "$name.csproj") -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$package = Join-Path $dir "package"
Copy-Item (Join-Path $dir "bin\Release\netstandard2.1\$name.dll") $package -Force

if ($Install) {
    $target = Join-Path $env:APPDATA "r2modmanPlus-local\RiskOfRain2\profiles\VP\BepInEx\plugins\Kaitorque-$name"
    New-Item -ItemType Directory -Force $target | Out-Null
    Copy-Item (Join-Path $package "*") $target -Force
    Write-Host "Installed to $target"
}

$version = (Get-Content (Join-Path $package "manifest.json") -Raw | ConvertFrom-Json).version_number
$zip = Join-Path (Split-Path $dir -Parent) "Kaitorque-$name-$version.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $package "*") -DestinationPath $zip
Write-Host "Packaged $zip"
