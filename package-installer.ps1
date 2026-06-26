$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$localDotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { "dotnet" }

$artifacts = Join-Path $root "artifacts"
$publishDir = Join-Path $artifacts "app-publish"
$payloadZip = Join-Path $artifacts "payload.zip"
$installerResources = Join-Path $root "src\WindowsTunTrafficTray.Installer\Resources"
$installerPayload = Join-Path $installerResources "payload.zip"
$installerPublish = Join-Path $artifacts "installer"
$setupOutput = Join-Path $artifacts "WindowsTunTrafficTraySetup.exe"

Remove-Item -LiteralPath $publishDir, $installerPublish -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $payloadZip, $installerPayload, $setupOutput -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $artifacts, $publishDir, $installerResources, $installerPublish -Force | Out-Null

& $dotnet publish (Join-Path $root "src\WindowsTunTrafficTray\WindowsTunTrafficTray.csproj") `
    --configuration Release `
    --self-contained false `
    -p:PublishSingleFile=true `
    --output $publishDir

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $payloadZip -Force
Copy-Item -LiteralPath $payloadZip -Destination $installerPayload -Force

& $dotnet publish (Join-Path $root "src\WindowsTunTrafficTray.Installer\WindowsTunTrafficTray.Installer.csproj") `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    --output $installerPublish

Copy-Item -LiteralPath (Join-Path $installerPublish "WindowsTunTrafficTraySetup.exe") -Destination $setupOutput -Force
Write-Host "Installer created: $setupOutput"
