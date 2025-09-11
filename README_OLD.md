# Pick6 - OBS Game Capture Clone

A real-time game capture and projection application specifically designed for FiveM, providing OBS-like screen capture functionality with borderless fullscreen projection and **automatic injection workflow**.

**🆕 NEW: Unified Entry Point with pick6_loader!**

## Available Implementations

### C++ Version (NEW) - `cpp/`
- **Native Performance**: Maximum speed and efficiency
- **Advanced Stealth**: Hide from Alt+Tab and Task Manager
- **Custom Keybinds**: Fully configurable global hotkeys
- **DirectX 11**: Hardware-accelerated rendering
- **Cross-platform**: Windows (primary) + Linux (development)

### C# Version - `src/` (UNIFIED)
- **Single Executable**: `pick6_loader.exe` replaces multiple separate executables
- **Cross-platform**: Windows GUI + console, Linux/macOS console only
- **Easy Development**: Managed code with .NET 8
- **Windows Forms GUI**: Familiar Windows interface on Windows
- **Console Fallback**: Works everywhere with console interface

## Quick Start

### Unified Entry Point (pick6_loader.exe)

```bash
# Windows: Launch GUI by default
pick6_loader.exe

# Force console mode (any platform)
pick6_loader.exe --console

# Console mode with auto-start
pick6_loader.exe --console --auto-start

# Custom settings in console mode
pick6_loader.exe --console --fps 30 --resolution 1920 1080

# Show help
pick6_loader.exe --help
```

### C++ Version (Recommended for Performance)
```bash
cd cpp
mkdir build && cd build
cmake ..
make -j4        # Linux
# or
cmake --build . --config Release  # Windows
./bin/Pick6CPP
```

### C# Version (Unified pick6_loader)
```bash
# Build the unified loader
dotnet build

# Run with default behavior (GUI on Windows, console elsewhere)
dotnet run --project src/Pick6.Loader

# Or build and run the published executable
dotnet build -c Release
./src/Pick6.Loader/bin/Release/net8.0/pick6_loader
```

## Features

🎮 **FiveM Focused**: Specifically designed for FiveM game capture
🚀 **Vulkan Injection**: Direct frame capture through DLL injection for optimal performance
🖥️ **Borderless Projection**: Fullscreen borderless window projection similar to OBS
⚡ **Real-time Capture**: Low-latency frame capture with configurable FPS
🎯 **Smart Detection**: Auto-detection of FiveM processes with Vulkan support
⚙️ **Configurable**: Adjustable resolution, FPS, and capture settings
📦 **Unified Interface**: Single executable with GUI mode (Windows) and console mode (cross-platform)
🔄 **Automatic Injection**: Click "Start Injection" and system handles everything

## Usage

### GUI Mode (pick6_loader.exe on Windows) - **UNIFIED!**

The new unified interface works exactly like OBS Game Capture:

1. **Start Injection**: Click the blue "Start Injection" button
2. **Automatic Monitoring**: System continuously monitors for FiveM processes
3. **Auto-Injection**: When FiveM starts, injection happens automatically
4. **Real-time Status**: See process detection and injection status in real-time
5. **Settings**: Configure FPS, auto-projection, and other capture settings

**Status Indicators:**
- Process Status: Shows when FiveM is detected
- Capture Status: Shows injection method (Vulkan injection or window capture)
- Ready State: "Ready to inject" → "Monitoring..." → "Successfully injected"

### Console Mode Options

```bash
# Default behavior (GUI on Windows, console elsewhere)
pick6_loader.exe

# Force console mode (any platform)
pick6_loader.exe --console

# Console mode with auto-start
pick6_loader.exe --console --auto-start

# Set custom FPS (console mode)
pick6_loader.exe --console --fps 30

# Set custom resolution (console mode)
pick6_loader.exe --console --resolution 1920 1080 --fps 60

# Show help
pick6_loader.exe --help
```

### Console Interactive Mode

Run `pick6_loader.exe --console` to enter interactive mode:

1. **Scan for FiveM processes** - Detect running FiveM instances (enhanced with Vulkan detection)
2. **Start capture** - Begin capturing frames (uses Vulkan injection when available)
3. **Stop capture** - Stop frame capture
4. **Start projection** - Open borderless projection window
5. **Stop projection** - Close projection window
6. **Configure settings** - Adjust FPS, resolution, etc.
7. **Quick start** - Auto-detect and start everything (prioritizes Vulkan processes)
8. **Show status** - Display current system status including Vulkan support

## Configuration

### GUI Settings
- **Target FPS**: Frame rate for capture (15-120 FPS)
- **Auto-start projection**: Automatically open projection window when capture starts

