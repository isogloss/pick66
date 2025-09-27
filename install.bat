@echo off
REM Pick66 Self-Contained Installer
REM Downloads source, compiles, and installs to Downloads folder
REM Automatically installs .NET 8 SDK if not present or insufficient version found

setlocal enabledelayedexpansion

REM ====================================================================
REM                        INSTALLER HEADER
REM ====================================================================
call :DisplayHeader

REM ====================================================================
REM                      SYSTEM VALIDATION
REM ====================================================================
call :ValidateWindows
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

REM ====================================================================
REM                    .NET SDK MANAGEMENT
REM ====================================================================
call :CheckAndInstallDotNet
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

REM ====================================================================
REM                   SOURCE CODE DOWNLOAD
REM ====================================================================
call :DownloadSourceCode
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

REM ====================================================================
REM                   BUILD AND DEPLOYMENT
REM ====================================================================
call :BuildAndDeploy
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

REM ====================================================================
REM                  INSTALLATION COMPLETION
REM ====================================================================
call :CompleteInstallation

exit /b 0

REM ====================================================================
REM                          FUNCTIONS
REM ====================================================================

:DisplayHeader
echo.
echo =========================================
echo          Pick66 Installer v5.0
echo           Refactored Edition
echo =========================================
echo.
echo Features:
echo  • Automatic .NET 8 SDK installation
echo  • Source download from GitHub
echo  • Complete build and deployment
echo  • No manual prerequisites required
echo.
goto :eof

:ValidateWindows
echo [1/4] Validating system requirements...
if not "%OS%"=="Windows_NT" (
    echo ERROR: This installer only works on Windows.
    echo.
    pause
    exit /b 1
)
echo     ✓ Windows OS detected
goto :eof

:CheckAndInstallDotNet
echo [2/4] Checking .NET SDK...

REM Initialize variables
set DOTNET_SUFFICIENT=0
set DOTNET_VERSION=
set PRIVATE_DOTNET_DIR=

REM Check if dotnet is available and sufficient
dotnet --version >nul 2>&1
if %ERRORLEVEL% equ 0 (
    call :ValidateExistingDotNet
) else (
    echo     .NET SDK not found
)

REM Install .NET SDK if needed
if %DOTNET_SUFFICIENT% equ 0 (
    call :InstallPrivateDotNet
    if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%
)

REM Set dotnet command
call :SetDotNetCommand
goto :eof

:ValidateExistingDotNet
for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
for /f "tokens=* delims= " %%j in ("!DOTNET_VERSION!") do set DOTNET_VERSION=%%j
if defined DOTNET_VERSION (
    for /f "tokens=1 delims=." %%a in ("!DOTNET_VERSION!") do set MAJOR_VERSION=%%a
    if defined MAJOR_VERSION (
        if !MAJOR_VERSION! geq 8 (
            set DOTNET_SUFFICIENT=1
            echo     ✓ .NET SDK OK (version: !DOTNET_VERSION!)
        ) else (
            echo     Found .NET SDK version !DOTNET_VERSION!, but need version 8 or higher
        )
    ) else (
        echo     Could not parse .NET SDK version: !DOTNET_VERSION!
    )
) else (
    echo     Could not determine .NET SDK version
)
goto :eof

:InstallPrivateDotNet
echo     .NET 8 SDK not found or insufficient version detected
echo     Bootstrapping .NET 8 SDK installation...

REM Create private installation directory
set PRIVATE_DOTNET_DIR=%TEMP%\Pick66_Build_DotNet_%RANDOM%
if exist "!PRIVATE_DOTNET_DIR!" rmdir /s /q "!PRIVATE_DOTNET_DIR!" >nul 2>&1
mkdir "!PRIVATE_DOTNET_DIR!"

REM Download installation script
echo     Downloading .NET installation script...
set DOTNET_INSTALL_SCRIPT=!PRIVATE_DOTNET_DIR!\dotnet-install.ps1
powershell -Command "try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile '!DOTNET_INSTALL_SCRIPT!' -UseBasicParsing; exit 0 } catch { Write-Host 'Error downloading install script:' $_.Exception.Message; exit 1 }" 2>&1

if %ERRORLEVEL% neq 0 (
    call :DisplayDotNetError
    exit /b 1
)

REM Install .NET 8 SDK
echo     Installing .NET 8 SDK (this may take a few minutes)...
echo     Please wait while the SDK is downloaded and installed...
echo     NOTE: If installation appears to hang, please wait - this is normal for first-time installation.

REM Use a more robust installation approach with timeout handling
powershell -ExecutionPolicy Bypass -Command "try { & '!DOTNET_INSTALL_SCRIPT!' -Channel 8.0 -Quality GA -InstallDir '!PRIVATE_DOTNET_DIR!' -NoPath -Verbose; if ($LASTEXITCODE -eq 0) { Write-Host 'Installation successful' } else { Write-Host 'Installation failed with exit code:' $LASTEXITCODE; exit $LASTEXITCODE } } catch { Write-Host 'Installation error:' $_.Exception.Message; exit 1 }" 2>&1

