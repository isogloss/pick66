#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pick66 Unified Cross-Platform Installer / Builder

.DESCRIPTION
    Cross-platform installer for Pick66 that works on Windows, Linux, and macOS.
    Automatically detects the platform and builds the appropriate Windows executable.

.PARAMETER Mode
    Installation mode:
    - "full" (default): Full clean publish install
    - "fast": Fast incremental publish (falls back to full on failure)  
    - "build": Restore + build only (no publish/install)

.PARAMETER OutputPath
    Custom output directory path. If not specified, uses PICK66_OUTPUT environment 
    variable or defaults to ~/Desktop/Pick66 (or %USERPROFILE%\Desktop\Pick66 on Windows)

.EXAMPLE
    ./install.ps1
    ./install.ps1 -Mode fast
    ./install.ps1 -Mode build
    ./install.ps1 -OutputPath "/custom/path"

.NOTES
    Requires:
    - .NET 8 SDK or higher
    - PowerShell Core (pwsh)
    
    Environment Variables:
    - PICK66_OUTPUT: Override default output directory
#>

param(
    [ValidateSet("full", "fast", "build")]
    [string]$Mode = "full",
    
    [string]$OutputPath = ""
)

# Set error action preference for better error handling
$ErrorActionPreference = "Stop"

# Console output functions
function Write-Step {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "    ✓ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "ERROR: $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "WARNING: $Message" -ForegroundColor Yellow
}

# Header
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "          Pick66 Installer v3.0" -ForegroundColor Cyan
Write-Host "         Cross-Platform Edition" -ForegroundColor Cyan
Write-Host "            Mode: $Mode" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Platform detection
$IsWindowsOS = ($IsWindows -eq $true) -or ($env:OS -eq "Windows_NT")
$IsLinuxOS = ($IsLinux -eq $true)
$IsMacOSOS = ($IsMacOS -eq $true)

Write-Host "Platform: " -NoNewline
if ($IsWindowsOS) {
    Write-Host "Windows" -ForegroundColor Green
} elseif ($IsLinuxOS) {
    Write-Host "Linux" -ForegroundColor Green
} elseif ($IsMacOSOS) {
    Write-Host "macOS" -ForegroundColor Green
} else {
    Write-Host "Unknown" -ForegroundColor Yellow
}

# Step 1: Validate project location
Write-Step "[1/5] Validating project structure..."

$ProjectFile = "src/Pick6.Loader/Pick6.Loader.csproj"
if (-not (Test-Path $ProjectFile)) {
    Write-Error "src/Pick6.Loader/Pick6.Loader.csproj not found."
    Write-Host "Run this from the repository root." -ForegroundColor Yellow
    Write-Host ""
    if ($IsWindowsOS) {
        pause
    } else {
        Read-Host "Press Enter to continue"
    }
    exit 1
}

Write-Success "Project structure validated"

# Step 2: Check .NET SDK
Write-Step "[2/5] Checking .NET SDK..."

try {
    $DotNetVersion = & dotnet --version
    if ($LASTEXITCODE -ne 0) {
        throw ".NET command failed"
    }
    
    $MajorVersion = [int]($DotNetVersion.Split('.')[0])
    if ($MajorVersion -lt 8) {
        Write-Error ".NET 8 or higher required. Found: $DotNetVersion"
        Write-Host "Install from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
        if ($IsWindowsOS) {
            pause
        } else {
            Read-Host "Press Enter to continue"
        }
        exit 1
    }
    
    Write-Success ".NET SDK OK (version: $DotNetVersion)"
} catch {
    Write-Error ".NET 8 SDK required but not found."
    Write-Host "Install from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    if ($IsWindowsOS) {
        pause
    } else {
        Read-Host "Press Enter to continue"
    }
    exit 1
}

# Step 3: Determine output directory
if ($OutputPath) {
    $OutputDir = $OutputPath
} elseif ($env:PICK66_OUTPUT) {
    $OutputDir = $env:PICK66_OUTPUT
} else {
    if ($IsWindowsOS) {
        $OutputDir = Join-Path $env:USERPROFILE "Desktop\Pick66"
    } else {
        $OutputDir = Join-Path $HOME "Desktop/Pick66"
    }
}

Write-Host "Output directory: $OutputDir" -ForegroundColor Cyan

# Step 4: Restore dependencies
Write-Step "[3/5] Restoring dependencies..."

