#include <windows.h>
#include "Pick6Export.h"

BOOL APIENTRY DllMain(
    HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
) {
    switch (ul_reason_for_call) {
    case DLL_PROCESS_ATTACH:
        // Initialize when DLL is loaded
        DisableThreadLibraryCalls(hModule);
        Pick6_Initialize();
        break;
        
    case DLL_PROCESS_DETACH:
        // Cleanup when DLL is unloaded
        Pick6_Cleanup();
        break;
        
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    }
    return TRUE;
}
