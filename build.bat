@echo off
setlocal enabledelayedexpansion

echo ============================================================
echo Pick6 Complete Build Script
echo Builds native components and prepares loader for deployment
echo ============================================================
echo.

:: Save starting directory
set START_DIR=%CD%

:: Check prerequisites
call :CheckPrerequisites
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

:: Build native DLL
call :BuildNativeDll
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

:: Copy DLLs to Loader project
call :CopyDllsToLoader
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

:: Show completion summary
call :ShowSummary

echo.
echo Build completed successfully!
exit /b 0

::=============================================================
:: FUNCTIONS
::=============================================================

:CheckPrerequisites
echo [1/3] Checking prerequisites...

:: Check for .NET SDK
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: .NET SDK not found. Please install .NET 8 SDK.
    echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    exit /b 1
)
echo   [OK] .NET SDK found

:: Check for CMake (for native build)
where cmake >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo WARNING: CMake not found. Native DLL build will be skipped.
    echo If you need Pick6Native.dll, install CMake from: https://cmake.org/download/
    set SKIP_NATIVE=1
) else (
    echo   [OK] CMake found
    set SKIP_NATIVE=0
)

echo.
goto :eof

:BuildNativeDll
echo [2/3] Building native components...

if %SKIP_NATIVE%==1 (
    echo   Skipping native DLL build (CMake not available)
    echo   Note: Pick6Native.dll provides better injection compatibility
    goto :eof
)

:: Navigate to native project
cd /d "%START_DIR%\src\Pick6.Native"

:: Check if build.bat exists
if not exist "build.bat" (
    echo ERROR: build.bat not found in src\Pick6.Native
    cd /d "%START_DIR%"
    exit /b 1
)

:: Run the native build script
echo   Building Pick6Native.dll...
call build.bat
if %ERRORLEVEL% neq 0 (
    echo ERROR: Native DLL build failed
    cd /d "%START_DIR%"
    exit /b 1
)

cd /d "%START_DIR%"
echo   [OK] Native DLL build completed
echo.
goto :eof

:CopyDllsToLoader
echo [3/3] Copying DLLs to Loader project...

set LOADER_DIR=%START_DIR%\src\Pick6.Loader
set NATIVE_BUILD_DIR=%START_DIR%\src\Pick6.Native\build\Release

:: Create loader directory if it doesn't exist
if not exist "%LOADER_DIR%" (
    echo ERROR: Loader directory not found: %LOADER_DIR%
    exit /b 1
)

:: Copy Pick6Native.dll if it exists
if exist "%NATIVE_BUILD_DIR%\Pick6Native.dll" (
    echo   Copying Pick6Native.dll...
    copy /Y "%NATIVE_BUILD_DIR%\Pick6Native.dll" "%LOADER_DIR%\" >nul
    if %ERRORLEVEL% neq 0 (
        echo WARNING: Failed to copy Pick6Native.dll
    ) else (
        echo   [OK] Pick6Native.dll copied
    )
) else (
    echo   [SKIP] Pick6Native.dll not found (native build may have failed)
)

:: Check for Pick6VulkanHook.dll
if not exist "%LOADER_DIR%\Pick6VulkanHook.dll" (
    echo.
    echo   WARNING: Pick6VulkanHook.dll not found in Loader directory!
    echo   This DLL is REQUIRED for the application to work.
    echo.
    echo   Please build or obtain Pick6VulkanHook.dll and place it in:
    echo   %LOADER_DIR%
    echo.
    echo   See: %LOADER_DIR%\Pick6VulkanHook.dll.placeholder.md
    echo.
) else (
    echo   [OK] Pick6VulkanHook.dll present
)

echo.
goto :eof

:ShowSummary
echo ============================================================
echo Build Summary
echo ============================================================
echo.

:: Check what DLLs are ready in the Loader directory
set DLL_COUNT=0
set MISSING_COUNT=0

echo Native DLLs in Loader project:
if exist "%LOADER_DIR%\Pick6VulkanHook.dll" (
    set /a DLL_COUNT+=1
    echo   [✓] Pick6VulkanHook.dll (REQUIRED)
) else (
    set /a MISSING_COUNT+=1
    echo   [✗] Pick6VulkanHook.dll (REQUIRED - MISSING!)
)

if exist "%LOADER_DIR%\Pick6Native.dll" (
    set /a DLL_COUNT+=1
    echo   [✓] Pick6Native.dll (optional)
) else (
    echo   [○] Pick6Native.dll (optional, not present)
)

:: Check for proxy DLLs
if exist "%LOADER_DIR%\dxgi.dll" (
    set /a DLL_COUNT+=1
    echo   [✓] dxgi.dll (proxy, optional)
)
if exist "%LOADER_DIR%\d3d11.dll" (
    set /a DLL_COUNT+=1
    echo   [✓] d3d11.dll (proxy, optional)
)
if exist "%LOADER_DIR%\vulkan-1.dll" (
    set /a DLL_COUNT+=1
    echo   [✓] vulkan-1.dll (proxy, optional)
)

echo.
echo Found %DLL_COUNT% DLL(s) ready for deployment

if %MISSING_COUNT% gtr 0 (
    echo.
    echo ⚠️  WARNING: %MISSING_COUNT% required DLL(s) missing!
    echo The application will NOT work without Pick6VulkanHook.dll
    echo.
    echo Next steps:
    echo   1. Build or obtain Pick6VulkanHook.dll
    echo   2. Copy it to: %LOADER_DIR%
    echo   3. Re-run this build script or build the Loader project
    echo.
) else (
    echo.
    echo ✅ All required DLLs are present!
    echo.
    echo You can now build the loader:
    echo   dotnet publish src\Pick6.Loader\Pick6.Loader.csproj --configuration Release --runtime win-x64 --self-contained true
    echo.
    echo Or use the installer script:
    echo   install.bat
    echo.
)

echo For more information, see:
echo   - DLL_DEPLOYMENT_GUIDE.md
echo   - src\Pick6.Loader\README_DLL_REQUIREMENTS.md
echo.

goto :eof
