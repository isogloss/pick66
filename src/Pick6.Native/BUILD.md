# Building Pick6Native on Windows

This guide provides multiple ways to build the Pick6Native C++ DLL on Windows.

## Prerequisites

### Required
- **Windows 10/11** (64-bit)
- **Visual Studio 2019 or later** with:
  - Desktop development with C++ workload
  - Windows 10 SDK
- **CMake 3.15 or later**

### Optional
- **Git** (for cloning the repository)

## Quick Start

### Method 1: Using the Build Script (Easiest)

1. Open the `src/Pick6.Native` folder in File Explorer
2. Double-click `build.bat`
3. The script will:
   - Configure CMake
   - Build Release and Debug configurations
   - Offer to run tests

Output will be in `src/Pick6.Native/build/Release/Pick6Native.dll`

### Method 2: Using Visual Studio

1. **Open Visual Studio**
2. Select **File → Open → Folder**
3. Navigate to and open `src/Pick6.Native`
4. Visual Studio will automatically detect CMake and configure the project
5. Select **Release** or **Debug** from the configuration dropdown
6. Press **Ctrl+Shift+B** to build
7. The DLL will be in `out/build/x64-Release/Pick6Native.dll`

### Method 3: Using CMake GUI

1. **Launch CMake GUI**
2. Set **"Where is the source code"** to `path/to/pick66/src/Pick6.Native`
3. Set **"Where to build the binaries"** to `path/to/pick66/src/Pick6.Native/build`
4. Click **Configure**
   - Select your Visual Studio version (e.g., "Visual Studio 16 2019")
   - Select platform: **x64**
   - Click **Finish**
5. Click **Generate**
6. Click **Open Project** to open in Visual Studio
7. Build from Visual Studio (F7 or Ctrl+Shift+B)

### Method 4: Command Line (Developer Command Prompt)

```cmd
# Open "Developer Command Prompt for VS 2019" (or your VS version)

# Navigate to the native source directory
cd path\to\pick66\src\Pick6.Native

# Create and enter build directory
mkdir build
cd build

# Configure with CMake (targeting Visual Studio 2019, x64)
cmake .. -G "Visual Studio 16 2019" -A x64

# Build Release configuration
cmake --build . --config Release

# Build Debug configuration  
cmake --build . --config Debug

# The DLL will be in build/Release/Pick6Native.dll
```

## Build Options

### Configuration Types

- **Release**: Optimized build for production use
  - Smaller size
  - Maximum performance
  - No debug symbols
  
- **Debug**: Development build with debugging support
  - Debug symbols included
  - No optimizations
  - Easier to debug

### CMake Options

```cmd
# Disable test executable
cmake .. -G "Visual Studio 16 2019" -A x64 -DBUILD_TESTS=OFF

# Specify different generator
cmake .. -G "Visual Studio 17 2022" -A x64
```

## Verification

After building, verify the output:

```cmd
# List output files
dir build\Release

# Expected files:
# - Pick6Native.dll    (main DLL)
# - Pick6Native.lib    (import library)
# - Pick6Native.pdb    (debug symbols, Release config)
# - Pick6Test.exe      (test executable)
```

## Testing

### Running the Test Executable

```cmd
cd build\Release
Pick6Test.exe
```

The test will:
1. Initialize the injector
2. Enable SeDebugPrivilege
3. Search for FiveM processes
4. Display found processes and their modules

### Manual Testing with C#

Create a simple C# console app:

```csharp
using System;
using System.Runtime.InteropServices;

class Program {
    [DllImport("Pick6Native.dll")]
    static extern bool Pick6_Initialize();
    
    [DllImport("Pick6Native.dll")]
    static extern bool Pick6_EnableSeDebugPrivilege();
    
    static void Main() {
        Console.WriteLine("Testing Pick6Native...");
        
        if (Pick6_Initialize()) {
            Console.WriteLine("✓ Initialized");
            
            if (Pick6_EnableSeDebugPrivilege()) {
                Console.WriteLine("✓ SeDebugPrivilege enabled");
            }
        }
    }
}
```

Build and run with Pick6Native.dll in the same directory.

## Troubleshooting

### "CMake not found"
- Install CMake from https://cmake.org/download/
- Add to PATH or use Visual Studio's CMake

### "No CMAKE_CXX_COMPILER could be found"
- Install Visual Studio with "Desktop development with C++" workload
- Make sure to install "MSVC v142 - VS 2019 C++ x64/x86 build tools"

### "Windows SDK not found"
- Install Windows 10 SDK via Visual Studio Installer
- Typical location: `C:\Program Files (x86)\Windows Kits\10`

### "Cannot open include file: 'windows.h'"
- Windows SDK is not installed or not found
- Re-run Visual Studio Installer and ensure Windows SDK is checked

### "LNK1104: cannot open file 'kernel32.lib'"
- Windows SDK libraries not found
- Check SDK installation in Visual Studio Installer

### Build succeeds but DLL crashes
- Make sure you're building for x64 (not x86)
- Verify Windows SDK version is compatible with your OS

### "The code execution cannot proceed because VCRUNTIME140.dll was not found"
- Install Visual C++ Redistributable
- Or build with static runtime (already configured in CMakeLists.txt)

## Clean Build

To perform a clean build:

```cmd
# Remove build directory
rmdir /s /q build

# Re-run build script
build.bat
```

Or in Visual Studio:
1. **Build → Clean Solution**
2. **Build → Rebuild Solution**

## Advanced: Cross-Compilation

While not recommended, you can cross-compile from Linux using MinGW:

```bash
# Install MinGW
sudo apt-get install mingw-w64

# Configure for cross-compilation
mkdir build && cd build
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/mingw-w64-x86_64.cmake

# Build
make
```

Note: This may have compatibility issues. Building on Windows is strongly recommended.

## Integration with Main Project

After building Pick6Native.dll:

1. Copy `build/Release/Pick6Native.dll` to the Pick6.Loader output directory
2. The C# code can now use `NativeInjector.cs` to call into the native DLL
3. Build the main C# project normally with `dotnet build`

## Next Steps

- See [README.md](README.md) for API documentation
- See [../Pick6.Core/NativeInjector.cs](../Pick6.Core/NativeInjector.cs) for C# integration
- Contribute improvements via pull requests
