@echo off
setlocal enabledelayedexpansion

echo ========================================
echo Pick6 Native - C++ Build Script
echo ========================================
echo.

:: Check if CMake is available
where cmake >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: CMake not found. Please install CMake and add it to PATH.
    echo Download from: https://cmake.org/download/
    pause
    exit /b 1
)

:: Set build directory
set BUILD_DIR=build

:: Clean previous build
if exist "%BUILD_DIR%" (
    echo Cleaning previous build...
    rmdir /s /q "%BUILD_DIR%"
)

:: Create build directory
echo Creating build directory...
mkdir "%BUILD_DIR%"
cd "%BUILD_DIR%"

:: Configure CMake
echo.
echo Configuring CMake...
cmake .. -G "Visual Studio 16 2019" -A x64
if %errorlevel% neq 0 (
    echo ERROR: CMake configuration failed
    cd ..
    pause
    exit /b 1
)

:: Build Release configuration
echo.
echo Building Release configuration...
cmake --build . --config Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    cd ..
    pause
    exit /b 1
)

:: Build Debug configuration
echo.
echo Building Debug configuration...
cmake --build . --config Debug
if %errorlevel% neq 0 (
    echo WARNING: Debug build failed (non-critical)
)

cd ..

:: Show results
echo.
echo ========================================
echo Build Complete!
echo ========================================
echo.
echo Output files:
if exist "%BUILD_DIR%\Release\Pick6Native.dll" (
    echo [RELEASE] %BUILD_DIR%\Release\Pick6Native.dll
)
if exist "%BUILD_DIR%\Debug\Pick6Native.dll" (
    echo [DEBUG]   %BUILD_DIR%\Debug\Pick6Native.dll
)
if exist "%BUILD_DIR%\Release\Pick6Test.exe" (
    echo [TEST]    %BUILD_DIR%\Release\Pick6Test.exe
)
echo.

:: Offer to run tests
if exist "%BUILD_DIR%\Release\Pick6Test.exe" (
    set /p RUN_TEST="Run test executable? (y/n): "
    if /i "!RUN_TEST!"=="y" (
        echo.
        echo Running tests...
        echo ----------------------------------------
        "%BUILD_DIR%\Release\Pick6Test.exe"
        echo ----------------------------------------
    )
)

echo.
echo Build script completed successfully.
pause
