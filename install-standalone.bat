@echo off
setlocal enabledelayedexpansion

echo ===============================================
echo            Pick66 - One-Click Installer
echo        Windows x64 Self-Contained Install
echo          No .NET SDK Installation Required!
echo ===============================================
echo.

REM Check for internet connectivity (optional - for downloads)
ping -n 1 github.com >nul 2>&1
set INTERNET_AVAILABLE=%ERRORLEVEL%

REM Get the user's Downloads folder
for /f "tokens=2*" %%i in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders" /v "{374DE290-123F-4565-9164-39C4925E467B}" 2^>nul') do set "DOWNLOADS_FOLDER=%%j"
if not defined DOWNLOADS_FOLDER (
    set "DOWNLOADS_FOLDER=%USERPROFILE%\Downloads"
)

REM Create Pick66 folder in Downloads
set "INSTALL_DIR=%DOWNLOADS_FOLDER%\Pick66"
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo [1/3] Setting up installation directory...
echo Installation path: %INSTALL_DIR%
echo.

REM Check if we have a pre-built executable embedded in this directory
set "SOURCE_EXE=%~dp0pick6_loader.exe"
if exist "%SOURCE_EXE%" (
    echo [2/3] Found pre-built executable, copying to installation directory...
    copy "%SOURCE_EXE%" "%INSTALL_DIR%\pick6_loader.exe" >nul
    if %ERRORLEVEL% neq 0 (
        echo ERROR: Failed to copy executable!
        pause
        exit /b 1
    )
    goto :installation_complete
)

REM Try to build from source if .NET SDK is available
echo [2/3] Looking for existing build or .NET SDK...
dotnet --version >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo Found .NET SDK, attempting to build from source...
    echo This may take a few minutes...
    
    dotnet clean >nul 2>&1
    dotnet restore >nul 2>&1
    if %ERRORLEVEL% neq 0 (
        echo WARNING: Package restore failed, trying alternative method...
        goto :try_download
    )
    
    dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
        --configuration Release ^
        --runtime win-x64 ^
        --self-contained true ^
        --output dist ^
        -p:PublishSingleFile=true ^
        -p:IncludeNativeLibrariesForSelfExtract=true ^
        -p:PublishReadyToRun=true ^
        -p:DebugType=none >nul 2>&1
        
    if %ERRORLEVEL% equ 0 (
        if exist "dist\pick6_loader.exe" (
            copy "dist\pick6_loader.exe" "%INSTALL_DIR%\pick6_loader.exe" >nul
            if %ERRORLEVEL% equ 0 goto :installation_complete
        )
    )
    echo Build from source failed, trying alternative method...
)

:try_download
REM If internet is available, try to download from releases
if %INTERNET_AVAILABLE% neq 0 (
    echo ERROR: No pre-built executable found and no internet connection available.
    echo.
    echo To install Pick66, you have these options:
    echo   1. Download the full release package with pre-built executable
    echo   2. Install .NET 8 SDK and run this installer again
    echo   3. Connect to the internet to download the latest version
    echo.
    pause
    exit /b 1
)

echo Attempting to download latest release from GitHub...
REM Try to use PowerShell to download if available
powershell -Command "& {[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12; try { $latest = (Invoke-RestMethod 'https://api.github.com/repos/isogloss/pick66/releases/latest'); $asset = $latest.assets | Where-Object {$_.name -like '*pick6_loader.exe'}; if($asset) { Invoke-WebRequest $asset.browser_download_url -OutFile '%INSTALL_DIR%\pick6_loader.exe' } else { exit 1 } } catch { exit 1 }}" >nul 2>&1

if exist "%INSTALL_DIR%\pick6_loader.exe" (
    echo Downloaded successfully!
    goto :installation_complete
)

echo ERROR: Could not download or build Pick66 executable.
echo Please visit https://github.com/isogloss/pick66/releases for manual download.
pause
exit /b 1

:installation_complete
echo [3/3] Finalizing installation...

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