#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pick66 Installation Script - Builds and installs Pick66 to Downloads folder

.DESCRIPTION
    This script builds Pick66 in Release mode, publishes a self-contained Windows x64 
    single-file executable, and installs it to the current user's Downloads folder.

    Correct invocation patterns:
    • pwsh ./install.ps1 -Launch        (PowerShell 7+ command line)
    • ./install.ps1 -Launch             (From PowerShell 7+ prompt)
    • install.cmd -Launch               (Windows wrapper - double-click safe)

.PARAMETER Launch
    Launch the application immediately after successful installation

.PARAMETER Clean
    Remove previous build artifacts before building

.PARAMETER OutputPath
    Custom output path (default: user's Downloads folder)

.PARAMETER VerboseBuild
    Enable detailed MSBuild output and diagnostics for troubleshooting build issues

.NOTES
    Prerequisites:
    - PowerShell 7.0 or later
    - .NET 8 SDK or later
    - Windows 10/11 (target platform)

    Environment Variables:
    - PICK66_LAUNCH=1 : Automatically launch after installation (same as -Launch)

.EXAMPLE
    .\install.ps1
    Basic installation to Downloads folder

.EXAMPLE
    .\install.ps1 -Launch
    Install and launch the application

.EXAMPLE
    .\install.ps1 -Clean -Launch
    Clean build, install, and launch

.EXAMPLE
    install.cmd -Launch
    Install and launch using Windows wrapper (double-click safe)

.EXAMPLE
    .\install.ps1 -VerboseBuild
    Install with detailed MSBuild output for troubleshooting
#>

param(
    [switch]$Launch,
    [switch]$Clean,
    [string]$OutputPath = "",
    [switch]$VerboseBuild
)

# PowerShell version compatibility check
function Test-PowerShellVersion {
    $psVersion = $PSVersionTable.PSVersion
    $majorVersion = $psVersion.Major
    $minorVersion = $psVersion.Minor
    
    # Block versions older than 5.1
    if ($majorVersion -lt 5 -or ($majorVersion -eq 5 -and $minorVersion -lt 1)) {
        Write-Error "PowerShell 5.1 or later is required. Current version: $($psVersion)"
        Write-Host "Please upgrade to PowerShell 7+ from: https://github.com/PowerShell/PowerShell" -ForegroundColor Red
        return $false
    }
    
    # Warn for PowerShell 5.1 and recommend 7+
    if ($majorVersion -eq 5) {
        Write-Warning "Running on Windows PowerShell $($psVersion). PowerShell 7+ is recommended for best performance and features."
        Write-Host "Download PowerShell 7+ from: https://github.com/PowerShell/PowerShell" -ForegroundColor Cyan
        Write-Host "Continuing with compatibility mode..." -ForegroundColor Yellow
        Write-Host ""
        
        # Give users a moment to see the warning
        Start-Sleep -Seconds 2
    }
    
    return $true
}

# Environment variable support - treat PICK66_LAUNCH=1 as implicit -Launch
if ($env:PICK66_LAUNCH -eq "1" -and !$Launch) {
    $Launch = $true
}

# Preflight checks for common mis-invocation scenarios
function Test-InvocationEnvironment {
    # Use the dedicated PowerShell version check
    if (!(Test-PowerShellVersion)) {
        return $false
    }
    
    # Check if running in a proper console environment
    $isProperConsole = $true
    $shouldWarn = $false
    
    try {
        # Check for common mis-invocation patterns
        if ($Host.Name -notmatch 'Console' -and [string]::IsNullOrEmpty($env:WT_SESSION) -and [Environment]::UserInteractive) {
            $isProperConsole = $false
            $shouldWarn = $true
        }
    }
    catch {
        # If we can't determine the environment, continue silently
    }
    
    if ($shouldWarn -and $isProperConsole -eq $false) {
        Write-Host ""
        Write-Warning "This script should be run from a PowerShell console for best results."
        Write-Host "Recommended usage:" -ForegroundColor $ColorInfo
        Write-Host "  pwsh -NoLogo -ExecutionPolicy Bypass -File install.ps1 -Launch" -ForegroundColor $ColorInfo
        Write-Host "  OR use the provided wrapper: install.cmd -Launch" -ForegroundColor $ColorInfo
        Write-Host ""
    }
    
    return $true
}

# Script configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Establish script root and set working directory
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptRoot

# Enhanced error handling - trap unhandled errors
trap {
    Write-Host ""
    Write-Host "[X] Unhandled Error:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Location: $($_.InvocationInfo.ScriptName):$($_.InvocationInfo.ScriptLineNumber)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "If this error persists:" -ForegroundColor Yellow
    Write-Host "   • Report this issue at: https://github.com/isogloss/pick66/issues" -ForegroundColor Yellow
    Write-Host "   • Include the full error message above" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Project configuration
$ProjectPath = Join-Path $ScriptRoot "src/Pick6.Loader/Pick6.Loader.csproj"
$ExeName = "pick6_loader.exe"
$AppDisplayName = "Pick66 - Projection Interface"

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

function Write-LogInfo {
    param([string]$Message)
    Write-Host "[*] $Message" -ForegroundColor $ColorInfo
}

function Write-LogSuccess {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor $ColorSuccess
}

function Write-LogWarn {
    param([string]$Message)
    Write-Host "[!] $Message" -ForegroundColor $ColorWarning
}

function Write-LogError {
    param([string]$Message)
    Write-Host "[X] $Message" -ForegroundColor $ColorError
}

function Test-DotNetSdk {
    try {
        $dotnetVersion = dotnet --version
        Write-LogSuccess "Found .NET SDK version: $dotnetVersion"
        
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
        Write-LogError "Failed to detect .NET SDK: $($_.Exception.Message)"
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
        Write-LogWarn "Could not determine Downloads folder, using current directory"
        return (Get-Location).Path
    }
}

function Remove-BuildArtifacts {
    if (!$Clean) { return }
    
    Write-LogInfo "Cleaning previous build artifacts..."
    
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
        
        Write-LogSuccess "Build artifacts cleaned"
    }
    catch {
        Write-LogWarn "Could not clean all artifacts: $($_.Exception.Message)"
    }
}

function Publish-Application {
    param([string]$OutputDir)
    
    Write-LogInfo "Publishing self-contained single-file executable..."
    
    try {
        # Create publish directory
        $publishDir = "publish"
        if (Test-Path $publishDir) {
            Remove-Item $publishDir -Recurse -Force
        }
        New-Item -Path $publishDir -ItemType Directory | Out-Null
        
        # Determine verbosity level
        $verbosity = if ($VerboseBuild) { "minimal" } else { "quiet" }
        
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
            "--verbosity", $verbosity
        )
        
        Write-Host "Running: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
        
        # Capture output to display errors properly
        $output = & dotnet @publishArgs 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            # If not verbose and failed, retry with minimal verbosity for diagnostics
            if (!$VerboseBuild) {
                Write-LogWarn "Publish failed, retrying with diagnostic verbosity..."
                $publishArgs[-1] = "minimal"  # Change last argument (verbosity) to minimal
                $output = & dotnet @publishArgs 2>&1
                
                if ($LASTEXITCODE -ne 0) {
                    Write-Host ""
                    Write-Host "Build/Publish Error Details:" -ForegroundColor $ColorError
                    Write-Host $output -ForegroundColor $ColorError
                    throw "Publish failed with exit code $LASTEXITCODE after retry with diagnostics"
                }
            } else {
                Write-Host ""
                Write-Host "Build/Publish Error Details:" -ForegroundColor $ColorError
                Write-Host $output -ForegroundColor $ColorError
                throw "Publish failed with exit code $LASTEXITCODE"
            }
        } else {
            # Show output if verbose mode
            if ($VerboseBuild -and $output) {
                Write-Host $output
            }
        }
        
        # Find the executable
        $exePath = Join-Path $publishDir "pick6_loader.exe"
        if (!(Test-Path $exePath)) {
            throw "Published executable not found at: $exePath"
        }
        
        # Get file size
        $fileInfo = Get-Item $exePath
        $fileSize = [math]::Round($fileInfo.Length / 1MB, 1)
        
        Write-LogSuccess ("Publish completed successfully ({0} MB)" -f $fileSize)
        return $exePath
    }
    catch {
        Write-LogError "Publish failed: $($_.Exception.Message)"
        throw
    }
}

