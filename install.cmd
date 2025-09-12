@echo off
setlocal

REM Try PowerShell 7+ first (pwsh), fall back to Windows PowerShell (powershell)
pwsh -NoLogo -ExecutionPolicy Bypass -File "%~dp0install.ps1" %* 2>nul
if %ERRORLEVEL% equ 0 goto :success

REM If pwsh failed or not found, try Windows PowerShell
echo PowerShell 7+ not found, falling back to Windows PowerShell...
powershell -NoLogo -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
if %ERRORLEVEL% neq 0 (
    echo.
    echo Error: Script execution failed (exit code %ERRORLEVEL%). 
    echo Build diagnostics have been displayed above.
    echo Recommended: Install PowerShell 7+ from https://github.com/PowerShell/PowerShell
    echo.
    pause
)

:success
exit /b %ERRORLEVEL%