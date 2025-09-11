#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pick66 Installation Script - Builds and installs Pick66 to Downloads folder

.DESCRIPTION
    This script builds Pick66 in Release mode, publishes a self-contained Windows x64 
    single-file executable, and installs it to the current user's Downloads folder.

.PARAMETER Launch
    Launch the application immediately after successful installation

.PARAMETER Clean
    Remove previous build artifacts before building

.PARAMETER OutputPath
    Custom output path (default: user's Downloads folder)

.EXAMPLE
    .\install.ps1
    Basic installation to Downloads folder

.EXAMPLE
    .\install.ps1 -Launch
    Install and launch the application

.EXAMPLE
    .\install.ps1 -Clean -Launch
    Clean build, install, and launch
#>

param(
    [switch]$Launch,
    [switch]$Clean,
    [string]$OutputPath = ""
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Project configuration
$ProjectPath = "src/Pick6.Loader/Pick6.Loader.csproj"
$ExeName = "pick66.exe"
$AppDisplayName = "Pick66 - Lottery Generator & Game Capture"

# Colors for output
$ColorInfo = "Cyan"
$ColorSuccess = "Green"
$ColorWarning = "Yellow"
$ColorError = "Red"

function Write-Header {
    param([string]$Title)
    Write-Host ""
    Write-Host "============================================" -ForegroundColor $ColorInfo
    Write-Host " $Title" -ForegroundColor $ColorInfo
    Write-Host "============================================" -ForegroundColor $ColorInfo
    Write-Host ""
}

function Write-Step {
    param([string]$Message)
    Write-Host "🔧 $Message" -ForegroundColor $ColorInfo
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $ColorSuccess
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor $ColorWarning
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $ColorError
}

function Test-DotNetSdk {
    try {
        $dotnetVersion = dotnet --version
        Write-Success "Found .NET SDK version: $dotnetVersion"
        
        # Check minimum version (8.0)
        if ($dotnetVersion -match "^(\d+)\.") {
            $majorVersion = [int]$matches[1]
            if ($majorVersion -lt 8) {
                throw "Minimum .NET 8 SDK required, found version $majorVersion"
            }
        }
        
        return $true
    }
    catch {
        Write-Error "Failed to detect .NET SDK: $($_.Exception.Message)"
        Write-Host "Please install .NET 8 SDK from: https://dot.net" -ForegroundColor $ColorWarning
        return $false
    }
}

function Get-OutputDirectory {
    if (![string]::IsNullOrWhiteSpace($OutputPath)) {
        return $OutputPath
    }
    
    # Get user's Downloads folder
    try {
        $downloadsPath = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile)
        $downloadsPath = Join-Path $downloadsPath "Downloads"
        
        if (!(Test-Path $downloadsPath)) {
            New-Item -Path $downloadsPath -ItemType Directory -Force | Out-Null
        }
        
        return $downloadsPath
    }
    catch {
        Write-Warning "Could not determine Downloads folder, using current directory"
        return (Get-Location).Path
    }
}

function Remove-BuildArtifacts {
    if (!$Clean) { return }
    
    Write-Step "Cleaning previous build artifacts..."
    
    try {
        # Clean standard build outputs
        $cleanPaths = @(
            "src/*/bin",
            "src/*/obj",
            "publish",
            "dist"
        )
        
        foreach ($pattern in $cleanPaths) {
            $paths = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue
            foreach ($path in $paths) {
                if (Test-Path $path) {
                    Remove-Item $path -Recurse -Force
                    Write-Host "   Removed: $($path.Name)"
                }
            }
        }
        
        Write-Success "Build artifacts cleaned"
    }
    catch {
        Write-Warning "Could not clean all artifacts: $($_.Exception.Message)"
    }
}

function Build-Project {
    Write-Step "Building project in Release mode..."
    
    try {
        $buildResult = dotnet build $ProjectPath --configuration Release --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE"
        }
        Write-Success "Build completed successfully"
    }
    catch {
        Write-Error "Build failed: $($_.Exception.Message)"
        throw
    }
}