function Install-Application {
    param(
        [string]$SourcePath,
        [string]$TargetDir
    )
    
    Write-LogInfo "Installing to $TargetDir..."
    
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
        
        Write-LogSuccess "Installation completed successfully"
        Write-Host ""
        Write-Host "Executable Details:" -ForegroundColor $ColorInfo
        Write-Host "   Path: $targetPath"
        Write-Host ("   Size: {0} MB" -f $fileSize)
        Write-Host "   SHA256: $hashShort..."
        Write-Host ""
        Write-Host "Ready for use!" -ForegroundColor $ColorSuccess
        Write-Host "   • No dependencies required"
        Write-Host "   • Runs on Windows 10/11"
        Write-Host "   • Self-contained .NET runtime"
        
        return $targetPath
    }
    catch {
        Write-LogError "Installation failed: $($_.Exception.Message)"
        throw
    }
}

function Launch-Application {
    param([string]$ExePath)
    
    if (!$Launch) { return }
    
    Write-LogInfo "Launching application..."
    
    try {
        Start-Process $ExePath
        Write-LogSuccess "Application launched successfully"
    }
    catch {
        Write-LogWarn "Could not launch application: $($_.Exception.Message)"
        Write-Host "You can manually launch it from: $ExePath"
    }
}

function Show-Usage {
    Write-Host ""
    Write-Host "Usage Examples:" -ForegroundColor $ColorInfo
    Write-Host "   $ExeName                    # GUI mode (default)"
    Write-Host "   $ExeName --help             # Show help"
    Write-Host "   $ExeName --fps 144          # Set target FPS"
    Write-Host "   $ExeName --auto-start       # Auto-start capture/projection"
    Write-Host ""
}

