#include "Pick6Injector.h"
#include <tlhelp32.h>
#include <psapi.h>
#include <sstream>
#include <filesystem>

namespace fs = std::filesystem;

namespace Pick6 {

// Constructor
Injector::Injector() {
    // Initialize if needed
}

// Destructor
Injector::~Injector() {
    // Cleanup if needed
}

// Main injection method with strategy selection
InjectionResult Injector::InjectIntoProcess(DWORD processId, const std::wstring& dllPath) {
    if (!EnableSeDebugPrivilege()) {
        return InjectionResult::Failed(InjectionStrategy::Direct, 
            L"Failed to enable SeDebugPrivilege. Please run as Administrator.", 
            GetLastError());
    }

    // Try direct injection first
    return InjectDirect(processId, dllPath);
}

// Direct LoadLibrary injection via CreateRemoteThread
InjectionResult Injector::InjectDirect(DWORD processId, const std::wstring& dllPath) {
    // Verify DLL exists
    if (!fs::exists(dllPath)) {
        return InjectionResult::Failed(InjectionStrategy::Direct, 
            L"DLL file not found: " + dllPath);
    }

    // Open target process
    HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, processId);
    if (hProcess == NULL) {
        return InjectionResult::Failed(InjectionStrategy::Direct,
            L"Failed to open target process: " + GetLastErrorAsString(),
            GetLastError());
    }

    bool success = PerformDirectInjection(hProcess, dllPath);
    CloseHandle(hProcess);

    if (success) {
        return InjectionResult::Succeeded(InjectionStrategy::Direct, 
            L"Direct LoadLibrary injection completed");
    } else {
        return InjectionResult::Failed(InjectionStrategy::Direct,
            L"Direct LoadLibrary injection failed");
    }
}

