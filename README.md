# Pick6 - OBS Game Capture Clone

A real-time game capture and projection application specifically designed for FiveM, providing OBS-like screen capture functionality with borderless fullscreen projection and **automatic injection workflow**.

## Features

🎮 **FiveM Focused**: Specifically designed for FiveM game capture
🚀 **Vulkan Injection**: Direct frame capture through DLL injection for optimal performance
🖥️ **Borderless Projection**: Fullscreen borderless window projection similar to OBS
⚡ **Real-time Capture**: Low-latency frame capture with configurable FPS
🎯 **Smart Detection**: Auto-detection of FiveM processes with Vulkan support
⚙️ **Configurable**: Adjustable resolution, FPS, and capture settings
📦 **Dual Interface**: GUI mode (OBS-style) and console mode
🔄 **Automatic Injection**: Click "Start Injection" and system handles everything

## Quick Start

### GUI Mode (Recommended - OBS-style interface)
1. **Download and Run**: Simply run `Pick6.GUI.exe` 
2. **Click "Start Injection"**: This preps the system for automatic monitoring
3. **Start FiveM**: The system automatically detects and injects when FiveM starts
4. **Enjoy**: Your FiveM game will be projected in a borderless window

### Console Mode (Legacy)
1. **Download and Run**: Run `Pick6.Launcher.exe` (attempts GUI first, falls back to console)
2. **Start FiveM**: Make sure FiveM is running
3. **Quick Start**: Choose option 7 in the menu for automatic setup
4. **Enjoy**: Your FiveM game will be projected in a borderless window

## Usage

### GUI Mode (Pick6.GUI.exe) - **NEW!**

The new GUI interface works exactly like OBS Game Capture:

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
# GUI Mode (default - launches Pick6.GUI.exe if available)
Pick6.Launcher.exe

# Force console mode
Pick6.Launcher.exe --console

# Auto-start capture and projection (console mode)
Pick6.Launcher.exe --console --auto-start

# Set custom FPS (console mode)
Pick6.Launcher.exe --console --fps 30

# Set custom resolution (console mode)
Pick6.Launcher.exe --console --resolution 1920 1080 --fps 60

# Show help
Pick6.Launcher.exe --help
```

### Console Interactive Mode

Run `Pick6.Launcher.exe --console` to enter interactive mode:

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

```bash
# Clone the repository
git clone https://github.com/isogloss/pick66.git
cd pick66

# Build the solution
dotnet build

# Run the launcher
dotnet run --project src/Pick6.Launcher
```

## Project Structure

```
src/
├── Pick6.Core/          # Core capture engine
├── Pick6.UI/            # User interface
├── Pick6.Projection/    # Projection window logic
└── Pick6.Launcher/      # Main executable
```

## License

This project is open source and available under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

---

**Note**: This application is designed for legitimate screen capture purposes. Please respect the terms of service of any games or applications you use with Pick6.