### Console Settings

- **Target FPS**: Frame rate for capture (default: 60)
- **Resolution**: Output resolution (default: original game resolution)
- **Hardware Acceleration**: Enable/disable hardware acceleration

### Projection Settings

- **Screen Selection**: Choose which monitor for projection
- **VSync**: Enable/disable vertical sync
- **Projection Mode**: Fullscreen, windowed, or borderless

## System Requirements

- **OS**: Windows 10/11 (64-bit)
- **Runtime**: .NET 8.0 or later (included in executable)
- **Memory**: 4GB RAM minimum
- **Graphics**: DirectX 11 compatible graphics card

## How It Works

### OBS-Style Workflow (GUI Mode)
1. **Prep Injection**: User clicks "Start Injection" button
2. **Background Monitoring**: System continuously scans for FiveM processes
3. **Automatic Detection**: When FiveM starts, it's immediately detected
4. **Smart Injection**: Prioritizes Vulkan injection, falls back to window capture
5. **Auto-Projection**: Optionally starts projection window automatically
6. **Status Updates**: Real-time feedback on all operations

### Technical Process
1. **Enhanced Process Detection**: Scans for FiveM processes using multiple methods including Vulkan detection
2. **Vulkan Frame Injection**: Uses DLL injection to capture frames directly from Vulkan API calls for superior performance
3. **Fallback Window Capture**: Falls back to Windows GDI+ for compatibility when injection is not available
4. **Frame Processing**: Processes captured frames with optional scaling and filtering
5. **Projection**: Displays frames in a borderless fullscreen window for immersive viewing

## FiveM Compatibility

Pick6 automatically detects various FiveM versions including:
- FiveM (main release)
- FiveM_b2060, FiveM_b2189, FiveM_b2372
- FiveM_b2545, FiveM_b2612, FiveM_b2699
- FiveM_b2802, FiveM_b2944
- CitizenFX

## Troubleshooting

## Troubleshooting

### No FiveM Process Found
- Ensure FiveM is running and fully loaded
- Check that FiveM has a visible window (not minimized)
- Try running Pick6 as administrator for injection privileges

### Vulkan Injection Failed
- Run Pick6 as administrator to enable DLL injection
- Ensure FiveM is using Vulkan (most modern versions do)
- Check that no antivirus is blocking DLL injection

### Poor Performance
- Use Vulkan injection mode for best performance (requires admin rights)
- Lower the target FPS in settings
- Disable hardware acceleration if experiencing issues
- Close unnecessary applications

### Projection Not Showing
- Check that projection is started (option 4)
- Ensure capture is running (option 2)
- Verify FiveM window is active and visible

## Building from Source

### Standard Build
```bash
# Clone the repository
git clone https://github.com/isogloss/pick66.git
cd pick66

# Build the unified solution
dotnet build

# Run the unified loader (default behavior)
dotnet run --project src/Pick6.Loader

# Or build and run executable directly
dotnet build -c Release
./src/Pick6.Loader/bin/Release/net8.0/pick6_loader
```

### Single-File Publishing

For a single-file distributable executable:

```bash
# Windows (produces pick6_loader.exe)
dotnet publish src/Pick6.Loader -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:PublishReadyToRun=true

# Linux
dotnet publish src/Pick6.Loader -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:PublishReadyToRun=true
```

The published executable will be in `src/Pick6.Loader/bin/Release/net8.0/[runtime]/publish/`

## Project Structure

```
├── cpp/                    # C++ Implementation (NEW)
│   ├── src/
│   │   ├── core/          # Game capture engine
│   │   ├── gui/           # UI and keybind management
│   │   ├── projection/    # Projection window + stealth
│   │   └── main.cpp       # Application entry point
│   ├── build/             # CMake build directory
│   └── README.md          # C++ specific documentation
├── src/                     # C# Implementation (UNIFIED)
│   ├── Pick6.Core/          # Core capture engine (class library)
│   ├── Pick6.Projection/    # Projection window logic (class library)
│   ├── Pick6.Loader/        # 🆕 UNIFIED EXECUTABLE (pick6_loader.exe)
│   ├── Pick6.UI/            # ⚠️ DEPRECATED - use Pick6.Loader --console
│   ├── Pick6.GUI/           # ⚠️ DEPRECATED - use Pick6.Loader (default)
│   └── Pick6.Launcher/      # ⚠️ DEPRECATED - use Pick6.Loader
└── README.md              # This file
```

## License

This project is open source and available under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

---

**Note**: This application is designed for legitimate screen capture purposes. Please respect the terms of service of any games or applications you use with Pick6.