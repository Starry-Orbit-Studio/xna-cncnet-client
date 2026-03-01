<#
.SYNOPSIS
  Quick build script for development - uses dotnet build instead of publish
.DESCRIPTION
  Much faster than build.ps1 for iterative development
.EXAMPLE
  .\QuickBuild.ps1
  Build with default configuration (WindowsDXDebug + net8.0-windows)
.EXAMPLE
  .\QuickBuild.ps1 -Configuration release
  Build in Release mode
.EXAMPLE
  .\QuickBuild.ps1 -Framework net48
  Build for .NET Framework 4.8
#>

param(
    [Parameter()]
    [ValidateSet("debug", "release")]
    [string]
    $Configuration = "debug",

    [Parameter()]
    [ValidateSet("net8", "net48")]
    [string]
    $Framework = "net8"
)

$script:ConfigMap = @{
    "debug" = "WindowsDXDebug"
    "release" = "WindowsDXRelease"
}

$script:FrameworkMap = @{
    "net8" = "net8.0-windows"
    "net48" = "net48"
}

$script:ConfigName = $script:ConfigMap[$Configuration]
$script:FrameworkName = $script:FrameworkMap[$Framework]

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Quick Build: $ConfigName ($FrameworkName)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "DXMainClient") "DXMainClient.csproj"
$projectPath = Resolve-Path $projectPath

dotnet build $projectPath --configuration $script:ConfigName --framework $script:FrameworkName

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Build completed successfully!" -ForegroundColor Green
$outputPath = Join-Path (Join-Path (Join-Path (Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "DXMainClient") "bin") $script:ConfigName) "WindowsDX") $script:FrameworkName
Write-Host "Output: $outputPath" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Read-Host "Press Enter to exit"
