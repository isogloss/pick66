@echo off
REM BINGO - Pick66 Installer
REM Builds and installs loader.exe

echo ===============================================
echo              BINGO - Pick66 Installer
echo ===============================================
echo.

REM Check if .NET SDK is available
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Error: .NET SDK not found
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Building loader.exe...
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output dist

if %ERRORLEVEL% neq 0 (
    echo Build failed
    pause
    exit /b 1
)

if not exist "dist\loader.exe" (
    echo Error: loader.exe not created
    pause
    exit /b 1
)

echo.
echo Installation complete! 
echo Run: dist\loader.exe
echo.
pause