function Request-InteractiveLaunch {
    # Only prompt if Launch is not already set and we're in an interactive session
    if ($Launch -or !([Environment]::UserInteractive)) {
        return
    }
    
    try {
        # Check if we can actually prompt the user
        if ($Host.UI.RawUI -and $Host.Name -match 'Console') {
            Write-Host ""
            $response = Read-Host "Launch Pick66 now? (Y/N)"
            if ($response -match '^[Yy]') {
                $script:Launch = $true
                Write-LogSuccess "Launch scheduled after installation"
            }
        }
    }
    catch {
        # If we can't prompt (non-interactive context, CI, etc.), continue silently
    }
}

# Main execution
try {
    Write-Header $AppDisplayName
    Write-Host "Installation Script v1.0" -ForegroundColor $ColorInfo
    
    # Run preflight checks
    if (!(Test-InvocationEnvironment)) {
        exit 1
    }
    
    # Check prerequisites
    if (!(Test-DotNetSdk)) {
        exit 1
    }
    
    # Check project exists
    if (!(Test-Path $ProjectPath)) {
        Write-LogError "Project file not found: $ProjectPath"
        Write-Host "Script root: $ScriptRoot" -ForegroundColor $ColorWarning
        Write-Host "Resolved project path: $ProjectPath" -ForegroundColor $ColorWarning
        Write-Host "Please ensure the script is run from the repository root or all project files exist." -ForegroundColor $ColorWarning
        Write-Host "Alternative: Use the provided wrapper 'install.cmd' which handles paths automatically." -ForegroundColor $ColorWarning
        exit 1
    }
    
    # Get output directory
    $outputDirectory = Get-OutputDirectory
    Write-Host "Target directory: $outputDirectory" -ForegroundColor $ColorInfo
    
    # Ask for interactive launch if not specified
    Request-InteractiveLaunch
    
    # Execute build pipeline
    Remove-BuildArtifacts
    $publishedExe = Publish-Application $outputDirectory
    $installedExe = Install-Application $publishedExe $outputDirectory
    Launch-Application $installedExe
    Show-Usage
    
    Write-Host ""
    Write-LogSuccess "Installation completed successfully!"
    
    exit 0
}
catch {
    Write-Host ""
    Write-LogError "Installation failed: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor $ColorWarning
    Write-Host "   • Ensure .NET 8 SDK is installed"
    Write-Host "   • Run from the repository root directory OR use install.cmd"
    Write-Host "   • Check that all project files exist"
    Write-Host "   • Try running with -Clean flag"
    Write-Host "   • Use -VerboseBuild for detailed diagnostic output"
    Write-Host "   • For execution issues, use: pwsh -ExecutionPolicy Bypass -File install.ps1"
    Write-Host ""
    
    exit 1
}