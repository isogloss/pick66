#include <windows.h>
#include <iostream>
#include "../Pick6Export.h"

int main() {
    std::wcout << L"Pick6 Native Injector Test" << std::endl;
    std::wcout << L"===========================" << std::endl << std::endl;

    // Initialize
    if (!Pick6_Initialize()) {
        std::wcerr << L"Failed to initialize Pick6" << std::endl;
        return 1;
    }

    std::wcout << L"✓ Pick6 initialized successfully" << std::endl;

    // Enable SeDebugPrivilege
    if (Pick6_EnableSeDebugPrivilege()) {
        std::wcout << L"✓ SeDebugPrivilege enabled" << std::endl;
    } else {
        std::wcout << L"⚠ Failed to enable SeDebugPrivilege (may need Administrator)" << std::endl;
    }

    // Find FiveM processes
    std::wcout << std::endl << L"Searching for FiveM processes..." << std::endl;
    DWORD processIds[32] = { 0 };
    DWORD processCount = 0;

    if (Pick6_FindFiveMProcesses(processIds, 32, &processCount)) {
        std::wcout << L"✓ Found " << processCount << L" FiveM process(es)" << std::endl;
        
        for (DWORD i = 0; i < processCount && i < 32; i++) {
            std::wcout << L"  - Process ID: " << processIds[i];
            
            if (Pick6_IsProcessRunning(processIds[i])) {
                std::wcout << L" (Running)";
            }
            
            // Check for Vulkan
            if (Pick6_IsProcessUsingModule(processIds[i], L"vulkan")) {
                std::wcout << L" [Vulkan]";
            }
            
            std::wcout << std::endl;
        }
    } else {
        std::wcout << L"ℹ No FiveM processes found" << std::endl;
    }

    // Test injection (commented out to prevent accidental injection)
    /*
    if (processCount > 0) {
        std::wcout << std::endl << L"Testing injection into first process..." << std::endl;
        
        wchar_t errorMsg[512] = { 0 };
        if (Pick6_InjectDirect(processIds[0], L"C:\\Path\\To\\Your\\DLL.dll", errorMsg, 512)) {
            std::wcout << L"✓ Injection successful" << std::endl;
        } else {
            std::wcout << L"✗ Injection failed: " << errorMsg << std::endl;
        }
    }
    */

    // Cleanup
    Pick6_Cleanup();
    std::wcout << std::endl << L"✓ Pick6 cleanup complete" << std::endl;

    return 0;
}
