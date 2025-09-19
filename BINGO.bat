@echo off
REM BINGO - Pick66 Installer

echo ===============================================
echo              BINGO - Pick66 Installer
echo ===============================================
echo.

REM Check .NET SDK
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Error: Need .NET 8 SDK
    echo Get it: https://dotnet.microsoft.com/download
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
    echo Error: No loader.exe created
    pause
    exit /b 1
)

echo.
echo Done! Run: dist\loader.exe
pause