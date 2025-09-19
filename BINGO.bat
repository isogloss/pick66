@echo off
REM BINGO - Pick66 Self-Contained Installer
REM This file automatically downloads and builds Pick66 - no repository download needed!

setlocal enabledelayedexpansion

echo.
echo ===============================================
echo          PICK66 AUTO-INSTALLER v1.0
echo ===============================================
echo.
echo This installer will automatically:
echo  - Download Pick66 source code from GitHub
echo  - Build the application using .NET
echo  - Create a ready-to-run executable
echo.
echo No manual repository download required!
echo.

REM Check .NET SDK
echo [1/4] Checking .NET SDK...
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ ERROR: .NET 8 SDK is required but not found
    echo.
    echo Please download and install .NET 8 SDK from:
    echo https://dotnet.microsoft.com/en-us/download/dotnet/8.0
    echo.
    echo After installation, restart your command prompt and try again.
    echo.
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set "DOTNET_VERSION=%%i"
echo    ✅ Found .NET SDK version: !DOTNET_VERSION!

REM Check PowerShell
echo [2/4] Checking PowerShell...
powershell -Command "Write-Host 'PowerShell check'" >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ ERROR: PowerShell is required for downloading source code
    echo.
    echo PowerShell should be available by default on Windows 10/11.
    echo Please ensure PowerShell is installed and accessible from PATH.
    echo.
    pause
    exit /b 1
)
echo    ✅ PowerShell available

echo.
echo ===============================================
echo            DOWNLOADING SOURCE CODE
echo ===============================================
echo.

REM Setup temporary directories with better randomization
for /f "tokens=2 delims=." %%i in ('ping -n 1 127.0.0.1 ^| findstr "TTL"') do set "SEED=%%i"
set "TEMP_DIR=%TEMP%\Pick66_Build_!RANDOM!!SEED!"
set "ZIP_FILE=!TEMP_DIR!\pick66-main.zip"
set "SRC_DIR=!TEMP_DIR!\pick66-main"

echo Creating temporary directory...
if not exist "!TEMP_DIR!" mkdir "!TEMP_DIR!"
echo    📁 Temp dir: !TEMP_DIR!

echo.
echo [3/4] Downloading from GitHub...
echo    🌐 URL: https://github.com/isogloss/pick66/archive/refs/heads/main.zip
echo    📦 Size: ~70KB
echo.

REM Download with enhanced PowerShell command
powershell -NoProfile -ExecutionPolicy Bypass -Command "& { try { $ProgressPreference = 'SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://github.com/isogloss/pick66/archive/refs/heads/main.zip' -OutFile '!ZIP_FILE!' -UserAgent 'Pick66-Installer/1.0' -TimeoutSec 30; Write-Host 'Download completed successfully!' } catch { Write-Error $_.Exception.Message; exit 1 } }"

if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ Download failed. This could be due to:
    echo    • No internet connection
    echo    • GitHub is temporarily unavailable  
    echo    • Firewall blocking the download
    echo    • Antivirus interfering with PowerShell
    echo.
    echo Please check your internet connection and try again.
    echo You can also manually download the repository from:
    echo https://github.com/isogloss/pick66
    echo.
    pause
    if exist "!TEMP_DIR!" rmdir /s /q "!TEMP_DIR!" 2>nul
    exit /b 1
)

REM Verify download
if not exist "!ZIP_FILE!" (
    echo ❌ ZIP file was not created
    pause
    if exist "!TEMP_DIR!" rmdir /s /q "!TEMP_DIR!" 2>nul
    exit /b 1
)

echo    ✅ Download successful

echo.
echo Extracting source code...
powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Expand-Archive -Path '!ZIP_FILE!' -DestinationPath '!TEMP_DIR!' -Force; Write-Host 'Extraction completed!' } catch { Write-Error $_.Exception.Message; exit 1 }"

if %ERRORLEVEL% neq 0 (
    echo ❌ Failed to extract ZIP file
    pause
    if exist "!TEMP_DIR!" rmdir /s /q "!TEMP_DIR!" 2>nul
    exit /b 1
)

REM Verify extraction
if not exist "!SRC_DIR!\src\Pick6.Loader\Pick6.Loader.csproj" (
    echo ❌ Project files not found after extraction
    echo Expected: !SRC_DIR!\src\Pick6.Loader\Pick6.Loader.csproj
    pause
    if exist "!TEMP_DIR!" rmdir /s /q "!TEMP_DIR!" 2>nul
    exit /b 1
)

echo    ✅ Extraction successful

echo.
echo ===============================================
echo              BUILDING APPLICATION  
echo ===============================================
echo.
echo [4/4] Building Pick66...
echo This may take 2-3 minutes depending on your system...
echo.

REM Create dist directory if it doesn't exist
if not exist "dist" mkdir "dist"

REM Build with detailed output for user feedback
dotnet publish "!SRC_DIR!\src\Pick6.Loader\Pick6.Loader.csproj" --configuration Release --runtime win-x64 --self-contained true --output "dist" --verbosity normal --nologo

if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ Build failed
    echo.
    echo This could be due to:
    echo  • Missing build tools or SDK components
    echo  • Corrupted source files
    echo  • Insufficient disk space
    echo.
    echo Please check the error messages above for more details.
    echo.
    pause
    if exist "!TEMP_DIR!" rmdir /s /q "!TEMP_DIR!" 2>nul
    exit /b 1
)

REM Verify build output
if not exist "dist\loader.exe" (
    echo ❌ loader.exe was not created
    echo Build completed but executable is missing
    pause
    if exist "!TEMP_DIR!" rmdir /s /q "!TEMP_DIR!" 2>nul
    exit /b 1
)

echo.
echo    ✅ Build successful!

echo.
echo Cleaning up temporary files...
if exist "!TEMP_DIR!" (
    rmdir /s /q "!TEMP_DIR!" 2>nul
    if %ERRORLEVEL% equ 0 (
        echo    ✅ Cleanup completed
    ) else (
        echo    ⚠ Note: Some temporary files may remain in !TEMP_DIR!
        echo      You can manually delete this folder if needed
    )
)

echo.
echo ===============================================
echo              🎉 SUCCESS! 🎉
echo ===============================================
echo.
echo Pick66 has been successfully built and installed!
echo.
echo 📁 Location: %CD%\dist\loader.exe
echo 💾 Size: 
for %%A in (dist\loader.exe) do echo    %%~zA bytes

echo.
echo 🚀 To run Pick66:
echo    dist\loader.exe
echo.
echo 📖 For usage instructions and help:
echo    https://github.com/isogloss/pick66
echo.
pause