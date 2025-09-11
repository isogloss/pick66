@echo off
setlocal
powershell -NoLogo -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
exit /b %ERRORLEVEL%