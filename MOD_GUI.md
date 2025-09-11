# Pick6 Mod Menu Documentation

## Overview

Pick6 now features a modern ImGui-style mod menu interface that replaces the traditional WinForms GUI. This new interface provides a clean, game-mod inspired design with enhanced functionality and better single-file deployment compatibility.

## Interface Design

The mod menu features a dark theme with two main tabs:

### Loader Tab
- **Status Display**: Shows current capture/projection status with color coding
- **Performance Metrics**: Real-time FPS and dropped frame counters  
- **Control Buttons**: 
  - Start/Stop Capture (Green/Red)
  - Start/Stop Projection (Blue/Dark Blue)
- **Console Log**: Scrollable log output with recent entries

### Settings Tab
- **Target FPS**: Slider (1-120 FPS)
- **Resolution**: Width/Height inputs (0=auto)
- **Hardware Acceleration**: Checkbox toggle
- **Auto-start Projection**: Checkbox toggle
- **UI Scale**: Slider for interface scaling (0.5-3.0x)
- **Monitor Index**: Numeric input for multi-monitor setups
- **Apply/Save Buttons**: Apply changes or persist to disk

## Key Features

### Modern Interface
- Dark theme optimized for gaming environments
- Tab-based navigation (Loader / Settings)
- Color-coded status indicators
- Real-time performance monitoring

### Settings Persistence
- Settings stored in `%AppData%\Pick6\imgui_settings.json`
- Automatic validation and range clamping
- UI scale preserved across restarts

### Enhanced Logging
- Thread-safe log display with auto-scroll
- Color-coded log levels (Error=Red, Warn=Orange, Info=Gray, Debug=Light Gray)
- Ring buffer maintains last 1000 entries

### Single-File Deployment
- Compatible with .NET 8 single-file publishing
- No external ImGui native dependencies required
- Uses Windows Forms host for maximum compatibility

## Command Line Interface

CLI behavior remains unchanged:
```bash
pick6.exe                    # Opens mod menu GUI
pick6.exe --help             # Shows help (no GUI)
pick6.exe --check-updates-only  # Updates only (no GUI)
```

## Architecture

### Core Components
- **Pick6.ModGui**: New mod menu interface project
- **GuiState**: Singleton state management
- **ImGuiLogSink**: Logging adapter for GUI integration
- **ImGuiSettings**: Simplified settings model

### Integration
- **Pick6.Loader** updated to launch mod menu instead of old WinForms GUI
- Existing **Pick6.Core** and **Pick6.Projection** functionality preserved
- All capture/projection APIs remain unchanged

## Migration from WinForms GUI

### Deprecated Components
- **Pick6.GUI.MainForm**: Replaced by ModMenuApplication
- Old WinForms settings dialog: Integrated into Settings tab
- Context menu approach: Replaced with dedicated UI controls

### Preserved Functionality
- All capture engine features (Vulkan injection, GDI fallback)
- Projection window management
- FiveM process detection and monitoring
- Settings persistence (migrated to new format)

## Building and Deployment

### Development Build
```bash
dotnet build src/Pick6.Loader/Pick6.Loader.csproj
```

### Single-File Release
```bash
dotnet publish src/Pick6.Loader/Pick6.Loader.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./publish
```

This produces a single `pick6_loader.exe` file (~21MB) with all dependencies.

## Technical Implementation

### Windows Forms Host
The mod menu uses a Windows Forms TabControl host instead of native ImGui rendering. This approach:
- Ensures single-file deployment compatibility
- Reduces native dependency complexity
- Provides familiar Windows styling
- Maintains accessibility support

### Performance
- 60 FPS UI update timer for smooth performance metrics
- Thread-safe event handling for log updates
- Minimal CPU overhead when idle

## Future Enhancements

Planned improvements for future releases:
- Global hotkey support for window visibility toggle
- Advanced performance graphs and diagnostics
- Window position/size persistence
- Enhanced multi-monitor support
- Customizable color themes