if %ERRORLEVEL% neq 0 (
    echo.
    echo Installation failed with error code: %ERRORLEVEL%
    call :DisplayDotNetError
    REM Try to cleanup partial installation
    if exist "!PRIVATE_DOTNET_DIR!" (
        echo     Cleaning up partial installation...
        rmdir /s /q "!PRIVATE_DOTNET_DIR!" >nul 2>&1
    )
    exit /b 1
)

REM Verify installation
call :VerifyPrivateDotNet
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%
goto :eof

:DisplayDotNetError
echo.
echo =========================================
echo    .NET SDK Installation Failed
echo =========================================
echo ERROR: Failed to install .NET 8 SDK.
echo.
echo This could be due to:
echo  - No internet connection
echo  - Firewall blocking PowerShell web requests  
echo  - Corporate proxy settings blocking downloads
echo  - Insufficient disk space in TEMP directory
echo  - Windows PowerShell execution policy restrictions
echo.
echo Troubleshooting steps:
echo  1. Check your internet connection
echo  2. Try running as Administrator
echo  3. Temporarily disable antivirus/firewall  
echo  4. Check available disk space
echo  5. Clear temporary files in %%TEMP%% directory
echo.
echo Alternatively, manually install .NET 8 SDK from:
echo https://dotnet.microsoft.com/download/dotnet/8.0
echo.
echo After manual installation, re-run this installer.
echo.
pause
goto :eof

:VerifyPrivateDotNet
set PATH=!PRIVATE_DOTNET_DIR!;!PATH!
"!PRIVATE_DOTNET_DIR!\dotnet.exe" --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: .NET SDK installation verification failed.
    echo The SDK was downloaded but may not be functioning correctly.
    pause
    exit /b 1
)
for /f "tokens=*" %%i in ('"!PRIVATE_DOTNET_DIR!\dotnet.exe" --version 2^>nul') do set DOTNET_VERSION=%%i
echo     ✓ .NET 8 SDK installed successfully (version: !DOTNET_VERSION!)
echo     Using private installation: !PRIVATE_DOTNET_DIR!
goto :eof

:SetDotNetCommand
if defined PRIVATE_DOTNET_DIR (
    set DOTNET_CMD="!PRIVATE_DOTNET_DIR!\dotnet.exe"
) else (
    set DOTNET_CMD=dotnet
)
goto :eof

:DownloadSourceCode
echo [3/4] Downloading source code...

REM Create temporary directory
set TEMP_DIR=%TEMP%\Pick66_Build_%RANDOM%
if exist "%TEMP_DIR%" rmdir /s /q "%TEMP_DIR%" >nul 2>&1
mkdir "%TEMP_DIR%"

REM Try git first, then fallback to PowerShell
git --version >nul 2>&1
if %ERRORLEVEL% equ 0 (
    call :DownloadWithGit
) else (
    call :DownloadWithPowerShell
)

if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

REM Validate downloaded source
call :ValidateSourceCode
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo     ✓ Source code downloaded
goto :eof

:DownloadWithGit
echo     Using git to download source...
git clone https://github.com/isogloss/pick66.git "%TEMP_DIR%\pick66" >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to clone repository.
    echo Make sure you have internet access and git is installed.
    pause
    exit /b 1
)
set SOURCE_DIR=%TEMP_DIR%\pick66
goto :eof

:DownloadWithPowerShell
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
goto :eof

:ValidateSourceCode
if not exist "%SOURCE_DIR%\src\Pick6.Loader\Pick6.Loader.csproj" (
    echo ERROR: Downloaded source appears to be incomplete.
    echo Expected project file not found.
    pause
    exit /b 1
)
goto :eof

:BuildAndDeploy
echo [4/4] Building Pick66 Loader...

REM Setup output directory with error checking
call :SetupOutputDirectory
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

REM Build .NET application with error handling
call :BuildDotNetApplication
if %ERRORLEVEL% neq 0 (
    echo.
    echo Build failed. Attempting cleanup before exit...
    call :CleanupTempFiles
    exit /b %ERRORLEVEL%
)

REM Build C++ proxy DLLs (non-critical, warnings only)
call :BuildProxyDlls

REM Verify installation before declaring success
call :VerifyInstallation
if %ERRORLEVEL% neq 0 (
    echo.
    echo Verification failed. Build may have completed but files are missing.
    call :CleanupTempFiles
    exit /b %ERRORLEVEL%
)

echo     ✓ Build process complete
goto :eof