// Perform the actual direct injection
bool Injector::PerformDirectInjection(HANDLE hProcess, const std::wstring& dllPath) {
    // Allocate memory in target process for DLL path
    size_t pathSize = (dllPath.length() + 1) * sizeof(wchar_t);
    LPVOID pRemoteMemory = VirtualAllocEx(hProcess, NULL, pathSize, 
        MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    
    if (pRemoteMemory == NULL) {
        return false;
    }

    // Write DLL path to allocated memory
    SIZE_T bytesWritten = 0;
    if (!WriteProcessMemory(hProcess, pRemoteMemory, dllPath.c_str(), 
        pathSize, &bytesWritten)) {
        VirtualFreeEx(hProcess, pRemoteMemory, 0, MEM_RELEASE);
        return false;
    }

    // Get LoadLibraryW address from kernel32.dll
    HMODULE hKernel32 = GetModuleHandleW(L"kernel32.dll");
    if (hKernel32 == NULL) {
        VirtualFreeEx(hProcess, pRemoteMemory, 0, MEM_RELEASE);
        return false;
    }

    LPTHREAD_START_ROUTINE pLoadLibrary = 
        (LPTHREAD_START_ROUTINE)GetProcAddress(hKernel32, "LoadLibraryW");
    
    if (pLoadLibrary == NULL) {
        VirtualFreeEx(hProcess, pRemoteMemory, 0, MEM_RELEASE);
        return false;
    }

    // Create remote thread to load the DLL
    DWORD threadId = 0;
    HANDLE hThread = CreateRemoteThread(hProcess, NULL, 0, pLoadLibrary, 
        pRemoteMemory, 0, &threadId);
    
    if (hThread == NULL) {
        VirtualFreeEx(hProcess, pRemoteMemory, 0, MEM_RELEASE);
        return false;
    }

    // Wait for the thread to complete
    WaitForSingleObject(hThread, 5000); // Wait up to 5 seconds
    
    // Cleanup
    CloseHandle(hThread);
    VirtualFreeEx(hProcess, pRemoteMemory, 0, MEM_RELEASE);

    return true;
}

// Proxy DLL injection
InjectionResult Injector::InjectProxy(DWORD processId, const std::wstring& proxyDllName) {
    // Get process directory
    std::wstring processDir = GetProcessDirectory(processId);
    if (processDir.empty()) {
        return InjectionResult::Failed(InjectionStrategy::DxgiProxy,
            L"Could not determine process directory");
    }

    // Check if directory is writable
    if (!IsDirectoryWritable(processDir)) {
        return InjectionResult::Failed(InjectionStrategy::DxgiProxy,
            L"Process directory is not writable. Please run as Administrator.");
    }

    // Construct target path
    std::wstring targetPath = processDir + L"\\" + proxyDllName;
    
    // Deploy proxy DLL
    return DeployProxyDll(proxyDllName, targetPath);
}

// Deploy proxy DLL to target location
InjectionResult Injector::DeployProxyDll(const std::wstring& proxyDllName, const std::wstring& targetPath) {
    // Get our proxy DLL path
    std::wstring ourProxyPath = GetProxyDllPath(proxyDllName);
    if (!fs::exists(ourProxyPath)) {
        return InjectionResult::Failed(InjectionStrategy::DxgiProxy,
            L"Proxy DLL not found: " + ourProxyPath);
    }

    // Create backup of original if it exists
    std::wstring backupPath = targetPath;
    size_t dotPos = backupPath.find_last_of(L'.');
    if (dotPos != std::wstring::npos) {
        backupPath.insert(dotPos, L".original");
    } else {
        backupPath += L".original";
    }

    if (fs::exists(targetPath) && !fs::exists(backupPath)) {
        if (!CreateBackup(targetPath, backupPath)) {
            return InjectionResult::Failed(InjectionStrategy::DxgiProxy,
                L"Failed to create backup of original DLL");
        }
    }

    // Copy our proxy DLL to target location
    try {
        fs::copy_file(ourProxyPath, targetPath, fs::copy_options::overwrite_existing);
        return InjectionResult::Succeeded(InjectionStrategy::DxgiProxy,
            L"Proxy DLL deployed successfully");
    }
    catch (const std::exception& e) {
        return InjectionResult::Failed(InjectionStrategy::DxgiProxy,
            L"Failed to copy proxy DLL to target location");
    }
}

// Get the path to our proxy DLL
std::wstring Injector::GetProxyDllPath(const std::wstring& proxyDllName) {
    std::wstring exePath = GetExecutablePath();
    std::wstring baseDir = fs::path(exePath).parent_path().wstring();

    // Try exact filename first
    std::wstring exactPath = baseDir + L"\\" + proxyDllName;
    if (fs::exists(exactPath)) {
        return exactPath;
    }

    // Try with Pick6 prefix naming convention
    std::wstring baseName = fs::path(proxyDllName).stem().wstring();
    std::wstring prefixedName = L"Pick6" + baseName + L"Proxy.dll";
    std::wstring prefixedPath = baseDir + L"\\" + prefixedName;
    if (fs::exists(prefixedPath)) {
        return prefixedPath;
    }

    // Try in proxy subdirectory
    std::wstring proxyDir = baseDir + L"\\proxies";
    if (fs::exists(proxyDir)) {
        std::wstring proxySubPath = proxyDir + L"\\" + proxyDllName;
        if (fs::exists(proxySubPath)) {
            return proxySubPath;
        }
    }

    // Return exact path as fallback
    return exactPath;
}

// Get the directory of a process
std::wstring Injector::GetProcessDirectory(DWORD processId) {
    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, processId);
    if (hProcess == NULL) {
        return L"";
    }

    wchar_t processPath[MAX_PATH] = { 0 };
    DWORD pathLen = MAX_PATH;
    
    if (QueryFullProcessImageNameW(hProcess, 0, processPath, &pathLen)) {
        CloseHandle(hProcess);
        std::wstring path(processPath);
        return fs::path(path).parent_path().wstring();
    }

    CloseHandle(hProcess);
    return L"";
}

// Check if directory is writable
bool Injector::IsDirectoryWritable(const std::wstring& directory) {
    // Try to create a temporary file
    std::wstring testFile = directory + L"\\.pick6_write_test.tmp";
    
    HANDLE hFile = CreateFileW(testFile.c_str(), GENERIC_WRITE, 0, NULL, 
        CREATE_ALWAYS, FILE_ATTRIBUTE_TEMPORARY, NULL);
    
    if (hFile == INVALID_HANDLE_VALUE) {
        return false;
    }

    CloseHandle(hFile);
    DeleteFileW(testFile.c_str());
    return true;
}

// Create backup of a file
bool Injector::CreateBackup(const std::wstring& originalPath, const std::wstring& backupPath) {
    try {
        fs::copy_file(originalPath, backupPath, fs::copy_options::skip_existing);
        return true;
    }
    catch (const std::exception&) {
        return false;
    }
}

// Restore from backup
bool Injector::RestoreFromBackup(const std::wstring& backupPath, const std::wstring& targetPath) {
    try {
        if (fs::exists(backupPath)) {
            fs::copy_file(backupPath, targetPath, fs::copy_options::overwrite_existing);
            fs::remove(backupPath);
            return true;
        }
    }
    catch (const std::exception&) {
    }
    return false;
}

