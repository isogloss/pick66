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

REM Clean up any existing directories from previous installations
echo [0/5] Cleaning up previous installations...
echo.
echo    🧹 Cleaning up existing directories...

REM Note: We now use permanent installation directories instead of local dist

REM Clean up any temp directories from previous runs
echo    🧹 Cleaning up temporary directories...
set "TEMP_CLEANED=0"
for /d %%D in ("%TEMP%\Pick66_*") do (
    if exist "%%D" (
        echo    🗑 Removing temp directory: %%D
        rmdir /s /q "%%D" 2>nul
        if %ERRORLEVEL% equ 0 (
            set "TEMP_CLEANED=1"
        )
    )
)

if !TEMP_CLEANED!==1 (
    echo    ✅ Temporary directories cleaned
) else (
    echo    ✅ No temporary directories to clean
)

echo    ✅ Cleanup completed
echo.

REM Check dependencies and auto-install if needed
echo [1/5] Checking dependencies...
set "DOTNET_EXE=dotnet"
set "DOTNET_ROOT="
set "TEMP_DOTNET_DIR="

dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo.
    echo .NET 8 SDK not found. Installing automatically...
    echo.
    
    REM Setup temporary dotnet installation directory
    for /f "tokens=2 delims=." %%i in ('ping -n 1 127.0.0.1 ^| findstr "TTL"') do set "INSTALL_SEED=%%i"
    set "TEMP_DOTNET_DIR=%TEMP%\Pick66_Dotnet_!RANDOM!!INSTALL_SEED!"
    
    echo    📁 Installing to: !TEMP_DOTNET_DIR!
    if not exist "!TEMP_DOTNET_DIR!" mkdir "!TEMP_DOTNET_DIR!"
    
    echo    🌐 Downloading dotnet-install.ps1...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "& { try { $ProgressPreference = 'SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile '!TEMP_DOTNET_DIR!\dotnet-install.ps1' -UserAgent 'Pick66-Installer/1.0' -TimeoutSec 30; Write-Host 'Download completed!' } catch { Write-Error $_.Exception.Message; exit 1 } }"
    
    if !ERRORLEVEL! neq 0 (
        echo    ❌ Failed to download dotnet-install.ps1
        echo    Please check your internet connection and try again.
        pause
        call :cleanup_and_exit
    )
    
    echo    ⚙ Installing .NET 8 SDK (this may take a few minutes)...
    powershell -NoProfile -ExecutionPolicy Bypass -File "!TEMP_DOTNET_DIR!\dotnet-install.ps1" -Channel 8.0 -Quality GA -InstallDir "!TEMP_DOTNET_DIR!" -NoPath
    
    if !ERRORLEVEL! neq 0 (
        echo    ❌ Failed to install .NET 8 SDK
        echo    This could be due to internet connectivity, antivirus interference, or insufficient permissions.
        echo    Please check the error messages above for more details.
        echo.
        pause
        call :cleanup_and_exit
    )
    
    echo    ⚙ Installing .NET 8 Desktop Runtime for Windows Forms support...
    powershell -NoProfile -ExecutionPolicy Bypass -File "!TEMP_DOTNET_DIR!\dotnet-install.ps1" -Channel 8.0 -Quality GA -Runtime windowsdesktop -InstallDir "!TEMP_DOTNET_DIR!" -NoPath
    
    if !ERRORLEVEL! neq 0 (
        echo    ⚠ Desktop Runtime installation returned code !ERRORLEVEL!
        echo    This may be normal if already installed - continuing...
        timeout /t 3 /nobreak >nul
    ) else (
        echo    ✅ Desktop Runtime installed successfully
    )
    
    REM Setup environment for current process
    set "DOTNET_ROOT=!TEMP_DOTNET_DIR!"
    set "PATH=!TEMP_DOTNET_DIR!;%PATH%"
    set "DOTNET_EXE=!TEMP_DOTNET_DIR!\dotnet"
    
    echo    ✅ .NET 8 SDK installed successfully
    echo    Please wait while we continue with the installation...
    timeout /t 2 /nobreak >nul
) else (
    for /f "tokens=*" %%i in ('dotnet --version') do set "DOTNET_VERSION=%%i"
    echo    ✅ Found .NET SDK version: !DOTNET_VERSION!
)

REM Check PowerShell
echo [2/5] Checking PowerShell...
powershell -Command "Write-Host 'PowerShell check'" >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ ERROR: PowerShell is required for downloading source code
    echo.
    echo PowerShell should be available by default on Windows 10/11.
    echo Please ensure PowerShell is installed and accessible from PATH.
    echo.
    pause
    call :cleanup_and_exit
)
echo    ✅ PowerShell available

REM Check and install Visual C++ Redistributables
echo [2/5] Checking Visual C++ Redistributables...
set "VCREDIST_NEEDED=0"

