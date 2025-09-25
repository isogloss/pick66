@echo off
REM Quick build script for development

echo Building Pick66...
dotnet restore --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo Restore failed!
    pause
    exit /b 1
)

dotnet build --configuration Release --no-restore --verbosity quiet

if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful! Output in src\Pick6.Loader\bin\Release\net8.0-windows\
pause