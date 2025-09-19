@echo off
REM Pick66 - Universal Installer Launcher
REM This script determines the best installation method for the user's system

echo ===============================================
echo              Pick66 - Universal Installer
echo ===============================================
echo.

REM Check if PowerShell is available and prefer it for better error handling
powershell -Command "exit 0" >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo Using PowerShell installer for enhanced functionality...
    powershell -ExecutionPolicy Bypass -File "%~dp0install-standalone.ps1" %*
) else (
    echo Using batch installer...
    call "%~dp0install-standalone.bat" %*
)