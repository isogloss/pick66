@echo off
REM Create a minimal stub DLL when native build tools are not available
REM This creates a basic stub that prevents runtime errors

setlocal enabledelayedexpansion

echo Creating minimal stub DLL...

REM Create a temporary C file for the stub
set "TEMP_C_FILE=%TEMP%\pick6_stub_!RANDOM!.c"

echo #include ^<windows.h^> > "!TEMP_C_FILE!"
echo. >> "!TEMP_C_FILE!"
echo BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) { return TRUE; } >> "!TEMP_C_FILE!"
echo. >> "!TEMP_C_FILE!"
echo __declspec(dllexport) int InitializeVulkanHook() { return 1; } >> "!TEMP_C_FILE!"
echo __declspec(dllexport) int CaptureFrame(void* frameData) { return 0; } >> "!TEMP_C_FILE!"
echo __declspec(dllexport) void ShutdownVulkanHook() { } >> "!TEMP_C_FILE!"

REM Try to compile with GCC (if available via MinGW or similar)
gcc -shared -o "..\..\dist\Pick6VulkanHook.dll" "!TEMP_C_FILE!" 2>nul
if %ERRORLEVEL% equ 0 (
    echo ✅ Stub DLL created with GCC
    del "!TEMP_C_FILE!" 2>nul
    exit /b 0
)

REM Try TinyCC if available
tcc -shared -o "..\..\dist\Pick6VulkanHook.dll" "!TEMP_C_FILE!" 2>nul
if %ERRORLEVEL% equ 0 (
    echo ✅ Stub DLL created with TCC
    del "!TEMP_C_FILE!" 2>nul
    exit /b 0
)

REM Clean up temp file
del "!TEMP_C_FILE!" 2>nul

echo ⚠ Could not create stub DLL - no suitable compiler found
echo Application will run but may have limited Vulkan functionality
exit /b 1