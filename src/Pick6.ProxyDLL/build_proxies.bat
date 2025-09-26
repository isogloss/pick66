@echo off
REM Build script for proxy DLLs
REM Requires Visual Studio C++ compiler in PATH

echo Building Pick6 Proxy DLLs...

REM Check for compiler
where cl >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo Error: Visual Studio C++ compiler not found in PATH
    echo Please run this from a Visual Studio Developer Command Prompt
    pause
    exit /b 1
)

REM Create output directory
if not exist "bin" mkdir bin

REM Build dxgi.dll proxy
echo Building dxgi.dll proxy...
cl /LD /MT dxgi_proxy_template.cpp /Fe:bin\dxgi.dll /link /SUBSYSTEM:WINDOWS /MACHINE:X64
if %ERRORLEVEL% NEQ 0 (
    echo Failed to build dxgi.dll proxy
    pause
    exit /b 1
)

REM Copy template for other proxies
echo Copying template for d3d11.dll...
copy dxgi_proxy_template.cpp d3d11_proxy_template.cpp >nul
powershell -Command "(gc d3d11_proxy_template.cpp) -replace 'dxgi', 'd3d11' | Out-File -encoding ASCII d3d11_proxy_template.cpp"

echo Building d3d11.dll proxy...
cl /LD /MT d3d11_proxy_template.cpp /Fe:bin\d3d11.dll /link /SUBSYSTEM:WINDOWS /MACHINE:X64

echo Copying template for vulkan-1.dll...
copy dxgi_proxy_template.cpp vulkan_proxy_template.cpp >nul
powershell -Command "(gc vulkan_proxy_template.cpp) -replace 'dxgi', 'vulkan-1' -replace 'CreateDXGIFactory', 'vkCreateInstance' | Out-File -encoding ASCII vulkan_proxy_template.cpp"

echo Building vulkan-1.dll proxy...
cl /LD /MT vulkan_proxy_template.cpp /Fe:bin\vulkan-1.dll /link /SUBSYSTEM:WINDOWS /MACHINE:X64

REM Clean up temporary files
del *.obj >nul 2>nul
del *.exp >nul 2>nul
del *.lib >nul 2>nul
del d3d11_proxy_template.cpp >nul 2>nul
del vulkan_proxy_template.cpp >nul 2>nul

echo.
echo Proxy DLL build complete!
echo Output files in bin\ directory:
dir bin\*.dll

pause