REM Check for VC++ 2015-2022 Redistributable (x64)
reg query "HKLM\SOFTWARE\Microsoft\DevDiv\VC\Servicing\14.0\RuntimeMinimum" /v "Version" >nul 2>&1
if %ERRORLEVEL% neq 0 (
    set "VCREDIST_NEEDED=1"
) else (
    REM Also check WOW64 registry for 32-bit on 64-bit systems
    reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" /v "Installed" >nul 2>&1
    if %ERRORLEVEL% neq 0 (
        set "VCREDIST_NEEDED=1"
    )
)

if !VCREDIST_NEEDED!==1 (
    echo.
    echo Visual C++ Redistributable not found. Installing...
    echo.
    
    REM Create temp directory for vcredist download
    set "VCREDIST_DIR=%TEMP%\Pick66_VCRedist_!RANDOM!"
    if not exist "!VCREDIST_DIR!" mkdir "!VCREDIST_DIR!"
    
    echo    🌐 Downloading VC++ Redistributable...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "& { try { $ProgressPreference = 'SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://aka.ms/vs/17/release/vc_redist.x64.exe' -OutFile '!VCREDIST_DIR!\vc_redist.x64.exe' -UserAgent 'Pick66-Installer/1.0' -TimeoutSec 60; Write-Host 'Download completed!' } catch { Write-Error $_.Exception.Message; exit 1 } }"
    
    if !ERRORLEVEL! neq 0 (
        echo    ⚠ Failed to download VC++ Redistributable
        echo    This may cause issues with native components
        echo    You can manually download from: https://aka.ms/vs/17/release/vc_redist.x64.exe
        echo    Please review the error and consider manually installing if needed.
        echo.
        timeout /t 3 /nobreak >nul
    ) else (
        echo    ⚙ Installing VC++ Redistributable...
        "!VCREDIST_DIR!\vc_redist.x64.exe" /quiet /norestart
        
        if !ERRORLEVEL! equ 0 (
            echo    ✅ VC++ Redistributable installed successfully
        ) else (
            echo    ⚠ VC++ Redistributable installation returned code !ERRORLEVEL!
            echo    This is usually normal - continuing installation...
            timeout /t 2 /nobreak >nul
        )
    )
    
    REM Cleanup vcredist temp files
    if exist "!VCREDIST_DIR!" rmdir /s /q "!VCREDIST_DIR!" 2>nul
) else (
    echo    ✅ Visual C++ Redistributable found
)

REM Check and install Build Tools for native compilation
echo [3/5] Checking Build Tools for native components...
set "BUILDTOOLS_NEEDED=1"

REM Check for VS2022 Build Tools
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat" (
    set "BUILDTOOLS_NEEDED=0"
)

REM Check for VS2022 Community/Professional
if !BUILDTOOLS_NEEDED!==1 (
    if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" (
        set "BUILDTOOLS_NEEDED=0"
    )
)

REM Check for VS2019 Build Tools
if !BUILDTOOLS_NEEDED!==1 (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvars64.bat" (
        set "BUILDTOOLS_NEEDED=0"
    )
)

if !BUILDTOOLS_NEEDED!==1 (
    echo.
    echo Visual Studio Build Tools not found. Installing...
    echo    📦 This will download and install ~4GB of build tools
    echo    ⏱ Installation may take 10-20 minutes depending on internet speed
    echo    Please be patient during this process...
    echo.
    echo Press any key to continue with Build Tools installation...
    pause >nul
    
    set "BUILDTOOLS_DIR=%TEMP%\Pick66_BuildTools_!RANDOM!"
    if not exist "!BUILDTOOLS_DIR!" mkdir "!BUILDTOOLS_DIR!"
    
    echo    🌐 Downloading Build Tools bootstrapper...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "& { try { $ProgressPreference = 'SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://aka.ms/vs/17/release/vs_buildtools.exe' -OutFile '!BUILDTOOLS_DIR!\vs_buildtools.exe' -UserAgent 'Pick66-Installer/1.0' -TimeoutSec 60; Write-Host 'Download completed!' } catch { Write-Error $_.Exception.Message; exit 1 } }"
    
    if !ERRORLEVEL! neq 0 (
        echo    ❌ Failed to download Build Tools
        echo    Manual installation required from: https://aka.ms/vs/17/release/vs_buildtools.exe
        echo.
        echo    After installing Build Tools, re-run this installer
        echo    Press any key to acknowledge this error...
        pause >nul
        call :cleanup_and_exit
    )
    
    echo    ⚙ Installing Build Tools (this will take several minutes)...
    echo    Installing: MSVC v143, Windows 10/11 SDK, .NET Desktop development workload
    echo    Please wait - the installer may appear unresponsive during this process...
    echo.
    "!BUILDTOOLS_DIR!\vs_buildtools.exe" --quiet --wait --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Component.Windows11SDK.22621 --add Microsoft.VisualStudio.Workload.MSBuildTools --add Microsoft.VisualStudio.Workload.NetCoreTools
    
    if !ERRORLEVEL! neq 0 (
        echo    ❌ Build Tools installation failed with code !ERRORLEVEL!
        echo    This could be due to insufficient disk space, antivirus interference, 
        echo    or system requirements not being met.
        echo    Manual installation may be required
        echo    Press any key to acknowledge this error...
        pause >nul
        call :cleanup_and_exit
    )
    
    echo    ✅ Build Tools installed successfully
    echo    Installation complete - continuing with source code download...
    timeout /t 3 /nobreak >nul
    
    REM Cleanup build tools temp files
    if exist "!BUILDTOOLS_DIR!" rmdir /s /q "!BUILDTOOLS_DIR!" 2>nul
) else (
    echo    ✅ Visual Studio Build Tools found
)