:SetupOutputDirectory
set OUTPUT_DIR=%USERPROFILE%\Downloads\Pick66
echo     Output directory: %OUTPUT_DIR%
if exist "%OUTPUT_DIR%" (
    echo     Cleaning existing installation...
    rmdir /s /q "%OUTPUT_DIR%" >nul 2>&1
    REM Give the system time to release file handles
    timeout /t 1 /nobreak >nul 2>&1
)
mkdir "%OUTPUT_DIR%" >nul 2>&1
if not exist "%OUTPUT_DIR%" (
    echo ERROR: Failed to create output directory: %OUTPUT_DIR%
    echo This could be due to:
    echo  - Insufficient permissions
    echo  - Path length limitations  
    echo  - Disk space issues
    echo  - Antivirus blocking directory creation
    pause
    exit /b 1
)
echo     ✓ Output directory ready
goto :eof

:BuildDotNetApplication
cd /d "%SOURCE_DIR%"

echo     Restoring .NET dependencies...
%DOTNET_CMD% restore src\Pick6.Loader\Pick6.Loader.csproj --verbosity quiet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to restore dependencies.
    echo Retrying with verbose output to show detailed error...
    echo.
    %DOTNET_CMD% restore src\Pick6.Loader\Pick6.Loader.csproj --verbosity normal
    echo.
    echo Make sure you have internet access for NuGet packages.
    pause
    exit /b 1
)

echo     Building and publishing...
%DOTNET_CMD% publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --verbosity quiet ^
    --output "%OUTPUT_DIR%" ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:IncludeAllContentForSelfExtract=true ^
    -p:EnableWindowsTargeting=true >nul 2>&1

if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed.
    echo Retrying with verbose output to show detailed error...
    echo.
    %DOTNET_CMD% publish src\Pick6.Loader\Pick6.Loader.csproj ^
        --configuration Release ^
        --runtime win-x64 ^
        --self-contained true ^
        --verbosity normal ^
        --output "%OUTPUT_DIR%" ^
        -p:PublishSingleFile=true ^
        -p:IncludeNativeLibrariesForSelfExtract=true ^
        -p:IncludeAllContentForSelfExtract=true ^
        -p:EnableWindowsTargeting=true
    echo.
    echo Please check that .NET 8 SDK is properly installed and OutputDir is writable.
    echo Output directory: "%OUTPUT_DIR%"
    pause
    exit /b 1
)
echo     ✓ .NET build successful
goto :eof

:BuildProxyDlls
echo     Building C++ proxy DLLs...
cd /d "%SOURCE_DIR%\src\Pick6.ProxyDLL"

REM Check for compiler availability
call :CheckCppCompiler
if %ERRORLEVEL% neq 0 goto :skip_proxy_build

REM Build all proxy DLLs
call :CompileProxyDlls

REM Copy DLLs to output
if exist "bin\*.dll" (
    copy bin\*.dll "%OUTPUT_DIR%" >nul 2>&1
    echo     ✓ Proxy DLLs copied to output
)

REM Cleanup build artifacts
call :CleanupBuildArtifacts
goto :eof

:skip_proxy_build
echo     WARNING: Skipping proxy DLL build due to missing compiler
goto :eof

:CheckCppCompiler
cl >nul 2>&1
if %ERRORLEVEL% neq 0 (
    call :InstallBuildTools
    if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%
)
goto :eof

:InstallBuildTools
echo     Visual C++ compiler not found, attempting to download Build Tools...

set BUILD_TOOLS_URL=https://aka.ms/vs/17/release/vs_buildtools.exe
set BUILD_TOOLS_EXE=%TEMP_DIR%\vs_buildtools.exe

echo     Downloading Visual Studio Build Tools...
powershell -Command "try { Invoke-WebRequest -Uri '%BUILD_TOOLS_URL%' -OutFile '%BUILD_TOOLS_EXE%'; exit 0 } catch { exit 1 }" >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo     WARNING: Failed to download Build Tools. Proxy DLLs will not be built.
    echo     You can manually install Visual Studio Build Tools later and run:
    echo     "%SOURCE_DIR%\src\Pick6.ProxyDLL\build_proxies.bat"
    exit /b 1
)

echo     Installing Visual Studio Build Tools (this may take several minutes)...
echo     Please wait for the installation to complete...
"%BUILD_TOOLS_EXE%" --quiet --wait --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Component.VC.Tools.x86.x64 --add Microsoft.VisualStudio.Component.Windows10SDK.20348
if %ERRORLEVEL% neq 0 (
    echo     WARNING: Build Tools installation failed. Proxy DLLs will not be built.
    exit /b 1
)

REM Locate and configure VS tools
for /f "usebackq delims=" %%i in (`dir /b /s "C:\Program Files*\Microsoft Visual Studio\*\BuildTools\VC\Auxiliary\Build\vcvars64.bat" 2^>nul`) do (
    call "%%i" >nul 2>&1
    goto :found_vcvars
)

