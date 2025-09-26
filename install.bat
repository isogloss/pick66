@echo off
REM Pick66 Self-Contained Installer
REM Downloads source, compiles, and installs to Downloads folder

setlocal enabledelayedexpansion

echo.
echo =========================================
echo          Pick66 Installer v4.0
echo        Self-Contained Edition
echo =========================================
echo.

REM Check if we're on Windows
if not "%OS%"=="Windows_NT" (
    echo ERROR: This installer only works on Windows.
    echo.
    pause
    exit /b 1
)

REM Check for .NET SDK
echo [1/4] Checking .NET SDK...
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: .NET SDK not found.
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

REM Get .NET version and validate
for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
for /f "tokens=1 delims=." %%a in ("%DOTNET_VERSION%") do set MAJOR_VERSION=%%a
if %MAJOR_VERSION% lss 8 (
    echo ERROR: .NET 8 or higher required. Found: %DOTNET_VERSION%
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)
echo     ✓ .NET SDK OK (version: %DOTNET_VERSION%)

REM Create temporary directory for source download
set TEMP_DIR=%TEMP%\Pick66_Build_%RANDOM%
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%" >nul 2>&1
mkdir "%TEMP_DIR%"

REM Check for git or download tools
echo.
echo [2/4] Downloading source code...
git --version >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo     Using git to download source...
    git clone https://github.com/isogloss/pick66.git "%TEMP_DIR%\pick66" >nul 2>&1
    if %ERRORLEVEL% neq 0 (
        echo ERROR: Failed to clone repository.
        echo Make sure you have internet access and git is installed.
        pause
        exit /b 1
    )
    set SOURCE_DIR=%TEMP_DIR%\pick66
) else (
    REM Try PowerShell for download if git not available
    echo     Using PowerShell to download source...
    powershell -Command "try { Invoke-WebRequest -Uri 'https://github.com/isogloss/pick66/archive/refs/heads/main.zip' -OutFile '%TEMP_DIR%\source.zip'; Expand-Archive -Path '%TEMP_DIR%\source.zip' -DestinationPath '%TEMP_DIR%'; exit 0 } catch { exit 1 }" >nul 2>&1
    if %ERRORLEVEL% neq 0 (
        echo ERROR: Failed to download source code.
        echo Please ensure you have internet access.
        echo Consider installing git for more reliable downloads.
        pause
        exit /b 1
    )
    set SOURCE_DIR=%TEMP_DIR%\pick66-main
)

if not exist "%SOURCE_DIR%\src\Pick6.Loader\Pick6.Loader.csproj" (
    echo ERROR: Downloaded source appears to be incomplete.
    echo Expected project file not found.
    pause
    exit /b 1
)
echo     ✓ Source code downloaded

REM Set output directory to user's Downloads folder
set OUTPUT_DIR=%USERPROFILE%\Downloads\Pick66
if exist "%OUTPUT_DIR%" (
    echo     Cleaning existing installation...
    rmdir /s /q "%OUTPUT_DIR%" >nul 2>&1
)
mkdir "%OUTPUT_DIR%" >nul 2>&1

echo.
echo [3/4] Building Pick66 Loader...
echo     Output directory: %OUTPUT_DIR%
cd /d "%SOURCE_DIR%"

REM Restore dependencies
dotnet restore --verbosity quiet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to restore dependencies.
    pause
    exit /b 1
)

REM Build and publish
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --verbosity quiet ^
    --output "%OUTPUT_DIR%" ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:IncludeAllContentForSelfExtract=true >nul 2>&1

if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed.
    echo.
    pause
    exit /b 1
)
echo     ✓ Build successful

REM Verify installation
echo.
echo [4/4] Verifying installation...
if not exist "%OUTPUT_DIR%\loader.exe" (
    echo ERROR: Installation failed - loader.exe not found.
    pause
    exit /b 1
)
echo     ✓ Installation verified

REM Clean up temporary files
cd /d "%USERPROFILE%"
rmdir /s /q "%TEMP_DIR%" >nul 2>&1

REM Success
echo.
echo =========================================
echo           Installation Complete!
echo =========================================
echo.
echo Pick66 Loader has been installed to:
echo %OUTPUT_DIR%\loader.exe
echo.
echo Run loader.exe to start Pick66.
echo.
echo Press any key to open the Downloads folder...
pause >nul
explorer "%OUTPUT_DIR%"

exit /b 0