switch ($Mode.ToLower()) {
    "full" {
        Write-Host "Restoring dependencies (full)..."
        & dotnet restore --verbosity quiet --runtime win-x64
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Dependency restore failed."
            if ($IsWindowsOS) {
                pause
            } else {
                Read-Host "Press Enter to continue"
            }
            exit 1
        }
    }
    "build" {
        Write-Host "Restoring dependencies (build)..."
        & dotnet restore --verbosity quiet --runtime win-x64
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Dependency restore failed."
            if ($IsWindowsOS) {
                pause
            } else {
                Read-Host "Press Enter to continue"
            }
            exit 1
        }
    }
    "fast" {
        Write-Host "Quick dependency verification (fast)..."
        & dotnet restore --verbosity minimal --no-dependencies --runtime win-x64 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Dependency verification failed, performing full restore..."
            & dotnet restore --verbosity quiet --runtime win-x64
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Dependency restore failed."
                if ($IsWindowsOS) {
                    pause
                } else {
                    Read-Host "Press Enter to continue"
                }
                exit 1
            }
        }
    }
}

Write-Success "Dependencies ready"

# Step 5: Build or Publish
if ($Mode.ToLower() -eq "build") {
    Write-Step "[4/5] Building (no publish)..."
    # Always target Windows even when building on Linux since this is a Windows-only app
    & dotnet build $ProjectFile --configuration Release --no-restore --verbosity quiet --runtime win-x64
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed."
        if ($IsWindowsOS) {
            pause
        } else {
            Read-Host "Press Enter to continue"
        }
        exit 1
    }
    Write-Success "Build successful"
} else {
    Write-Step "[4/5] Publishing application (mode=$Mode)..."
    
    # Ensure output directory exists
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }
    
    if ($Mode.ToLower() -eq "fast") {
        # Fast incremental publish
        Write-Host "Attempting fast incremental publish..."
        & dotnet publish $ProjectFile `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            --no-restore `
            --verbosity minimal `
            --output $OutputDir `
            -p:PublishSingleFile=true `
            -p:PublishReadyToRun=false `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:IncludeAllContentForSelfExtract=true `
            /p:UseSharedCompilation=true
            
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Fast publish failed, falling back to full publish..."
            $Mode = "full"
        } else {
            Write-Success "Fast publish succeeded"
        }
    }
    
    if ($Mode.ToLower() -eq "full") {
        Write-Host "Performing full publish..."
        & dotnet publish $ProjectFile `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            --no-restore `
            --verbosity quiet `
            --output $OutputDir `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:IncludeAllContentForSelfExtract=true
            
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Publish failed."
            if ($IsWindowsOS) {
                pause
            } else {
                Read-Host "Press Enter to continue"
            }
            exit 1
        }
        Write-Success "Publish succeeded"
    }
}

# Step 6: Verify installation (skip for build-only mode)
if ($Mode.ToLower() -ne "build") {
    Write-Step "[5/5] Verifying installation..."
    
    $LoaderPath = Join-Path $OutputDir "loader.exe"
    if (-not (Test-Path $LoaderPath)) {
        Write-Error "Installation incomplete - loader.exe not found."
        if ($IsWindowsOS) {
            pause
        } else {
            Read-Host "Press Enter to continue"
        }
        exit 1
    }
    
    Write-Success "Verification passed"
}

# Completion
Write-Host ""
Write-Step "[✓] Done!"
Write-Host "Output directory: $OutputDir" -ForegroundColor Green

if ($Mode.ToLower() -ne "build") {
    Write-Host "Run loader.exe to start Pick66." -ForegroundColor Yellow
    
    # Platform-specific folder opening
    Write-Host ""
    if ($IsWindowsOS) {
        Write-Host "Press any key to open the output folder..." -ForegroundColor Cyan
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        Start-Process "explorer.exe" -ArgumentList $OutputDir
    } else {
        $OpenFolder = Read-Host "Open output folder? (y/N)"
        if ($OpenFolder -eq "y" -or $OpenFolder -eq "Y") {
            if ($IsLinuxOS) {
                if (Get-Command "xdg-open" -ErrorAction SilentlyContinue) {
                    & xdg-open $OutputDir
                } elseif (Get-Command "nautilus" -ErrorAction SilentlyContinue) {
                    & nautilus $OutputDir
                } else {
                    Write-Host "Please manually navigate to: $OutputDir" -ForegroundColor Yellow
                }
            } elseif ($IsMacOSOS) {
                & open $OutputDir
            }
        }
    }
} else {
    if ($IsWindowsOS) {
        pause
    } else {
        Read-Host "Press Enter to continue"
    }
}

Write-Host ""
exit 0