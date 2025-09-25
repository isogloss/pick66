@echo off
REM Pick66 Simple Installer
REM This script builds and installs Pick66 from the local repository

setlocal enabledelayedexpansion

echo.
echo ========================================
echo         Pick66 Installer v2.1
echo ========================================
echo.
echo Building Pick66 from local sources...
echo.

REM Check if we're in the correct directory
if not exist "src\Pick6.Loader\Pick6.Loader.csproj" (
    echo ERROR: Pick6.Loader project not found!
    echo Make sure you're running this from the Pick66 repository root.
    echo.
    pause
    exit /b 1
)

REM Check if .NET is available
echo [1/4] Checking .NET availability...
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: .NET 8 SDK is required but not found.
    echo.
    echo Please install .NET 8 SDK from: 
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    echo After installation, restart this script.
    echo.
    pause
    exit /b 1
)

REM Check .NET version is 8.0 or higher
for /f "tokens=1 delims=." %%a in ('dotnet --version') do set DOTNET_MAJOR=%%a
if %DOTNET_MAJOR% LSS 8 (
    echo ERROR: .NET 8 or higher is required.
    echo Found version: 
    dotnet --version
    echo.
    echo Please install .NET 8 SDK from: 
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)
echo     ✓ .NET SDK found (version: 
dotnet --version
echo )

REM Restore dependencies first (faster incremental builds)
echo.
echo [2/4] Restoring dependencies...
dotnet restore --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Failed to restore dependencies!
    echo Please check your internet connection and try again.
    echo.
    pause
    exit /b 1
)
echo     ✓ Dependencies restored

REM Build the application with optimized settings
echo.
echo [3/4] Building application...
set "OUTPUT_DIR=%USERPROFILE%\Desktop\Pick66"
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --no-restore ^
    --verbosity quiet ^
    --output "!OUTPUT_DIR!" ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:IncludeAllContentForSelfExtract=true

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Build failed!
    echo Please check the error messages above.
    echo.
    pause
    exit /b 1
)

REM Installation successful
echo.
echo [4/4] Installation complete!
echo     ✓ Application built successfully

REM Verify installation
if not exist "!OUTPUT_DIR!\loader.exe" (
    echo.
    echo ERROR: Installation incomplete - loader.exe not found
    pause
    exit /b 1
)

echo.
echo ========================================
echo         Installation Complete!
echo ========================================
echo.
echo Pick66 has been installed to:
echo !OUTPUT_DIR!\
echo.
echo To run Pick66:
echo 1. Navigate to your Desktop\Pick66 folder
echo 2. Run loader.exe
echo.
echo Press any key to open the installation folder...
pause >nul
explorer "!OUTPUT_DIR!"

exit /b 0