# Pick6 Native - C++ DLL Injector

A native, unmanaged C++ DLL for game process injection, specifically designed for FiveM. This is a rewrite of the C# injection functionality to create a more suitable DLL for injection scenarios.

## Features

- **Native C++ Implementation**: Unmanaged code with minimal dependencies
- **Direct LoadLibrary Injection**: Classic CreateRemoteThread + LoadLibraryW injection
- **Proxy DLL Support**: Deploy proxy DLLs (dxgi.dll, d3d11.dll, vulkan-1.dll) for graphics API hooking
- **Process Privilege Management**: Automatic SeDebugPrivilege elevation
- **FiveM Process Detection**: Find and identify FiveM game processes
- **Module Enumeration**: Check which graphics APIs a process is using

## Building

### Prerequisites

- CMake 3.15 or higher
- Visual Studio 2019 or higher (with C++ development tools)
- Windows SDK

### Build Instructions

#### Using Visual Studio

1. Open the folder in Visual Studio (File -> Open -> Folder)
2. CMake will automatically configure the project
3. Build using Ctrl+Shift+B

#### Using Command Line

```cmd
# Create build directory
mkdir build
cd build

# Configure CMake (Release build)
cmake .. -G "Visual Studio 16 2019" -A x64

# Build
cmake --build . --config Release

# The DLL will be in: build/Release/Pick6Native.dll
```

#### Using CMake GUI

1. Open CMake GUI
2. Set source directory to this folder
3. Set build directory to `build` subfolder
4. Click "Configure" and select your Visual Studio version
5. Click "Generate"
6. Open the generated solution in Visual Studio and build

## API Reference

### Initialization

```cpp
BOOL Pick6_Initialize();
VOID Pick6_Cleanup();
```

### Injection Functions

```cpp
// Direct injection via LoadLibrary
BOOL Pick6_InjectDirect(
    DWORD processId,
    LPCWSTR dllPath,
    LPWSTR errorMessage,
    DWORD errorMessageSize
);

// Deploy proxy DLL
BOOL Pick6_DeployProxy(
    DWORD processId,
    LPCWSTR proxyDllName,  // e.g., L"dxgi.dll"
    LPWSTR errorMessage,
    DWORD errorMessageSize
);
```

### Process Utilities

```cpp
// Find FiveM processes
BOOL Pick6_FindFiveMProcesses(
    DWORD* processIds,
    DWORD maxProcesses,
    DWORD* processCount
);

// Check if process is running
BOOL Pick6_IsProcessRunning(DWORD processId);

// Check if process uses a specific module
BOOL Pick6_IsProcessUsingModule(DWORD processId, LPCWSTR moduleName);
```

### Privilege Management

```cpp
// Enable SeDebugPrivilege
BOOL Pick6_EnableSeDebugPrivilege();
```

## Usage Example

```cpp
#include "Pick6Export.h"

int main() {
    // Initialize
    Pick6_Initialize();
    
    // Enable debug privilege
    Pick6_EnableSeDebugPrivilege();
    
    // Find FiveM processes
    DWORD processIds[32];
    DWORD count = 0;
    Pick6_FindFiveMProcesses(processIds, 32, &count);
    
    if (count > 0) {
        // Inject into first process
        wchar_t errorMsg[512];
        BOOL success = Pick6_InjectDirect(
            processIds[0],
            L"C:\\Path\\To\\Hook.dll",
            errorMsg,
            512
        );
        
        if (!success) {
            wprintf(L"Injection failed: %s\n", errorMsg);
        }
    }
    
    // Cleanup
    Pick6_Cleanup();
    return 0;
}
```

## Architecture

### Core Components

1. **Pick6Injector.h/cpp**: Main injection logic and utilities
   - `Injector` class: Core injection functionality
   - `PrivilegeManager` class: Windows privilege management

2. **Pick6Export.h/cpp**: C API exports
   - Exported C functions for easy interop
   - Global injector instance management

3. **DllMain.cpp**: DLL entry point
   - Initialization and cleanup on load/unload

### Injection Strategies

#### Direct Injection
1. Open target process with `OpenProcess`
2. Allocate memory in target with `VirtualAllocEx`
3. Write DLL path to target memory with `WriteProcessMemory`
4. Get `LoadLibraryW` address from kernel32.dll
5. Create remote thread with `CreateRemoteThread`
6. Wait for thread completion

#### Proxy DLL Injection
1. Locate target process directory
2. Backup original DLL (e.g., dxgi.dll)
3. Copy our proxy DLL to replace original
4. When process loads the graphics API, it loads our proxy
5. Proxy forwards calls while hooking/monitoring

## Integration with C# Code

The C# code can P/Invoke these exported functions:

```csharp
[DllImport("Pick6Native.dll")]
public static extern bool Pick6_InjectDirect(
    uint processId,
    [MarshalAs(UnmanagedType.LPWStr)] string dllPath,
    [MarshalAs(UnmanagedType.LPWStr)] StringBuilder errorMessage,
    uint errorMessageSize
);
```

## Dependencies

The DLL uses only standard Windows APIs:
- `kernel32.dll`: Process and memory management
- `advapi32.dll`: Token and privilege management
- `psapi.dll`: Process enumeration

**No external dependencies** - fully self-contained native DLL.

## Build Artifacts

- `Pick6Native.dll`: Main injection DLL
- `Pick6Native.lib`: Import library for linking
- `Pick6Test.exe`: Test executable (optional)

## Security Considerations

- Requires Administrator privileges for SeDebugPrivilege
- Process injection is detected by some antivirus software
- Proxy DLL replacement requires write access to game directory
- Always backup original DLLs before replacement

## Comparison with C# Version

| Feature | C# Version | C++ Version |
|---------|-----------|-------------|
| Managed Code | Yes | No (Native) |
| .NET Required | Yes | No |
| DLL Size | Larger | Smaller |
| Dependencies | .NET Runtime | None (Windows APIs only) |
| Injection Suitability | Lower | Higher |
| Performance | Good | Excellent |
| Antivirus Detection | Higher | Lower |

## License

Part of the Pick6 project. See main project README for license information.
