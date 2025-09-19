#Requires -Version 5.1
<#
.SYNOPSIS
    Pick66 - Standalone Installer (No .NET SDK Required)

.DESCRIPTION
    One-click installer that works without requiring .NET SDK installation.
    Tries multiple methods: pre-built executable, build from source, or download.

.PARAMETER Launch
    Launch Pick66 after successful installation

.PARAMETER OutputPath
    Custom installation directory (default: Downloads\Pick66)

.PARAMETER ForceDownload
    Force download latest version even if local build is possible

.EXAMPLE
    .\install-standalone.ps1
    
.EXAMPLE
    .\install-standalone.ps1 -Launch

.EXAMPLE  
    .\install-standalone.ps1 -OutputPath "C:\Games\Pick66" -Launch -ForceDownload
#>

param(
    [switch]$Launch,
    [string]$OutputPath = "",
    [switch]$ForceDownload
)

$ErrorActionPreference = "Stop"

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "         Pick66 - Standalone Installer" -ForegroundColor Cyan  
Write-Host "        Windows x64 Self-Contained Install" -ForegroundColor Cyan
Write-Host "        No .NET SDK Installation Required!" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Set output path
if ([string]::IsNullOrEmpty($OutputPath)) {
    $OutputPath = Join-Path $env:USERPROFILE "Downloads\Pick66"
}

if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

Write-Host "[1/3] Setting up installation directory..." -ForegroundColor Yellow
Write-Host "Installation path: $OutputPath" -ForegroundColor Cyan
Write-Host ""

$targetExe = Join-Path $OutputPath "pick6_loader.exe"
$success = $false

# Method 1: Check for pre-built executable in same directory as installer
$sourceExe = Join-Path (Split-Path $MyInvocation.MyCommand.Path) "pick6_loader.exe"
if (!$ForceDownload -and (Test-Path $sourceExe)) {
    Write-Host "[2/3] Found pre-built executable, copying to installation directory..." -ForegroundColor Yellow
    try {
        Copy-Item $sourceExe $targetExe -Force
        $success = $true
        Write-Host "Pre-built executable installed successfully!" -ForegroundColor Green
    } catch {
        Write-Host "Failed to copy pre-built executable: $_" -ForegroundColor Red
    }
}

# Method 2: Try to build from source if .NET SDK is available
if (!$success -and !$ForceDownload) {
    Write-Host "[2/3] Looking for .NET SDK to build from source..." -ForegroundColor Yellow
    try {
        $dotnetVersion = dotnet --version 2>$null
        if ($dotnetVersion) {
            Write-Host "Found .NET SDK version: $dotnetVersion" -ForegroundColor Green
            Write-Host "Building from source... this may take a few minutes." -ForegroundColor Yellow
            
            # Build the project
            dotnet clean | Out-Null
            dotnet restore
            if ($LASTEXITCODE -eq 0) {
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
                
                dotnet @publishArgs | Out-Null
                if ($LASTEXITCODE -eq 0 -and (Test-Path "dist\pick6_loader.exe")) {
                    Copy-Item "dist\pick6_loader.exe" $targetExe -Force
                    $success = $true
                    Write-Host "Built and installed from source successfully!" -ForegroundColor Green
                } else {
                    Write-Host "Build failed, trying alternative method..." -ForegroundColor Yellow
                }
            }
        }
    } catch {
        Write-Host ".NET SDK not found or build failed, trying alternative method..." -ForegroundColor Yellow
    }
}

# Method 3: Download from GitHub releases
if (!$success) {
    Write-Host "[2/3] Attempting to download latest release from GitHub..." -ForegroundColor Yellow
    try {
        # Test internet connectivity
        $testConnection = Test-NetConnection -ComputerName "api.github.com" -Port 443 -InformationLevel Quiet -WarningAction SilentlyContinue
        if (!$testConnection) {
            throw "No internet connection available"
        }
        
        # Get latest release info
        $releaseUrl = "https://api.github.com/repos/isogloss/pick66/releases/latest"
        $release = Invoke-RestMethod $releaseUrl -Headers @{"User-Agent" = "Pick66-Installer"}
        
        # Find the executable asset
        $asset = $release.assets | Where-Object { $_.name -like "*pick6_loader.exe" -or $_.name -like "*pick66*.exe" }
        if (!$asset) {
            # If no executable found, try to find a zip with executable
            $zipAsset = $release.assets | Where-Object { $_.name -like "*.zip" }
            if ($zipAsset) {
                $zipPath = Join-Path $env:TEMP "pick66_release.zip"
                Invoke-WebRequest $zipAsset.browser_download_url -OutFile $zipPath -Headers @{"User-Agent" = "Pick66-Installer"}
                
                # Extract and find executable
                $extractPath = Join-Path $env:TEMP "pick66_extracted"
                Expand-Archive $zipPath $extractPath -Force
                $extractedExe = Get-ChildItem $extractPath -Recurse -Filter "*pick6_loader.exe" | Select-Object -First 1
                if ($extractedExe) {
                    Copy-Item $extractedExe.FullName $targetExe -Force
                    $success = $true
                    Write-Host "Downloaded and extracted successfully!" -ForegroundColor Green
                    Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
                    Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue
                }
            }
        } else {
            # Direct executable download
            Invoke-WebRequest $asset.browser_download_url -OutFile $targetExe -Headers @{"User-Agent" = "Pick66-Installer"}
            $success = $true
            Write-Host "Downloaded successfully!" -ForegroundColor Green
        }
    } catch {
        Write-Host "Download failed: $_" -ForegroundColor Red
    }
}

# Final check
if (!$success -or !(Test-Path $targetExe)) {
    Write-Host ""
    Write-Host "ERROR: Could not install Pick66 executable." -ForegroundColor Red
    Write-Host ""
    Write-Host "To install Pick66, you have these options:" -ForegroundColor Yellow
    Write-Host "  1. Download the full release package from GitHub with pre-built executable" -ForegroundColor White
    Write-Host "  2. Install .NET 8 SDK and run this installer again" -ForegroundColor White
    Write-Host "  3. Check your internet connection and try again" -ForegroundColor White
    Write-Host ""
    Write-Host "Visit: https://github.com/isogloss/pick66/releases" -ForegroundColor Cyan
    Read-Host "Press Enter to exit"
    exit 1
}

# Installation complete
Write-Host "[3/3] Finalizing installation..." -ForegroundColor Yellow

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
Write-Host "Installation complete! No additional dependencies required." -ForegroundColor Green
Read-Host "Press Enter to exit"