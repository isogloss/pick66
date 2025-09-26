// Template for dxgi.dll proxy DLL
// Compile with: cl /LD /MT dxgi_proxy_template.cpp /Fe:dxgi.dll /link /DEF:dxgi_proxy.def
#include <windows.h>
#include <stdio.h>

// Original DLL handle
static HMODULE hOriginalDll = NULL;
static bool injectionPerformed = false;

// Function pointers for original exports
static FARPROC pOriginalCreateDXGIFactory = NULL;
static FARPROC pOriginalCreateDXGIFactory1 = NULL;
static FARPROC pOriginalCreateDXGIFactory2 = NULL;
static FARPROC pOriginalDXGIGetDebugInterface1 = NULL;

// Log function
void LogMessage(const char* message) {
    char logPath[MAX_PATH];
    GetModuleFileNameA(NULL, logPath, MAX_PATH);
    char* lastSlash = strrchr(logPath, '\\');
    if (lastSlash) {
        strcpy(lastSlash + 1, "logs\\injector.log");
        
        FILE* logFile = fopen(logPath, "a");
        if (logFile) {
            SYSTEMTIME st;
            GetLocalTime(&st);
            fprintf(logFile, "%04d-%02d-%02d %02d:%02d:%02d.%03d [INFO] %s\n", 
                st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, message);
            fclose(logFile);
        }
    }
}

// Load original DLL and get function addresses
bool LoadOriginalDll() {
    if (hOriginalDll != NULL) return true;
    
    char originalPath[MAX_PATH];
    GetModuleFileNameA(NULL, originalPath, MAX_PATH);
    char* lastSlash = strrchr(originalPath, '\\');
    if (lastSlash) {
        strcpy(lastSlash + 1, "dxgi.original.dll");
    }
    
    hOriginalDll = LoadLibraryA(originalPath);
    if (hOriginalDll == NULL) {
        // Try system directory
        GetSystemDirectoryA(originalPath, MAX_PATH);
        strcat(originalPath, "\\dxgi.dll");
        hOriginalDll = LoadLibraryA(originalPath);
    }
    
    if (hOriginalDll != NULL) {
        pOriginalCreateDXGIFactory = GetProcAddress(hOriginalDll, "CreateDXGIFactory");
        pOriginalCreateDXGIFactory1 = GetProcAddress(hOriginalDll, "CreateDXGIFactory1");
        pOriginalCreateDXGIFactory2 = GetProcAddress(hOriginalDll, "CreateDXGIFactory2");
        pOriginalDXGIGetDebugInterface1 = GetProcAddress(hOriginalDll, "DXGIGetDebugInterface1");
        
        LogMessage("Original dxgi.dll loaded successfully");
        return true;
    }
    
    LogMessage("Failed to load original dxgi.dll");
    return false;
}

// Perform injection of our payload
void PerformInjection() {
    if (injectionPerformed) return;
    injectionPerformed = true;
    
    LogMessage("DXGI proxy DLL loaded - attempting payload injection");
    
    char payloadPath[MAX_PATH];
    GetModuleFileNameA(NULL, payloadPath, MAX_PATH);
    char* lastSlash = strrchr(payloadPath, '\\');
    if (lastSlash) {
        strcpy(lastSlash + 1, "Pick6VulkanHook.dll");
    }
    
    HMODULE hPayload = LoadLibraryA(payloadPath);
    if (hPayload != NULL) {
        LogMessage("Payload DLL loaded successfully via DXGI proxy method");
        
        // Optionally call initialization function in payload
        FARPROC initFunc = GetProcAddress(hPayload, "InitializeHook");
        if (initFunc != NULL) {
            ((void(*)())initFunc)();
        }
    } else {
        char errorMsg[256];
        sprintf(errorMsg, "Failed to load payload DLL: %s", payloadPath);
        LogMessage(errorMsg);
    }
}

// DLL entry point
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    switch (ul_reason_for_call) {
        case DLL_PROCESS_ATTACH:
            DisableThreadLibraryCalls(hModule);
            LoadOriginalDll();
            PerformInjection();
            break;
        case DLL_PROCESS_DETACH:
            if (hOriginalDll != NULL) {
                FreeLibrary(hOriginalDll);
            }
            break;
    }
    return TRUE;
}

// Export functions - forward to original DLL
extern "C" {
    __declspec(dllexport) HRESULT CreateDXGIFactory(REFIID riid, void** ppFactory) {
        if (pOriginalCreateDXGIFactory != NULL) {
            return ((HRESULT(*)(REFIID, void**))pOriginalCreateDXGIFactory)(riid, ppFactory);
        }
        return E_FAIL;
    }
    
    __declspec(dllexport) HRESULT CreateDXGIFactory1(REFIID riid, void** ppFactory) {
        if (pOriginalCreateDXGIFactory1 != NULL) {
            return ((HRESULT(*)(REFIID, void**))pOriginalCreateDXGIFactory1)(riid, ppFactory);
        }
        return E_FAIL;
    }
    
    __declspec(dllexport) HRESULT CreateDXGIFactory2(UINT Flags, REFIID riid, void** ppFactory) {
        if (pOriginalCreateDXGIFactory2 != NULL) {
            return ((HRESULT(*)(UINT, REFIID, void**))pOriginalCreateDXGIFactory2)(Flags, riid, ppFactory);
        }
        return E_FAIL;
    }
    
    __declspec(dllexport) HRESULT DXGIGetDebugInterface1(UINT Flags, REFIID riid, void** pDebug) {
        if (pOriginalDXGIGetDebugInterface1 != NULL) {
            return ((HRESULT(*)(UINT, REFIID, void**))pOriginalDXGIGetDebugInterface1)(Flags, riid, pDebug);
        }
        return E_FAIL;
    }
}