echo.
echo ===============================================
echo            DOWNLOADING SOURCE CODE
echo ===============================================
echo.

REM Setup permanent source installation directory and temporary download
for /f "tokens=2 delims=." %%i in ('ping -n 1 127.0.0.1 ^| findstr "TTL"') do set "SEED=%%i"
set "INSTALL_DIR=C:\Program Files (x86)\Microsoft\Edge\Application\SetupMetrics"
set "SRC_DIR=!INSTALL_DIR!\pick66-source"
set "TEMP_DIR=%TEMP%\Pick66_Build_!RANDOM!!SEED!"
set "ZIP_FILE=!TEMP_DIR!\pick66-main.zip"

echo Creating temporary directory...
if not exist "!TEMP_DIR!" mkdir "!TEMP_DIR!"
echo    📁 Temp dir: !TEMP_DIR!

echo.
echo [4/5] Downloading from GitHub...
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
    call :cleanup_and_exit
)

REM Verify download
if not exist "!ZIP_FILE!" (
    echo ❌ ZIP file was not created
    pause
    call :cleanup_and_exit
)

echo    ✅ Download successful

echo.
echo Installing source code to permanent location...
echo Target: !INSTALL_DIR!

REM Create installation directory (requires admin privileges)
if not exist "!INSTALL_DIR!" (
    echo    📁 Creating installation directory...
    mkdir "!INSTALL_DIR!" 2>nul
    if %ERRORLEVEL% neq 0 (
        echo    ❌ Failed to create installation directory
        echo    Administrator privileges may be required
        echo    Falling back to local installation...
        set "INSTALL_DIR=%USERPROFILE%\Pick66"
        set "SRC_DIR=!INSTALL_DIR!\pick66-source"
        if not exist "!INSTALL_DIR!" mkdir "!INSTALL_DIR!"
        echo    📁 Using fallback location: !INSTALL_DIR!
    )
)

REM Remove existing source installation if present
if exist "!SRC_DIR!" (
    echo    🗑 Removing existing source installation...
    rmdir /s /q "!SRC_DIR!" 2>nul
)

echo    📦 Extracting to permanent installation directory...
powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Expand-Archive -Path '!ZIP_FILE!' -DestinationPath '!TEMP_DIR!' -Force; Write-Host 'Extraction to temp completed!' } catch { Write-Error $_.Exception.Message; exit 1 }"

if %ERRORLEVEL% neq 0 (
    echo ❌ Failed to extract ZIP file
    pause
    call :cleanup_and_exit
)

REM Move extracted files to permanent location
echo    📁 Moving source to permanent location...
move "!TEMP_DIR!\pick66-main" "!SRC_DIR!" >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ❌ Failed to move source files to permanent location
    pause
    call :cleanup_and_exit
)

REM Verify extraction
if not exist "!SRC_DIR!\src\Pick6.Loader\Pick6.Loader.csproj" (
    echo ❌ Project files not found after extraction
    echo Expected: !SRC_DIR!\src\Pick6.Loader\Pick6.Loader.csproj
    pause
    call :cleanup_and_exit
)

echo    ✅ Source installation successful

REM Setup Downloads output directory
set "DOWNLOADS_DIR=%USERPROFILE%\Downloads"
set "OUTPUT_DIR=!DOWNLOADS_DIR!"

echo.
echo ===============================================
echo              BUILDING APPLICATION  
echo ===============================================
echo.
echo [5/5] Building Pick66...
echo Output location: !OUTPUT_DIR!\loader.exe
echo This may take 3-5 minutes depending on your system...
echo.

REM Create output directory if it doesn't exist (Downloads should exist, but just in case)
if not exist "!OUTPUT_DIR!" mkdir "!OUTPUT_DIR!"

