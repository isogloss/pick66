#Requires -Version 5.1
<#
.SYNOPSIS
    Pick66 - Simple PowerShell Installer (Windows Only)

.DESCRIPTION
    Legacy installer that requires .NET SDK. For better experience, use install-standalone.ps1
    
.PARAMETER Launch
    Launch Pick66 after successful installation

.PARAMETER OutputPath
    Custom installation directory (default: Downloads\Pick66)

.EXAMPLE
    .\setup.ps1
    
.EXAMPLE
    .\setup.ps1 -Launch

.EXAMPLE  
    .\setup.ps1 -OutputPath "C:\Games\Pick66" -Launch
#>

param(
    [switch]$Launch,
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "         Pick66 - Simple PowerShell Installer" -ForegroundColor Cyan  
Write-Host "          Windows x64 Self-Contained Build" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "NOTICE: This installer now works without requiring .NET SDK!" -ForegroundColor Yellow
Write-Host "For the best experience, use install-standalone.ps1 instead." -ForegroundColor Yellow
Write-Host ""

# Check if enhanced installer exists
$enhancedInstaller = Join-Path (Split-Path $MyInvocation.MyCommand.Path) "install-standalone.ps1"
if (Test-Path $enhancedInstaller) {
    Write-Host "Found enhanced installer, redirecting..." -ForegroundColor Green
    $args = @()
    if ($Launch) { $args += "-Launch" }
    if ($OutputPath) { $args += "-OutputPath", $OutputPath }
    
    & $enhancedInstaller @args
    exit $LASTEXITCODE
}

Write-Host "Using legacy installer (requires .NET 8 SDK)..." -ForegroundColor Yellow
Write-Host ""

# Check .NET 8 SDK
Write-Host "[1/4] Checking .NET 8 SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "Found .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: .NET 8 SDK is required but not found." -ForegroundColor Red
    Write-Host "Please install .NET 8 SDK from: https://dot.net" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Clean and restore
Write-Host "[2/4] Cleaning and restoring packages..." -ForegroundColor Yellow
dotnet clean | Out-Null
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Package restore failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Build and publish
Write-Host "[3/4] Building Pick66 (Windows x64, self-contained)..." -ForegroundColor Yellow
$publishArgs = @(
    "publish", "src\Pick6.Loader\Pick6.Loader.csproj",
    "--configuration", "Release",
    "--runtime", "win-x64", 
    "--self-contained", "true",
    "--output", "dist",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:PublishReadyToRun=true",
    "-p:DebugType=none"
)

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Install
Write-Host "[4/4] Installing to Downloads folder..." -ForegroundColor Yellow

if ([string]::IsNullOrEmpty($OutputPath)) {
    $OutputPath = Join-Path $env:USERPROFILE "Downloads\Pick66"
}

if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$sourceExe = "dist\pick6_loader.exe"
$targetExe = Join-Path $OutputPath "pick6_loader.exe"

if (!(Test-Path $sourceExe)) {
    Write-Host "ERROR: Built executable not found at: $sourceExe" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Copy-Item $sourceExe $targetExe -Force

# Get file size
$fileSize = [Math]::Round((Get-Item $targetExe).Length / 1MB, 1)

Write-Host ""
Write-Host "===============================================" -ForegroundColor Green
Write-Host "           INSTALLATION COMPLETE!" -ForegroundColor Green  
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Installed to: $targetExe" -ForegroundColor Cyan
Write-Host "Size: $fileSize MB" -ForegroundColor Cyan
Write-Host ""
Write-Host "To run Pick66:" -ForegroundColor Yellow
Write-Host "  1. Navigate to: $OutputPath" -ForegroundColor White
Write-Host "  2. Double-click: pick6_loader.exe" -ForegroundColor White
Write-Host ""
Write-Host "Or run from PowerShell:" -ForegroundColor Yellow
Write-Host "  & `"$targetExe`"" -ForegroundColor White
Write-Host ""

# Launch if requested
if ($Launch) {
    Write-Host "Launching Pick66..." -ForegroundColor Yellow
    Start-Process $targetExe
    Write-Host "Pick66 launched successfully!" -ForegroundColor Green
} else {
    $launchNow = Read-Host "Launch Pick66 now? (y/N)"
    if ($launchNow -match '^[yY]') {
        Write-Host "Launching Pick66..." -ForegroundColor Yellow
        Start-Process $targetExe
    }
}

Write-Host ""
Write-Host "Installation complete!" -ForegroundColor Green
Read-Host "Press Enter to exit"