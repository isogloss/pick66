#pragma once

#ifdef PICK6NATIVE_EXPORTS
#define PICK6_API __declspec(dllexport)
#else
#define PICK6_API __declspec(dllimport)
#endif

#include <windows.h>

extern "C" {

/// <summary>
/// Initialize the injector
/// </summary>
PICK6_API BOOL WINAPI Pick6_Initialize();

/// <summary>
/// Cleanup and release resources
/// </summary>
PICK6_API VOID WINAPI Pick6_Cleanup();

/// <summary>
/// Inject DLL into target process using direct method
/// </summary>
/// <param name="processId">Target process ID</param>
/// <param name="dllPath">Full path to DLL to inject</param>
/// <param name="errorMessage">Buffer for error message (optional)</param>
/// <param name="errorMessageSize">Size of error message buffer</param>
/// <returns>TRUE on success, FALSE on failure</returns>
PICK6_API BOOL WINAPI Pick6_InjectDirect(
    DWORD processId, 
    LPCWSTR dllPath,
    LPWSTR errorMessage,
    DWORD errorMessageSize
);

/// <summary>
/// Deploy proxy DLL to target process directory
/// </summary>
/// <param name="processId">Target process ID</param>
/// <param name="proxyDllName">Name of proxy DLL (e.g., "dxgi.dll")</param>
/// <param name="errorMessage">Buffer for error message (optional)</param>
/// <param name="errorMessageSize">Size of error message buffer</param>
/// <returns>TRUE on success, FALSE on failure</returns>
PICK6_API BOOL WINAPI Pick6_DeployProxy(
    DWORD processId,
    LPCWSTR proxyDllName,
    LPWSTR errorMessage,
    DWORD errorMessageSize
);

/// <summary>
/// Find FiveM processes
/// </summary>
/// <param name="processIds">Buffer to receive process IDs</param>
/// <param name="maxProcesses">Maximum number of process IDs to return</param>
/// <param name="processCount">Receives the actual number of processes found</param>
/// <returns>TRUE on success, FALSE on failure</returns>
PICK6_API BOOL WINAPI Pick6_FindFiveMProcesses(
    DWORD* processIds,
    DWORD maxProcesses,
    DWORD* processCount
);

/// <summary>
/// Enable SeDebugPrivilege for current process
/// </summary>
/// <returns>TRUE on success, FALSE on failure</returns>
PICK6_API BOOL WINAPI Pick6_EnableSeDebugPrivilege();

/// <summary>
/// Check if a process is running
/// </summary>
/// <param name="processId">Process ID to check</param>
/// <returns>TRUE if running, FALSE otherwise</returns>
PICK6_API BOOL WINAPI Pick6_IsProcessRunning(DWORD processId);

/// <summary>
/// Check if a process is using a specific module
/// </summary>
/// <param name="processId">Process ID to check</param>
/// <param name="moduleName">Name of module to check for</param>
/// <returns>TRUE if module is loaded, FALSE otherwise</returns>
PICK6_API BOOL WINAPI Pick6_IsProcessUsingModule(DWORD processId, LPCWSTR moduleName);

} // extern "C"
