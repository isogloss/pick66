@echo off
REM Configuration validation script for Pick66
REM Validates that all projects are properly configured for self-contained deployment

echo.
echo ========================================
echo     Pick66 Configuration Validator
echo ========================================
echo.

REM Check .NET SDK
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ❌ .NET SDK not found
    exit /b 1
)
echo ✓ .NET SDK available

REM Validate project files exist
if not exist "src\Pick6.Loader\Pick6.Loader.csproj" (
    echo ❌ Pick6.Loader project not found
    exit /b 1
)
if not exist "src\Pick6.Core\Pick6.Core.csproj" (
    echo ❌ Pick6.Core project not found
    exit /b 1
)
if not exist "src\Pick6.Projection\Pick6.Projection.csproj" (
    echo ❌ Pick6.Projection project not found
    exit /b 1
)
echo ✓ All project files found

REM Test restore
echo.
echo Testing dependency restoration...
dotnet restore --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo ❌ Dependency restoration failed
    exit /b 1
)
echo ✓ Dependencies restored successfully

REM Test build without publish
echo.
echo Testing build configuration...
dotnet build --configuration Release --verbosity quiet --no-restore
if %ERRORLEVEL% neq 0 (
    echo ❌ Build configuration failed
    exit /b 1
)
echo ✓ Build configuration valid

REM Validate self-contained settings
echo.
echo Validating self-contained deployment settings...
findstr /C:"PublishSingleFile" src\Pick6.Loader\Pick6.Loader.csproj >nul
if %ERRORLEVEL% neq 0 (
    echo ❌ PublishSingleFile not configured
    exit /b 1
)
echo ✓ Single-file publishing configured

findstr /C:"IncludeAllContentForSelfExtract" src\Pick6.Loader\Pick6.Loader.csproj >nul
if %ERRORLEVEL% neq 0 (
    echo ❌ IncludeAllContentForSelfExtract not configured  
    exit /b 1
)
echo ✓ Content extraction configured

findstr /C:"net8.0-windows" Directory.Build.props >nul
if %ERRORLEVEL% neq 0 (
    echo ❌ Windows target framework not configured
    exit /b 1
)
echo ✓ Windows target framework configured

echo.
echo ========================================
echo    ✅ All Configuration Checks Passed
echo ========================================
echo.
echo The installer should now:
echo - Bundle all .NET runtime dependencies
echo - Create a fully self-contained executable
echo - Work faster with incremental builds
echo.
pause