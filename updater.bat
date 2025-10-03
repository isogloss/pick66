@echo off
REM ============================================================================
REM Pick6 Loader Updater Script
REM ============================================================================
REM 
REM This is a TEMPLATE/REFERENCE file showing the updater script structure.
REM The actual updater.bat is generated dynamically by UpdateService.cs
REM at runtime with specific paths filled in.
REM 
REM PURPOSE:
REM   - Wait for the current loader.exe process to exit
REM   - Backup the old loader.exe
REM   - Replace it with the new version
REM   - Restart the loader
REM   - Clean up temporary files
REM
REM This script is necessary because a running executable cannot replace itself.
REM ============================================================================

echo Pick6 Updater - Installing new version...
echo.

REM Wait for the current process to exit
timeout /t 2 /nobreak >nul

REM Backup the old loader
REM (Actual paths will be filled in by UpdateService.cs)
if exist "OLD_LOADER_PATH.bak" del "OLD_LOADER_PATH.bak"
move "OLD_LOADER_PATH" "OLD_LOADER_PATH.bak" >nul 2>&1

REM Copy the new loader
echo Copying new loader.exe...
copy "NEW_LOADER_PATH" "OLD_LOADER_PATH" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy new loader.exe
    echo Restoring backup...
    move "OLD_LOADER_PATH.bak" "OLD_LOADER_PATH" >nul 2>&1
    pause
    exit /b 1
)

echo Update successful!
echo.

REM Clean up temporary directory
echo Cleaning up...
rd /s /q "TEMP_UPDATE_DIR" >nul 2>&1

REM Remove backup if successful
del "OLD_LOADER_PATH.bak" >nul 2>&1

REM Restart the loader
echo Restarting Pick6 Loader...
start "" "OLD_LOADER_PATH"

REM Delete this script
del "%~f0"
