@echo off
REM Build script for native Pick6VulkanHook DLL
REM This script attempts to build the native DLL using available compilers

setlocal enabledelayedexpansion

echo Building Pick6VulkanHook.dll...

REM Try to find Visual Studio Build Tools or Visual Studio
set "VSTOOLS_PATH="
set "COMPILER_FOUND=0"

REM Check for VS2022 Build Tools
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat" (
    set "VSTOOLS_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat"
    set "COMPILER_FOUND=1"
)

REM Check for VS2022 Community/Professional
if !COMPILER_FOUND!==0 (
    if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" (
        set "VSTOOLS_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
        set "COMPILER_FOUND=1"
    )
)

REM Check for VS2019 Build Tools
if !COMPILER_FOUND!==0 (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvars64.bat" (
        set "VSTOOLS_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvars64.bat"
        set "COMPILER_FOUND=1"
    )
)

if !COMPILER_FOUND!==0 (
    echo ❌ Visual Studio Build Tools not found
    echo Please install Visual Studio Build Tools 2019 or later
    echo Download from: https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022
    exit /b 1
)

echo Using compiler at: !VSTOOLS_PATH!

REM Setup build environment
call "!VSTOOLS_PATH!" >nul 2>&1

REM Create output directory
if not exist "..\..\dist" mkdir "..\..\dist"

REM Compile the DLL
echo Compiling Pick6VulkanHook.dll...
cl.exe /LD /Fe:"..\..\dist\Pick6VulkanHook.dll" Pick6VulkanHook\Pick6VulkanHook.cpp /link /DEF:Pick6VulkanHook\Pick6VulkanHook.def kernel32.lib

if %ERRORLEVEL% neq 0 (
    echo ❌ Compilation failed
    exit /b 1
)

echo ✅ Pick6VulkanHook.dll built successfully
echo Location: %CD%\..\..\dist\Pick6VulkanHook.dll

REM Cleanup temporary files
del *.obj *.exp *.lib 2>nul

exit /b 0