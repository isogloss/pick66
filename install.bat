@echo off
REM Pick66 Unified Installer / Builder
REM Usage:
REM   install.bat              -> Full clean publish install
REM   install.bat fast         -> Fast incremental publish (falls back to full on failure)
REM   install.bat build        -> Restore + build only (no publish/install)
REM
REM Optional environment overrides:
REM   PICK66_OUTPUT   -> Absolute path for installation output (default: %USERPROFILE%\Desktop\Pick66)

setlocal enabledelayedexpansion

set "MODE=%~1"
if "%MODE%"=="" set "MODE=full"

echo.
echo =========================================
echo          Pick66 Installer v3.0
echo            Mode: %MODE%
echo =========================================
echo.

REM Validate project location
if not exist "src\Pick6.Loader\Pick6.Loader.csproj" (
    echo ERROR: src\Pick6.Loader\Pick6.Loader.csproj not found.
    echo Run this from the repository root.
    echo.
    pause
    exit /b 1
)

REM .NET availability
echo [1/5] Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET 8 SDK required but not found.
    echo Install from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

for /f "tokens=1 delims=." %%v in ('dotnet --version') do set DOTNET_MAJOR=%%v
if %DOTNET_MAJOR% LSS 8 (
    echo ERROR: .NET 8 or higher required. Found:
    dotnet --version
    pause
    exit /b 1
)
for /f %%v in ('dotnet --version') do echo     ✓ .NET SDK OK (version: %%v)

REM Determine output directory
if not defined PICK66_OUTPUT set "PICK66_OUTPUT=%USERPROFILE%\Desktop\Pick66"
set "OUTPUT_DIR=%PICK66_OUTPUT%"

REM Step 2: Restore (only for full or build)
if /i "%MODE%"=="full" (
    echo.
    echo [2/5] Restoring dependencies (full)...
    dotnet restore --verbosity quiet
    if errorlevel 1 (
        echo ERROR: Dependency restore failed.
        pause
        exit /b 1
    )
) else if /i "%MODE%"=="build" (
    echo.
    echo [2/5] Restoring dependencies (build)...
    dotnet restore --verbosity quiet
    if errorlevel 1 (
        echo ERROR: Dependency restore failed.
        pause
        exit /b 1
    )
) else (
    echo.
    echo [2/5] Quick dependency verification (fast)...
    dotnet restore --verbosity minimal --no-dependencies 2>nul
    if errorlevel 1 (
        echo Dependency verification failed, performing full restore...
        dotnet restore --verbosity quiet
        if errorlevel 1 (
            echo ERROR: Dependency restore failed.
            pause
            exit /b 1
        )
    )
)

echo     ✓ Dependencies ready

REM Step 3: Build or Publish
if /i "%MODE%"=="build" (
    echo.
    echo [3/5] Building (no publish)...
    dotnet build src\Pick6.Loader\Pick6.Loader.csproj --configuration Release --no-restore --verbosity quiet
    if errorlevel 1 (
        echo ERROR: Build failed.
        pause
        exit /b 1
    )
    echo     ✓ Build successful
    goto finish
)

echo.
echo [3/5] Publishing application (mode=%MODE%)...

if /i "%MODE%"=="fast" (
    REM Fast incremental publish
    dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
        --configuration Release ^
        --runtime win-x64 ^
        --self-contained true ^
        --no-restore ^
        --verbosity minimal ^
        --output "%OUTPUT_DIR%" ^
        -p:PublishSingleFile=true ^
        -p:PublishReadyToRun=false ^
        -p:IncludeNativeLibrariesForSelfExtract=true ^
        -p:IncludeAllContentForSelfExtract=true ^
        /p:UseSharedCompilation=true
    if errorlevel 1 (
        echo Fast publish failed, falling back to full publish...
        set "MODE=full"
    ) else (
        goto verify
    )
)

if /i "%MODE%"=="full" (
    dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
        --configuration Release ^
        --runtime win-x64 ^
        --self-contained true ^
        --no-restore ^
        --verbosity quiet ^
        --output "%OUTPUT_DIR%" ^
        -p:PublishSingleFile=true ^
        -p:IncludeNativeLibrariesForSelfExtract=true ^
        -p:IncludeAllContentForSelfExtract=true
    if errorlevel 1 (
        echo ERROR: Publish failed.
        pause
        exit /b 1
    )
)

echo     ✓ Publish succeeded

:verify
echo.
echo [4/5] Verifying installation...
if not exist "%OUTPUT_DIR%\loader.exe" (
    echo ERROR: Installation incomplete - loader.exe not found.
    pause
    exit /b 1
)

echo     ✓ Verification passed

:finish
echo.
echo [5/5] Done!
echo Output directory: %OUTPUT_DIR%\
if /i not "%MODE%"=="build" (
    echo Run loader.exe to start Pick66.
)

echo.
if /i not "%MODE%"=="build" (
    echo Press any key to open the output folder...
    pause >nul
    explorer "%OUTPUT_DIR%"
) else (
    pause
)
exit /b 0