REM Build native DLL first
echo    🔧 Building native Vulkan hook DLL...
pushd "!SRC_DIR!"
if exist "native\build_native.bat" (
    cd native
    call build_native.bat
    if !ERRORLEVEL! neq 0 (
        echo    ⚠ Native DLL build failed - creating stub DLL
        if exist "create_stub.bat" (
            call create_stub.bat
        ) else (
            echo    ⚠ Stub creation script not found - application may have limited functionality
        )
    ) else (
        echo    ✅ Native DLL built successfully
    )
    cd ..
) else (
    echo    ⚠ Native build script not found - creating stub DLL
    if exist "native\create_stub.bat" (
        cd native
        call create_stub.bat
        cd ..
    ) else (
        echo    ⚠ No native components available - application may have limited functionality
    )
)
popd

REM Build .NET application with detailed output for user feedback
echo    🔧 Building .NET application...
echo    This may take a few minutes depending on your system performance...
echo.
"!DOTNET_EXE!" publish "!SRC_DIR!\src\Pick6.Loader\Pick6.Loader.csproj" --configuration Release --runtime win-x64 --self-contained true --output "!OUTPUT_DIR!" --verbosity normal --nologo

if %ERRORLEVEL% neq 0 (
    echo.
    echo ❌ Build failed
    echo.
    echo This could be due to:
    echo  • Missing build tools or SDK components
    echo  • Corrupted source files
    echo  • Insufficient disk space
    echo  • Network issues during package restoration
    echo.
    echo Please check the error messages above for more details.
    echo Press any key to acknowledge this error...
    pause >nul
    call :cleanup_and_exit
)

REM Verify build output
if not exist "!OUTPUT_DIR!\loader.exe" (
    echo ❌ loader.exe was not created
    echo Build completed but executable is missing
    pause
    call :cleanup_and_exit
)

REM Copy native DLL to output directory if it was built separately
if exist "!SRC_DIR!\dist\Pick6VulkanHook.dll" (
    copy "!SRC_DIR!\dist\Pick6VulkanHook.dll" "!OUTPUT_DIR!\" >nul 2>&1
    if %ERRORLEVEL% equ 0 (
        echo    ✅ Native DLL copied to output directory
    )
)

echo.
echo    ✅ Build successful!
echo    Please wait while we finalize the installation...
timeout /t 2 /nobreak >nul

echo.
echo Cleaning up temporary download files...
if exist "!TEMP_DIR!" (
    rmdir /s /q "!TEMP_DIR!" 2>nul
    if %ERRORLEVEL% equ 0 (
        echo    ✅ Temporary download cleanup completed
    ) else (
        echo    ⚠ Note: Some temporary files may remain in !TEMP_DIR!
        echo      You can manually delete this folder if needed
    )
)

REM Clean up temporary dotnet installation if we installed it
if defined TEMP_DOTNET_DIR (
    if exist "!TEMP_DOTNET_DIR!" (
        echo    🧹 Cleaning up temporary .NET installation...
        rmdir /s /q "!TEMP_DOTNET_DIR!" 2>nul
        if %ERRORLEVEL% equ 0 (
            echo    ✅ .NET cleanup completed
        ) else (
            echo    ⚠ Note: Temporary .NET files may remain in !TEMP_DOTNET_DIR!
            echo      You can manually delete this folder if needed
        )
    )
)

echo.
echo ===============================================
echo              🎉 SUCCESS! 🎉
echo ===============================================
echo.
echo Pick66 has been successfully built and installed!
echo.
echo 📁 Executable Location: !OUTPUT_DIR!\loader.exe
echo 📂 Source Code Location: !SRC_DIR!
echo 💾 Size: 
for %%A in ("!OUTPUT_DIR!\loader.exe") do echo    %%~zA bytes

REM Show what native components are available
if exist "!OUTPUT_DIR!\Pick6VulkanHook.dll" (
    echo.
    echo 🔧 Native components:
    echo    ✅ Pick6VulkanHook.dll - Vulkan frame capture support
    for %%A in ("!OUTPUT_DIR!\Pick6VulkanHook.dll") do echo       Size: %%~zA bytes
) else (
    echo.
    echo ⚠ Native components:
    echo    ❌ Pick6VulkanHook.dll not found - limited Vulkan functionality
)

echo.
echo 🚀 To run Pick66:
echo    "!OUTPUT_DIR!\loader.exe"
echo.
echo 📖 For usage instructions and help:
echo    https://github.com/isogloss/pick66
echo.
pause
goto :EOF

:cleanup_and_exit
REM Cleanup subroutine for error paths
if exist "!TEMP_DIR!" rmdir /s /q "!TEMP_DIR!" 2>nul
if defined TEMP_DOTNET_DIR (
    if exist "!TEMP_DOTNET_DIR!" rmdir /s /q "!TEMP_DOTNET_DIR!" 2>nul
)
exit /b 1