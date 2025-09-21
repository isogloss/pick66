@echo off
REM Quick build script for development

echo Building Pick66...
dotnet build --configuration Release

if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful! Output in src\Pick6.Loader\bin\Release\net8.0-windows\
pause