function Publish-Application {
    param([string]$OutputDir)
    
    Write-Step "Publishing self-contained single-file executable..."
    
    try {
        # Create publish directory
        $publishDir = "publish"
        if (Test-Path $publishDir) {
            Remove-Item $publishDir -Recurse -Force
        }
        New-Item -Path $publishDir -ItemType Directory | Out-Null
        
        # Publish with optimizations
        $publishArgs = @(
            "publish"
            $ProjectPath
            "--configuration", "Release"
            "--runtime", "win-x64"
            "--self-contained", "true"
            "--output", $publishDir
            "/p:PublishSingleFile=true"
            "/p:IncludeAllContentForSelfExtract=true"
            "/p:PublishReadyToRun=true"
            "/p:DebugType=none"
            "/p:DebugSymbols=false"
            "--verbosity", "quiet"
        )
        
        $publishResult = & dotnet @publishArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Publish failed with exit code $LASTEXITCODE"
        }
        
        # Find the executable
        $exePath = Join-Path $publishDir "pick6_loader.exe"
        if (!(Test-Path $exePath)) {
            throw "Published executable not found at: $exePath"
        }
        
        # Get file size
        $fileInfo = Get-Item $exePath
        $fileSize = [math]::Round($fileInfo.Length / 1MB, 1)
        
        Write-Success "Publish completed successfully ($fileSize MB)"
        return $exePath
    }
    catch {
        Write-Error "Publish failed: $($_.Exception.Message)"
        throw
    }
}

function Install-Application {
    param(
        [string]$SourcePath,
        [string]$TargetDir
    )
    
    Write-Step "Installing to $TargetDir..."
    
    try {
        # Ensure target directory exists
        if (!(Test-Path $TargetDir)) {
            New-Item -Path $TargetDir -ItemType Directory -Force | Out-Null
        }
        
        # Copy executable
        $targetPath = Join-Path $TargetDir $ExeName
        Copy-Item $SourcePath $targetPath -Force
        
        # Verify installation
        if (!(Test-Path $targetPath)) {
            throw "Installation verification failed - file not found at target"
        }
        
        # Calculate SHA256 hash
        $hash = Get-FileHash $targetPath -Algorithm SHA256
        $hashShort = $hash.Hash.Substring(0, 16)
        
        # Get file info
        $fileInfo = Get-Item $targetPath
        $fileSize = [math]::Round($fileInfo.Length / 1MB, 1)
        
        Write-Success "Installation completed successfully"
        Write-Host ""
        Write-Host "📄 Executable Details:" -ForegroundColor $ColorInfo
        Write-Host "   Path: $targetPath"
        Write-Host "   Size: $fileSize MB"
        Write-Host "   SHA256: $hashShort..."
        Write-Host ""
        Write-Host "🚀 Ready for use!" -ForegroundColor $ColorSuccess
        Write-Host "   • No dependencies required"
        Write-Host "   • Runs on Windows 10/11"
        Write-Host "   • Self-contained .NET runtime"
        
        return $targetPath
    }
    catch {
        Write-Error "Installation failed: $($_.Exception.Message)"
        throw
    }
}

function Launch-Application {
    param([string]$ExePath)
    
    if (!$Launch) { return }
    
    Write-Step "Launching application..."
    
    try {
        Start-Process $ExePath
        Write-Success "Application launched successfully"
    }
    catch {
        Write-Warning "Could not launch application: $($_.Exception.Message)"
        Write-Host "You can manually launch it from: $ExePath"
    }
}

function Show-Usage {
    Write-Host ""
    Write-Host "💡 Usage Examples:" -ForegroundColor $ColorInfo
    Write-Host "   $ExeName                    # GUI mode (default)"
    Write-Host "   $ExeName --help             # Show help"
    Write-Host "   $ExeName --fps 144          # Set target FPS"
    Write-Host "   $ExeName --auto-start       # Auto-start capture/projection"
    Write-Host ""
}

# Main execution
try {
    Write-Header $AppDisplayName
    Write-Host "Installation Script v1.0" -ForegroundColor $ColorInfo
    
    # Check prerequisites
    if (!(Test-DotNetSdk)) {
        exit 1
    }
    
    # Check project exists
    if (!(Test-Path $ProjectPath)) {
        Write-Error "Project file not found: $ProjectPath"
        Write-Host "Please run this script from the repository root directory." -ForegroundColor $ColorWarning
        exit 1
    }
    
    # Get output directory
    $outputDirectory = Get-OutputDirectory
    Write-Host "Target directory: $outputDirectory" -ForegroundColor $ColorInfo
    
    # Execute build pipeline
    Remove-BuildArtifacts
    Build-Project
    $publishedExe = Publish-Application $outputDirectory
    $installedExe = Install-Application $publishedExe $outputDirectory
    Launch-Application $installedExe
    Show-Usage
    
    Write-Host ""
    Write-Success "Installation completed successfully! 🎉"
    
    exit 0
}
catch {
    Write-Host ""
    Write-Error "Installation failed: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "💡 Troubleshooting:" -ForegroundColor $ColorWarning
    Write-Host "   • Ensure .NET 8 SDK is installed"
    Write-Host "   • Run from the repository root directory"
    Write-Host "   • Check that all project files exist"
    Write-Host "   • Try running with -Clean flag"
    Write-Host ""
    
    exit 1
}