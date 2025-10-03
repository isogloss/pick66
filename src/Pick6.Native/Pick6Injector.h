#pragma once

#include <windows.h>
#include <string>
#include <vector>

namespace Pick6 {

/// <summary>
/// Available injection strategies
/// </summary>
enum class InjectionStrategy {
    Direct,         // Direct LoadLibrary remote thread injection
    DxgiProxy,      // Proxy DLL injection via dxgi.dll
    D3D11Proxy,     // Proxy DLL injection via d3d11.dll
    VulkanProxy     // Proxy DLL injection via vulkan-1.dll
};

/// <summary>
/// Result of an injection attempt
/// </summary>
struct InjectionResult {
    bool Success;
    InjectionStrategy Strategy;
    std::wstring Message;
    DWORD ErrorCode;

    InjectionResult() : Success(false), Strategy(InjectionStrategy::Direct), ErrorCode(0) {}
    
    static InjectionResult Failed(InjectionStrategy strategy, const std::wstring& message, DWORD errorCode = 0) {
        InjectionResult result;
        result.Success = false;
        result.Strategy = strategy;
        result.Message = message;
        result.ErrorCode = errorCode;
        return result;
    }

    static InjectionResult Succeeded(InjectionStrategy strategy, const std::wstring& message = L"") {
        InjectionResult result;
        result.Success = true;
        result.Strategy = strategy;
        result.Message = message;
        result.ErrorCode = 0;
        return result;
    }
};

/// <summary>
/// Process information structure
/// </summary>
struct ProcessInfo {
    DWORD ProcessId;
    std::wstring ProcessName;
    std::wstring WindowTitle;
    HWND WindowHandle;

    ProcessInfo() : ProcessId(0), WindowHandle(nullptr) {}
};

/// <summary>
/// Main injector class for Pick6
/// </summary>
class Injector {
public:
    Injector();
    ~Injector();

    // Core injection methods
    InjectionResult InjectIntoProcess(DWORD processId, const std::wstring& dllPath);
    InjectionResult InjectDirect(DWORD processId, const std::wstring& dllPath);
    InjectionResult InjectProxy(DWORD processId, const std::wstring& proxyDllName);

    // Privilege management
    bool EnableSeDebugPrivilege();

    // Process utilities
    std::vector<ProcessInfo> FindFiveMProcesses();
    bool IsProcessRunning(DWORD processId);
    bool IsProcessUsingModule(DWORD processId, const std::wstring& moduleName);

private:
    // Direct injection helpers
    bool PerformDirectInjection(HANDLE hProcess, const std::wstring& dllPath);

    // Proxy injection helpers
    InjectionResult DeployProxyDll(const std::wstring& proxyDllName, const std::wstring& targetPath);
    std::wstring GetProxyDllPath(const std::wstring& proxyDllName);
    std::wstring GetProcessDirectory(DWORD processId);
    bool IsDirectoryWritable(const std::wstring& directory);
    bool CreateBackup(const std::wstring& originalPath, const std::wstring& backupPath);
    bool RestoreFromBackup(const std::wstring& backupPath, const std::wstring& targetPath);

    // Utility functions
    std::wstring GetLastErrorAsString(DWORD errorCode = 0);
    std::wstring GetExecutablePath();
};

/// <summary>
/// Privilege manager utilities
/// </summary>
class PrivilegeManager {
public:
    static bool EnableSeDebugPrivilege();
private:
    static bool SetPrivilege(HANDLE hToken, LPCTSTR lpszPrivilege, BOOL bEnablePrivilege);
};

} // namespace Pick6
