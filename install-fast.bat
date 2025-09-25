@echo off
REM Pick66 Fast Installer (for development/repeated installs)
REM This script uses incremental building for faster installs

setlocal enabledelayedexpansion

echo.
echo ========================================
echo      Pick66 Fast Installer v2.1
echo ========================================
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
    echo Please install .NET 8 SDK and restart this script.
    pause
    exit /b 1
)
echo     ✓ .NET SDK found

REM Quick restore (only if needed)
echo.
echo [2/4] Quick dependency check...
dotnet restore --verbosity minimal --no-dependencies 2>nul
echo     ✓ Dependencies verified

REM Fast incremental build
echo.
echo [3/4] Fast incremental build...
set "OUTPUT_DIR=%USERPROFILE%\Desktop\Pick66"
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --no-restore ^
    --verbosity minimal ^
    --output "!OUTPUT_DIR!" ^
    -p:PublishSingleFile=true ^
    -p:PublishReadyToRun=false ^
    /p:UseSharedCompilation=true

if %ERRORLEVEL% neq 0 (
    echo.
    echo Fast build failed, falling back to full install...
    call install.bat
    exit /b %ERRORLEVEL%
)

REM Verify installation
echo.
echo [4/4] Verifying installation...
if not exist "!OUTPUT_DIR!\loader.exe" (
    echo ERROR: Installation incomplete - loader.exe not found
    pause
    exit /b 1
)

echo     ✓ Fast installation complete!
echo.
echo Pick66 updated at: !OUTPUT_DIR!\
echo Run loader.exe to start the application.
echo.
pause