@echo off
setlocal enabledelayedexpansion

echo ===============================================
echo            Pick66 - One-Click Installer
echo          Windows x64 Self-Contained Build
echo ===============================================
echo.

REM Check if .NET 8 SDK is installed
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: .NET 8 SDK is required but not found.
    echo Please install .NET 8 SDK from: https://dot.net
    echo.
    pause
    exit /b 1
)

echo [1/4] Cleaning previous builds...
dotnet clean >nul 2>&1

echo [2/4] Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo ERROR: Package restore failed!
    pause
    exit /b 1
)

echo [3/4] Building and publishing Pick66...
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output dist ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:PublishReadyToRun=true ^
    -p:DebugType=none

if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo [4/4] Installing to Downloads folder...

REM Get the user's Downloads folder
for /f "tokens=2*" %%i in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders" /v "{374DE290-123F-4565-9164-39C4925E467B}" 2^>nul') do set "DOWNLOADS_FOLDER=%%j"
if not defined DOWNLOADS_FOLDER (
    set "DOWNLOADS_FOLDER=%USERPROFILE%\Downloads"
)

REM Create Pick66 folder in Downloads
set "INSTALL_DIR=%DOWNLOADS_FOLDER%\Pick66"
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

REM Copy executable
copy "dist\pick6_loader.exe" "%INSTALL_DIR%\pick6_loader.exe" >nul
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to copy executable to Downloads folder!
    pause
    exit /b 1
)

REM Get file size
for %%F in ("%INSTALL_DIR%\pick6_loader.exe") do set FILE_SIZE=%%~zF
set /a FILE_SIZE_MB=%FILE_SIZE% / 1048576

echo.
echo ===============================================
echo              INSTALLATION COMPLETE!
echo ===============================================
echo.
echo Installed to: %INSTALL_DIR%\pick6_loader.exe
echo Size: %FILE_SIZE_MB% MB
echo.
echo To run Pick66:
echo   1. Navigate to: %INSTALL_DIR%
echo   2. Double-click: pick6_loader.exe
echo.
echo Or run from command line:
echo   "%INSTALL_DIR%\pick6_loader.exe"
echo.

REM Ask if user wants to launch now
set /p LAUNCH="Launch Pick66 now? (y/N): "
if /i "%LAUNCH%"=="y" (
    echo Launching Pick66...
    start "" "%INSTALL_DIR%\pick6_loader.exe"
)

echo.
echo Installation complete!
pause