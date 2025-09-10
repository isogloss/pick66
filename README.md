# Pick6 - OBS Game Capture Clone

A real-time game capture and projection application specifically designed for FiveM, providing OBS-like screen capture functionality with borderless fullscreen projection.

## Features

🎮 **FiveM Focused**: Specifically designed for FiveM game capture
🖥️ **Borderless Projection**: Fullscreen borderless window projection similar to OBS
⚡ **Real-time Capture**: Low-latency frame capture with configurable FPS
🎯 **Auto-Detection**: Automatic FiveM process detection and targeting
⚙️ **Configurable**: Adjustable resolution, FPS, and capture settings
📦 **Single Executable**: Self-contained launcher with no external dependencies

## Quick Start

1. **Download and Run**: Simply run `Pick6.Launcher.exe`
2. **Start FiveM**: Make sure FiveM is running
3. **Quick Start**: Choose option 7 in the menu for automatic setup
4. **Enjoy**: Your FiveM game will be projected in a borderless window

## Usage

### Command Line Options

```bash
# Auto-start capture and projection
Pick6.Launcher.exe --auto-start

# Set custom FPS
Pick6.Launcher.exe --fps 30

# Set custom resolution
Pick6.Launcher.exe --resolution 1920 1080 --fps 60

# Show help
Pick6.Launcher.exe --help
```

### Interactive Mode

Run without arguments to enter interactive mode:

1. **Scan for FiveM processes** - Detect running FiveM instances
2. **Start capture** - Begin capturing frames from FiveM
3. **Stop capture** - Stop frame capture
4. **Start projection** - Open borderless projection window
5. **Stop projection** - Close projection window
6. **Configure settings** - Adjust FPS, resolution, etc.
7. **Quick start** - Auto-detect and start everything
8. **Show status** - Display current system status

## Configuration

### Capture Settings

- **Target FPS**: Frame rate for capture (default: 60)
- **Resolution**: Output resolution (default: original game resolution)
- **Hardware Acceleration**: Enable/disable hardware acceleration

### Projection Settings

- **Screen Selection**: Choose which monitor for projection
- **VSync**: Enable/disable vertical sync
- **Projection Mode**: Fullscreen, windowed, or borderless

## System Requirements

- **OS**: Windows 10/11 (64-bit)
- **Runtime**: .NET 8.0 or later
- **Memory**: 4GB RAM minimum
- **Graphics**: DirectX 11 compatible graphics card

## How It Works

1. **Process Detection**: Scans for FiveM processes using multiple process name patterns
2. **Window Capture**: Uses Windows GDI+ for real-time window capture
3. **Frame Processing**: Processes captured frames with optional scaling and filtering
4. **Projection**: Displays frames in a borderless fullscreen window for immersive viewing

## FiveM Compatibility

Pick6 automatically detects various FiveM versions including:
- FiveM (main release)
- FiveM_b2060, FiveM_b2189, FiveM_b2372
- FiveM_b2545, FiveM_b2612, FiveM_b2699
- FiveM_b2802, FiveM_b2944
- CitizenFX

## Troubleshooting

### No FiveM Process Found
- Ensure FiveM is running and fully loaded
- Check that FiveM has a visible window (not minimized)
- Try running Pick6 as administrator

### Poor Performance
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