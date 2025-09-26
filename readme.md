# pick66

a game capture and projection tool for fivem with enhanced multi-strategy injection. features real-time borderless projection, automatic privilege elevation, and multi-dll proxy injection support for various graphics apis including dxgi, d3d11, and vulkan.

## Installation

Run `install.bat` to automatically build and install Pick66 to your Downloads folder. The installer is fully self-bootstrapping and will:

1. **Automatically detect and install .NET 8 SDK** if not present or if an older version is found
2. Download source code from GitHub
3. Build the Pick66 Loader application
4. Install to your Downloads folder

### Prerequisites

The installer requires:
- Windows operating system
- Internet connection
- PowerShell (built into Windows)

**No manual installation of .NET SDK is required** - the installer will automatically download and install .NET 8 SDK to a temporary location if needed.

### Manual Installation

If you prefer to install .NET 8 SDK manually, you can download it from: https://dotnet.microsoft.com/download/dotnet/8.0