:found_vcvars
cl >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo     WARNING: Could not configure C++ compiler. Proxy DLLs will not be built.
    exit /b 1
)
goto :eof

:CompileProxyDlls
if not exist "bin" mkdir bin

REM Build dxgi.dll proxy
cl /LD /MT dxgi_proxy_template.cpp /Fe:bin\dxgi.dll /link /SUBSYSTEM:WINDOWS /MACHINE:X64 >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo     ✓ dxgi.dll proxy built
) else (
    echo     WARNING: Failed to build dxgi.dll proxy
)

REM Build d3d11.dll proxy
copy dxgi_proxy_template.cpp d3d11_proxy_template.cpp >nul 2>&1
powershell -Command "(gc d3d11_proxy_template.cpp) -replace 'dxgi', 'd3d11' | Out-File -encoding ASCII d3d11_proxy_template.cpp" >nul 2>&1
cl /LD /MT d3d11_proxy_template.cpp /Fe:bin\d3d11.dll /link /SUBSYSTEM:WINDOWS /MACHINE:X64 >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo     ✓ d3d11.dll proxy built
) else (
    echo     WARNING: Failed to build d3d11.dll proxy
)

REM Build vulkan-1.dll proxy
copy dxgi_proxy_template.cpp vulkan_proxy_template.cpp >nul 2>&1
powershell -Command "(gc vulkan_proxy_template.cpp) -replace 'dxgi', 'vulkan-1' -replace 'CreateDXGIFactory', 'vkCreateInstance' | Out-File -encoding ASCII vulkan_proxy_template.cpp" >nul 2>&1
cl /LD /MT vulkan_proxy_template.cpp /Fe:bin\vulkan-1.dll /link /SUBSYSTEM:WINDOWS /MACHINE:X64 >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo     ✓ vulkan-1.dll proxy built
) else (
    echo     WARNING: Failed to build vulkan-1.dll proxy
)
goto :eof

:CleanupBuildArtifacts
del *.obj >nul 2>&1
del *.exp >nul 2>&1
del *.lib >nul 2>&1
del d3d11_proxy_template.cpp >nul 2>&1
del vulkan_proxy_template.cpp >nul 2>&1
goto :eof

:VerifyInstallation
echo     Verifying installation...
if not exist "%OUTPUT_DIR%\loader.exe" (
    echo ERROR: Installation failed - loader.exe not found.
    echo Expected location: "%OUTPUT_DIR%\loader.exe"
    echo.
    echo Contents of output directory:
    if exist "%OUTPUT_DIR%" (
        dir "%OUTPUT_DIR%" 2>nul
    ) else (
        echo Output directory does not exist!
    )
    echo.
    echo This usually indicates a build failure that was not detected.
    echo Please review the build output above for any error messages.
    pause
    exit /b 1
)
echo     ✓ Installation verified
goto :eof

:CompleteInstallation
REM Cleanup temporary files (don't fail if cleanup fails)
call :CleanupTempFiles

REM Display success message
call :DisplaySuccess

REM Open Downloads folder (optional, don't fail if explorer fails) 
echo Press any key to open the Downloads folder...
pause >nul
explorer "%OUTPUT_DIR%" >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Note: Could not automatically open Downloads folder.
    echo Please navigate to: %OUTPUT_DIR%
)
goto :eof

:CleanupTempFiles
echo     Cleaning up temporary files...
cd /d "%USERPROFILE%" >nul 2>&1
if exist "%TEMP_DIR%" (
    echo     Removing build directory: %TEMP_DIR%
    REM Use multiple attempts to handle locked files
    rmdir /s /q "%TEMP_DIR%" >nul 2>&1
    if exist "%TEMP_DIR%" (
        timeout /t 2 /nobreak >nul 2>&1
        rmdir /s /q "%TEMP_DIR%" >nul 2>&1
    )
    if exist "%TEMP_DIR%" (
        echo     WARNING: Could not fully remove temp directory, some files may remain
    )
)
if defined PRIVATE_DOTNET_DIR (
    if exist "%PRIVATE_DOTNET_DIR%" (
        echo     Cleaning up private .NET installation...
        rmdir /s /q "%PRIVATE_DOTNET_DIR%" >nul 2>&1
        if exist "%PRIVATE_DOTNET_DIR%" (
            timeout /t 2 /nobreak >nul 2>&1
            rmdir /s /q "%PRIVATE_DOTNET_DIR%" >nul 2>&1
        )
        if exist "%PRIVATE_DOTNET_DIR%" (
            echo     WARNING: Could not fully remove .NET temp directory, some files may remain
        )
    )
)
goto :eof

:DisplaySuccess
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
goto :eof