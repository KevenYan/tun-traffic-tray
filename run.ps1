$ErrorActionPreference = "Stop"

$localDotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { "dotnet" }

& $dotnet run --project "src\WindowsTunTrafficTray"
