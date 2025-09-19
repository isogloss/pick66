#Requires -Version 5.1
<#
.SYNOPSIS
    Create a complete Pick66 release package for distribution

.DESCRIPTION
    This script builds Pick66 and creates a complete package that users can download
    and install without any dependencies. The package includes the installer and
    optionally a pre-built executable.

.PARAMETER Version
    Version string for the release (e.g., "1.0.0")

.PARAMETER OutputPath
    Directory to create the release package (default: .\release)

.PARAMETER IncludeExecutable
    Include pre-built executable in the package (recommended for end users)

.PARAMETER Clean
    Clean build directories before building

.EXAMPLE
    .\create-release-package.ps1 -Version "1.0.0" -IncludeExecutable

.EXAMPLE
    .\create-release-package.ps1 -Version "1.0.0" -Clean -OutputPath "C:\Releases"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [string]$OutputPath = ".\release",
    
    [switch]$IncludeExecutable,
    
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "        Pick66 Release Package Creator" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Create output directory
$releaseDir = Join-Path $OutputPath "Pick66-v$Version"
$packageDir = Join-Path $releaseDir "Pick66-Installer"

Write-Host "Creating release package: Pick66-v$Version" -ForegroundColor Yellow
Write-Host "Output directory: $releaseDir" -ForegroundColor Cyan
Write-Host ""

if (Test-Path $releaseDir) {
    Write-Host "Removing existing release directory..." -ForegroundColor Yellow
    Remove-Item $releaseDir -Recurse -Force
}

New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

# Clean if requested
if ($Clean) {
    Write-Host "[1/5] Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean | Out-Null
}

# Build if including executable
if ($IncludeExecutable) {
    Write-Host "[2/5] Building Pick66 executable..." -ForegroundColor Yellow
    
    # Restore packages
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        throw "Package restore failed"
    }
    
    # Build and publish
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
        throw "Build failed"
    }
    
    if (!(Test-Path "dist\pick6_loader.exe")) {
        throw "Built executable not found"
    }
    
    # Copy executable to package
    Copy-Item "dist\pick6_loader.exe" $packageDir -Force
    $exeSize = [Math]::Round((Get-Item "dist\pick6_loader.exe").Length / 1MB, 1)
    Write-Host "Built executable: $exeSize MB" -ForegroundColor Green
} else {
    Write-Host "[2/5] Skipping executable build (source-only package)" -ForegroundColor Yellow
}

Write-Host "[3/5] Copying installer files..." -ForegroundColor Yellow

# Copy installer files
$installerFiles = @(
    "install.bat",
    "install-standalone.bat", 
    "install-standalone.ps1",
    "setup.bat",
    "setup.ps1",
    "README.md",
    "INSTALL.md"
)

foreach ($file in $installerFiles) {
    if (Test-Path $file) {
        Copy-Item $file $packageDir -Force
        Write-Host "  Copied: $file" -ForegroundColor Gray
    } else {
        Write-Warning "File not found: $file"
    }
}

Write-Host "[4/5] Copying source code (for developers)..." -ForegroundColor Yellow

# Copy source files for developers who want to build
$sourceDirs = @("src", "docs", "Tools")
foreach ($dir in $sourceDirs) {
    if (Test-Path $dir) {
        Copy-Item $dir $packageDir -Recurse -Force
        Write-Host "  Copied directory: $dir" -ForegroundColor Gray
    }
}

# Copy essential files
$essentialFiles = @(
    "Pick6.sln",
    "Directory.Build.props",
    ".gitignore"
)

foreach ($file in $essentialFiles) {
    if (Test-Path $file) {
        Copy-Item $file $packageDir -Force
        Write-Host "  Copied: $file" -ForegroundColor Gray
    }
}

Write-Host "[5/5] Creating release archive..." -ForegroundColor Yellow

# Create ZIP file
$zipFile = Join-Path $OutputPath "Pick66-v$Version-Complete.zip"
if (Test-Path $zipFile) {
    Remove-Item $zipFile -Force
}

Compress-Archive -Path $packageDir -DestinationPath $zipFile -CompressionLevel Optimal

# Calculate sizes
$packageSize = [Math]::Round((Get-ChildItem $packageDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 1)
$zipSize = [Math]::Round((Get-Item $zipFile).Length / 1MB, 1)

Write-Host ""
Write-Host "===============================================" -ForegroundColor Green
Write-Host "         RELEASE PACKAGE CREATED!" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Version: $Version" -ForegroundColor Cyan
Write-Host "Package directory: $packageDir ($packageSize MB)" -ForegroundColor Cyan
Write-Host "ZIP file: $zipFile ($zipSize MB)" -ForegroundColor Cyan
Write-Host ""

if ($IncludeExecutable) {
    Write-Host "✓ Includes pre-built executable (users don't need .NET SDK)" -ForegroundColor Green
} else {
    Write-Host "⚠ Source-only package (users will need .NET SDK or internet)" -ForegroundColor Yellow
}

Write-Host "✓ Includes all installer variants" -ForegroundColor Green
Write-Host "✓ Includes source code for developers" -ForegroundColor Green
Write-Host "✓ Ready for distribution" -ForegroundColor Green
Write-Host ""

Write-Host "Users can:" -ForegroundColor White
Write-Host "  1. Download and extract the ZIP file" -ForegroundColor White
Write-Host "  2. Double-click install.bat" -ForegroundColor White
Write-Host "  3. No other dependencies required!" -ForegroundColor White

Write-Host ""
Write-Host "Release package creation complete!" -ForegroundColor Green