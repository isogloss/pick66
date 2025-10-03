#include "Pick6Export.h"
#include "Pick6Injector.h"
#include <memory>

using namespace Pick6;

// Global injector instance
static std::unique_ptr<Injector> g_Injector = nullptr;

// Helper to copy error message
static void CopyErrorMessage(const std::wstring& message, LPWSTR buffer, DWORD bufferSize) {
    if (buffer && bufferSize > 0) {
        wcsncpy_s(buffer, bufferSize, message.c_str(), _TRUNCATE);
    }
}

extern "C" {

BOOL WINAPI Pick6_Initialize() {
    try {
        if (!g_Injector) {
            g_Injector = std::make_unique<Injector>();
        }
        return TRUE;
    }
    catch (...) {
        return FALSE;
    }
}

VOID WINAPI Pick6_Cleanup() {
    g_Injector.reset();
}

BOOL WINAPI Pick6_InjectDirect(
    DWORD processId,
    LPCWSTR dllPath,
    LPWSTR errorMessage,
    DWORD errorMessageSize
) {
    if (!g_Injector) {
        Pick6_Initialize();
    }

    try {
        std::wstring dllPathStr(dllPath);
        InjectionResult result = g_Injector->InjectDirect(processId, dllPathStr);
        
        if (!result.Success) {
            CopyErrorMessage(result.Message, errorMessage, errorMessageSize);
        }
        
        return result.Success ? TRUE : FALSE;
    }
    catch (...) {
        CopyErrorMessage(L"Unexpected exception during injection", errorMessage, errorMessageSize);
        return FALSE;
    }
}

BOOL WINAPI Pick6_DeployProxy(
    DWORD processId,
    LPCWSTR proxyDllName,
    LPWSTR errorMessage,
    DWORD errorMessageSize
) {
    if (!g_Injector) {
        Pick6_Initialize();
    }

    try {
        std::wstring proxyDllNameStr(proxyDllName);
        InjectionResult result = g_Injector->InjectProxy(processId, proxyDllNameStr);
        
        if (!result.Success) {
            CopyErrorMessage(result.Message, errorMessage, errorMessageSize);
        }
        
        return result.Success ? TRUE : FALSE;
    }
    catch (...) {
        CopyErrorMessage(L"Unexpected exception during proxy deployment", errorMessage, errorMessageSize);
        return FALSE;
    }
}

BOOL WINAPI Pick6_FindFiveMProcesses(
    DWORD* processIds,
    DWORD maxProcesses,
    DWORD* processCount
) {
    if (!g_Injector) {
        Pick6_Initialize();
    }

    try {
        auto processes = g_Injector->FindFiveMProcesses();
        
        DWORD count = min((DWORD)processes.size(), maxProcesses);
        for (DWORD i = 0; i < count; i++) {
            processIds[i] = processes[i].ProcessId;
        }
        
        if (processCount) {
            *processCount = (DWORD)processes.size();
        }
        
        return TRUE;
    }
    catch (...) {
        return FALSE;
    }
}

BOOL WINAPI Pick6_EnableSeDebugPrivilege() {
    if (!g_Injector) {
        Pick6_Initialize();
    }

    try {
        return g_Injector->EnableSeDebugPrivilege() ? TRUE : FALSE;
    }
    catch (...) {
        return FALSE;
    }
}

BOOL WINAPI Pick6_IsProcessRunning(DWORD processId) {
    if (!g_Injector) {
        Pick6_Initialize();
    }

    try {
        return g_Injector->IsProcessRunning(processId) ? TRUE : FALSE;
    }
    catch (...) {
        return FALSE;
    }
}

BOOL WINAPI Pick6_IsProcessUsingModule(DWORD processId, LPCWSTR moduleName) {
    if (!g_Injector) {
        Pick6_Initialize();
    }

    try {
        std::wstring moduleNameStr(moduleName);
        return g_Injector->IsProcessUsingModule(processId, moduleNameStr) ? TRUE : FALSE;
    }
    catch (...) {
        return FALSE;
    }
}

} // extern "C"
