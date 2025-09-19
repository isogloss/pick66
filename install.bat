@echo off
REM Pick66 - Universal Installer Launcher
REM This script determines the best installation method for the user's system

echo ===============================================
echo              Pick66 - One-Click Installer
echo              No Dependencies Required!
echo ===============================================
echo.
echo This installer will work even if you don't have .NET SDK installed.
echo It will try multiple methods to get Pick66 running on your system.
echo.

REM Check if PowerShell is available and prefer it for better error handling
powershell -Command "exit 0" >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo Using PowerShell installer for enhanced functionality...
    echo.
    powershell -ExecutionPolicy Bypass -File "%~dp0install-standalone.ps1" %*
    if %ERRORLEVEL% equ 0 (
        echo.
        echo Installation completed successfully!
        pause
        exit /b 0
    ) else (
        echo.
        echo PowerShell installer failed, trying batch version...
        echo.
    )
)

echo Using batch installer...
call "%~dp0install-standalone.bat" %*

if %ERRORLEVEL% equ 0 (
    echo.
    echo Installation completed successfully!
) else (
    echo.
    echo Installation failed. Please check the error messages above.
    echo For help, visit: https://github.com/isogloss/pick66/issues
)

pause