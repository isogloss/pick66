@echo off
REM Pick66 Build Validation Script
REM Validates that the Windows-only setup is correct

echo ===============================================
echo          Pick66 - Build Validation
echo ===============================================
echo.

echo [1/3] Checking .NET 8 SDK availability...
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ❌ .NET 8 SDK not found
    echo Please install from: https://dot.net
    goto :error
) else (
    echo ✅ .NET 8 SDK found
)

echo.
echo [2/3] Validating solution structure...
if not exist "Pick6.sln" (
    echo ❌ Solution file not found
    goto :error
)
echo ✅ Solution file found

if not exist "src\Pick6.Loader\Pick6.Loader.csproj" (
    echo ❌ Main loader project not found
    goto :error
)
echo ✅ Main loader project found

echo.
echo [3/3] Testing package restore...
dotnet restore >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ❌ Package restore failed
    goto :error
) else (
    echo ✅ Package restore successful
)

echo.
echo ===============================================
echo           VALIDATION SUCCESSFUL!
echo ===============================================
echo.
echo Your Pick66 setup is ready for Windows build.
echo Run 'setup.bat' to build and install.
echo.
goto :end

:error
echo.
echo ===============================================
echo             VALIDATION FAILED!
echo ===============================================
echo.
echo Please fix the issues above before continuing.
echo.

:end
pause