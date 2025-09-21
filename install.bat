@echo off
REM Pick66 Simple Installer
REM This script builds and installs Pick66 from the local repository

setlocal enabledelayedexpansion

echo.
echo ========================================
echo         Pick66 Installer v2.0
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
echo [1/3] Checking .NET availability...
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

REM Build the application
echo.
echo [2/3] Building application...
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "%USERPROFILE%\Desktop\Pick66"

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
echo [3/3] Installation complete!
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
echo %USERPROFILE%\Desktop\Pick66\
echo.
echo To run Pick66:
echo 1. Navigate to your Desktop\Pick66 folder
echo 2. Run loader.exe
echo.
echo Press any key to open the installation folder...
pause >nul
explorer "%USERPROFILE%\Desktop\Pick66"

exit /b 0