// Enable SeDebugPrivilege
bool Injector::EnableSeDebugPrivilege() {
    return PrivilegeManager::EnableSeDebugPrivilege();
}

// Find FiveM processes
std::vector<ProcessInfo> Injector::FindFiveMProcesses() {
    std::vector<ProcessInfo> processes;

    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE) {
        return processes;
    }

    PROCESSENTRY32W pe32;
    pe32.dwSize = sizeof(PROCESSENTRY32W);

    if (Process32FirstW(hSnapshot, &pe32)) {
        do {
            std::wstring processName(pe32.szExeFile);
            
            // Look for FiveM-related processes
            if (processName.find(L"FiveM") != std::wstring::npos ||
                processName.find(L"fivem") != std::wstring::npos) {
                
                ProcessInfo info;
                info.ProcessId = pe32.th32ProcessID;
                info.ProcessName = processName;
                processes.push_back(info);
            }
        } while (Process32NextW(hSnapshot, &pe32));
    }

    CloseHandle(hSnapshot);
    return processes;
}

// Check if process is running
bool Injector::IsProcessRunning(DWORD processId) {
    HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, processId);
    if (hProcess == NULL) {
        return false;
    }

    DWORD exitCode = 0;
    bool running = GetExitCodeProcess(hProcess, &exitCode) && exitCode == STILL_ACTIVE;
    CloseHandle(hProcess);
    return running;
}

// Check if process is using a specific module
bool Injector::IsProcessUsingModule(DWORD processId, const std::wstring& moduleName) {
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, processId);
    if (hSnapshot == INVALID_HANDLE_VALUE) {
        return false;
    }

    MODULEENTRY32W me32;
    me32.dwSize = sizeof(MODULEENTRY32W);

    bool found = false;
    if (Module32FirstW(hSnapshot, &me32)) {
        do {
            std::wstring modName(me32.szModule);
            std::transform(modName.begin(), modName.end(), modName.begin(), ::towlower);
            
            std::wstring searchName = moduleName;
            std::transform(searchName.begin(), searchName.end(), searchName.begin(), ::towlower);

            if (modName.find(searchName) != std::wstring::npos) {
                found = true;
                break;
            }
        } while (Module32NextW(hSnapshot, &me32));
    }

    CloseHandle(hSnapshot);
    return found;
}

// Get last error as string
std::wstring Injector::GetLastErrorAsString(DWORD errorCode) {
    if (errorCode == 0) {
        errorCode = GetLastError();
    }

    if (errorCode == 0) {
        return L"No error";
    }

    LPWSTR messageBuffer = nullptr;
    size_t size = FormatMessageW(
        FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL, errorCode, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPWSTR)&messageBuffer, 0, NULL);

    std::wstring message(messageBuffer, size);
    LocalFree(messageBuffer);

    return message;
}

// Get current executable path
std::wstring Injector::GetExecutablePath() {
    wchar_t buffer[MAX_PATH] = { 0 };
    GetModuleFileNameW(NULL, buffer, MAX_PATH);
    return std::wstring(buffer);
}

// PrivilegeManager implementation
bool PrivilegeManager::EnableSeDebugPrivilege() {
    HANDLE hToken = NULL;
    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken)) {
        return false;
    }

    bool result = SetPrivilege(hToken, SE_DEBUG_NAME, TRUE);
    CloseHandle(hToken);
    return result;
}

bool PrivilegeManager::SetPrivilege(HANDLE hToken, LPCTSTR lpszPrivilege, BOOL bEnablePrivilege) {
    TOKEN_PRIVILEGES tp;
    LUID luid;

    if (!LookupPrivilegeValue(NULL, lpszPrivilege, &luid)) {
        return false;
    }

    tp.PrivilegeCount = 1;
    tp.Privileges[0].Luid = luid;
    tp.Privileges[0].Attributes = bEnablePrivilege ? SE_PRIVILEGE_ENABLED : 0;

    if (!AdjustTokenPrivileges(hToken, FALSE, &tp, sizeof(TOKEN_PRIVILEGES), 
        (PTOKEN_PRIVILEGES)NULL, (PDWORD)NULL)) {
        return false;
    }

    if (GetLastError() == ERROR_NOT_ALL_ASSIGNED) {
        return false;
    }

    return true;
}

} // namespace Pick6
