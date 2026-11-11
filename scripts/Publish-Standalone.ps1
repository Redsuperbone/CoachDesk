param(
    [ValidateSet('win-x64', 'win-x86')]
    [string]$Runtime = 'win-x64',
    [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root "artifacts\CoachDesk-$Runtime"
$desktopOutput = Join-Path $output 'CoachDesk'
$apiOutput = Join-Path $desktopOutput 'Api'

Remove-Item -LiteralPath $output -Recurse -Force -ErrorAction SilentlyContinue

$publishArguments = @('-c', 'Release')
if ($SelfContained) {
    $publishArguments += @('-r', $Runtime, '--self-contained', 'true')
}

dotnet publish (Join-Path $root 'src\CoachDesk.Api\CoachDesk.Api.csproj') @publishArguments -o $apiOutput
dotnet publish (Join-Path $root 'src\CoachDesk.Desktop\CoachDesk.Desktop.csproj') @publishArguments -o $desktopOutput

Write-Host "Distribución creada en: $desktopOutput"
Write-Host "Ejecuta CoachDesk.Desktop.exe desde esa carpeta."
if (-not $SelfContained) { Write-Host 'Esta versión requiere .NET 8 Desktop Runtime en el equipo destino.' }
