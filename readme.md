# pick66

a game capture and projection tool for fivem with enhanced multi-strategy injection. features real-time borderless projection, automatic privilege elevation, and multi-dll proxy injection support for various graphics apis including dxgi, d3d11, and vulkan.

## 🆕 Native C++ Injector

Pick6 now includes a **native C++ DLL** (`Pick6Native.dll`) for improved DLL injection:
- ✅ **Native, unmanaged code** - no .NET runtime required
- ✅ **Minimal dependencies** - uses only Windows APIs
- ✅ **Better suited for injection** - smaller footprint and less antivirus detection
- ✅ **Full API support** - direct injection and proxy DLL deployment

See [`src/Pick6.Native/README.md`](src/Pick6.Native/README.md) for build instructions and API documentation.

## Installation

### Option 1: Download Pre-Built Executable (Recommended)

1. Go to the [Releases](https://github.com/isogloss/pick66/releases) page
2. Download the latest `Pick6-v{version}.exe`
3. Run the executable - **no installation required!**

The executable is fully self-contained:
- ✅ **No .NET runtime installation needed** - all dependencies are embedded
- ✅ **Single file** - just download and run
- ✅ **No installation required** - portable and ready to use
- ✅ **Built with Costura.Fody** - all managed dependencies embedded

### Option 2: Build from Source

Run `install.bat` to automatically build and install Pick66 to your Downloads folder. The installer is fully self-bootstrapping and will:

1. **Automatically detect and install .NET 8 SDK** if not present or if an older version is found
2. Download source code from GitHub
3. Build the Pick66 Loader application
4. Install to your Downloads folder

#### Prerequisites for Building

The installer requires:
- Windows operating system
- Internet connection
- PowerShell (built into Windows)

**No manual installation of .NET SDK is required** - the installer will automatically download and install .NET 8 SDK to a temporary location if needed.

#### Manual Build

If you prefer to install .NET 8 SDK manually, you can download it from: https://dotnet.microsoft.com/download/dotnet/8.0

⚠️ **Important**: Before building, ensure native DLLs are present. See [DLL_DEPLOYMENT_GUIDE.md](DLL_DEPLOYMENT_GUIDE.md) for details.

```bash
# Build native components first
cd src/Pick6.Native
build.bat
copy build\Release\Pick6Native.dll ..\Pick6.Loader\

# Build the loader (requires Pick6VulkanHook.dll to be present)
dotnet publish src/Pick6.Loader/Pick6.Loader.csproj --configuration Release --runtime win-x64 --self-contained true
```

For detailed build instructions, see:
- [DLL_DEPLOYMENT_GUIDE.md](DLL_DEPLOYMENT_GUIDE.md) - Native DLL deployment guide
- [src/Pick6.Loader/README_DLL_REQUIREMENTS.md](src/Pick6.Loader/README_DLL_REQUIREMENTS.md) - DLL requirements for building

## System Requirements

- Windows 10/11 (64-bit)
- Administrator privileges recommended for game injection features
- No additional dependencies required (for pre-built executable)

## Usage

Simply run the executable:

```bash
Pick6.exe                    # Start with GUI interface
Pick6.exe --help            # Show help information
Pick6.exe --check-updates   # Check for updates at startup
```

The application will:
1. Launch with a GUI interface
2. Detect FiveM processes automatically
3. Provide options for game capture and projection
4. Support multi-strategy injection for various graphics APIs

## Features

- 🎮 **Game capture and projection for FiveM**
- 💉 **Multi-strategy injection** - direct + proxy DLL fallbacks
- 🎨 **Graphics API support** - DXGI, D3D11, and Vulkan
- 🖥️ **Real-time borderless projection**
- 🔐 **Automatic privilege elevation**
- 🎯 **GUI interface** with system tray support
- 📦 **Single-file executable** - no installation needed
- 🔄 **Automatic updates** - checks GitHub releases for new versions

## Auto-Update Feature

Pick6 includes an automatic update mechanism that checks for new releases on startup:

- **Automatic**: Updates are checked automatically when you launch the application
- **Safe**: Uses a helper script to safely replace the running executable
- **Graceful**: Handles network failures without blocking the application
- **Configurable**: Can be disabled with the `--skip-loader-update` flag

### How It Works

1. On startup, Pick6 checks the GitHub repository for the latest release
2. If a newer version is available, it downloads the release `.zip` file
3. The new `loader.exe` is extracted to a temporary location
4. A batch script (`updater.bat`) is created to perform the file swap
5. The updater script runs after the current application exits
6. The application restarts automatically with the new version

### Command-Line Options

```bash
loader.exe                      # Start with auto-update enabled
loader.exe --skip-loader-update # Skip the update check
loader.exe --check-updates-only # Check for updates and exit
```

### For Developers

- The current version is defined in `Program.cs` as `LOADER_VERSION`
- Update checks can be enabled/disabled via the `ENABLE_LOADER_AUTO_UPDATE` flag
- The update service uses the GitHub API to fetch release information
- Version comparison is done by matching the release tag (without 'v' prefix) with the current version

## Project Architecture

Pick6 consists of three main components:

### 1. Pick6.Loader (C#)
The main application providing the user interface and orchestration:
- GUI and console menu interfaces
- Process detection and monitoring
- Configuration and settings management
- Auto-update functionality

### 2. Pick6.Core (C#)
Core business logic and injection orchestration:
- Multi-strategy injection with fallback
- Game capture engine
- Projection window management
- Frame statistics and diagnostics

### 3. Pick6.Native (C++)
**NEW**: Native injection DLL for better compatibility:
- Unmanaged, native C++ code
- Direct LoadLibrary injection
- Proxy DLL deployment and management
- Minimal dependencies (Windows APIs only)
- Better suited for DLL injection scenarios

The C# code can call into the C++ native DLL via P/Invoke for injection operations, combining the ease of C# development with the performance and compatibility advantages of native code.

## Troubleshooting

### "Core hook DLL not found" Error

If you see this error:
```
[Error] Core hook DLL not found: C:\Users\...\AppData\Local\Temp\.net\...
```

**Quick Fix:**
1. Ensure `Pick6VulkanHook.dll` is in the same directory as `loader.exe`
2. Right-click the DLL → Properties → Unblock (if option is present)
3. Check that antivirus isn't blocking the file

**Detailed Guide:** See [TROUBLESHOOTING_DLL_NOT_FOUND.md](TROUBLESHOOTING_DLL_NOT_FOUND.md)

### Other Issues

- **Application won't start**: Run as Administrator
- **FiveM not detected**: Make sure FiveM is running before launching Pick6
- **Injection fails**: Disable antivirus temporarily and try again
- **Black screen in projection**: Check graphics settings and monitor configuration

For more help:
- Read [DLL_DEPLOYMENT_GUIDE.md](DLL_DEPLOYMENT_GUIDE.md)
- Check existing [GitHub Issues](https://github.com/isogloss/pick66/issues)
- Open a new issue with detailed logs