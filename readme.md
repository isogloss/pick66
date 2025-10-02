# pick66

a game capture and projection tool for fivem with enhanced multi-strategy injection. features real-time borderless projection, automatic privilege elevation, and multi-dll proxy injection support for various graphics apis including dxgi, d3d11, and vulkan.

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

Then build with:
```bash
dotnet publish src/Pick6.Loader/Pick6.Loader.csproj --configuration Release --runtime win-x64 --self-contained true
```

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