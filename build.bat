@echo off
REM Build script for Pick6 solution
REM This script builds all projects in the correct order

echo ========================================
echo Building Pick6 Solution
echo ========================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
    exit /b 1
)

echo Found .NET SDK version:
dotnet --version
echo.

REM Build Pick6.Core
echo ========================================
echo Building Pick6.Core...
echo ========================================
dotnet build src\Pick6.Core\Pick6.Core.csproj -c Release
if errorlevel 1 (
    echo ERROR: Failed to build Pick6.Core
    exit /b 1
)
echo.

REM Build Pick6.VulkanHook
echo ========================================
echo Building Pick6.VulkanHook...
echo ========================================
dotnet build src\Pick6.VulkanHook\Pick6.VulkanHook.csproj -c Release
if errorlevel 1 (
    echo ERROR: Failed to build Pick6.VulkanHook
    exit /b 1
)
echo.

REM Build Pick6.Projection
echo ========================================
echo Building Pick6.Projection...
echo ========================================
dotnet build src\Pick6.Projection\Pick6.Projection.csproj -c Release
if errorlevel 1 (
    echo ERROR: Failed to build Pick6.Projection
    exit /b 1
)
echo.

REM Build Pick6.Loader
echo ========================================
echo Building Pick6.Loader...
echo ========================================
dotnet build src\Pick6.Loader\Pick6.Loader.csproj -c Release
if errorlevel 1 (
    echo ERROR: Failed to build Pick6.Loader
    exit /b 1
)
echo.

echo ========================================
echo Build completed successfully!
echo ========================================
echo.
echo Output files:
echo - Pick6.Core: src\Pick6.Core\bin\Release\net8.0-windows\
echo - Pick6.VulkanHook: src\Pick6.VulkanHook\bin\Release\net8.0-windows\
echo - Pick6.Projection: src\Pick6.Projection\bin\Release\net8.0-windows\
echo - Pick6.Loader: src\Pick6.Loader\bin\Release\net8.0-windows\
echo.
echo IMPORTANT: Copy Pick6VulkanHook.dll to the same directory as loader.exe
echo.

pause
