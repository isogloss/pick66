# Pick6 C++ Implementation

This is a complete C++ rewrite of the Pick6 game capture application with enhanced features:

## New Features

### 1. Fully Functional UI Window
- **Windows**: Native Win32 API GUI with OBS-style interface
- **Linux**: GTK3 support for development (when available)
- **Fallback**: Console mode when no GUI is available

### 2. Borderless Projection Window
- Fullscreen borderless window that mimics original game appearance
- DirectX 11 rendering on Windows for hardware acceleration
- Multiple monitor support
- Automatic scaling and aspect ratio handling

### 3. Stealth Features
- **Hide from Alt+Tab**: Windows don't appear in Alt+Tab switcher
- **Hide from Taskbar**: Applications invisible in taskbar
- **Tool Window Mode**: Reduced visibility in system UI
- **DWM Integration**: Uses Desktop Window Manager for advanced stealth

### 4. Customizable Keybinds
- **Global Hotkeys**: System-wide hotkey support
- **Configurable Actions**: 
  - `Ctrl+L` - Toggle Loader Menu (default)
  - `Ctrl+P` - Toggle Projection Window (default)
- **Persistent Settings**: Keybinds saved to `keybinds.cfg`
- **Settings UI**: Easy keybind customization interface

## Architecture

### Core Components

#### 1. Game Capture Engine (`GameCapture`)
- **Vulkan Injection**: Direct frame capture via DLL injection
- **Window Capture**: Fallback GDI capture method
- **High Performance**: Configurable FPS (15-120)
- **Frame Processing**: Real-time frame scaling and filtering

#### 2. Process Detection (`ProcessDetector`)
- **FiveM Detection**: Automatic detection of FiveM processes
- **Background Monitoring**: Continuous process scanning
- **Vulkan Support Detection**: Identifies Vulkan-capable processes
- **Multi-version Support**: All FiveM variants supported

#### 3. Projection System (`ProjectionWindow`)
- **Borderless Fullscreen**: True borderless experience
- **Hardware Rendering**: DirectX 11 acceleration
- **Stealth Mode**: Invisible to system tools
- **Multi-monitor**: Choose projection display

#### 4. Stealth Manager (`StealthManager`)
- **Alt+Tab Hiding**: `WS_EX_TOOLWINDOW` + DWM exclusion
- **Taskbar Hiding**: Shell integration manipulation
- **Process Hiding**: Reduced system visibility
- **Invisibility Mode**: Advanced stealth features

#### 5. Keybind System (`KeybindManager`)
- **Global Hotkeys**: `RegisterHotKey` Windows API
- **Thread-safe**: Dedicated message loop
- **Configurable**: Runtime keybind changes
- **Persistent**: File-based configuration

### UI Architecture

#### Windows Implementation
- **Win32 API**: Native Windows controls
- **Message Loop**: Standard Windows message handling
- **Common Controls**: Trackbars, checkboxes, buttons
- **GDI+**: Basic graphics rendering

#### Linux Implementation (Optional)
- **GTK3**: Cross-platform GUI toolkit
- **Cairo**: Vector graphics rendering
- **GLib**: Event handling and utilities

#### Fallback Mode
- **Console Interface**: Text-based interaction
- **Status Logging**: Real-time status updates
- **Basic Functionality**: Core features available

## Building

### Prerequisites

#### Windows
- Visual Studio 2019+ or MinGW-w64
- Windows SDK 10.0+
- CMake 3.20+

#### Linux (Development)
- GCC 9+ or Clang 10+
- CMake 3.20+
- GTK3 development packages (optional)

### Build Instructions

```bash
# Clone repository
git clone https://github.com/isogloss/pick66.git
cd pick66/cpp

# Create build directory
mkdir build && cd build

# Configure with CMake
cmake ..

# Build (Windows)
cmake --build . --config Release

# Build (Linux)
make -j4

# Run
./bin/Pick6CPP
```

### Windows-specific Build

```cmd
# Using Visual Studio
cmake .. -G "Visual Studio 16 2019" -A x64
cmake --build . --config Release

# Using MinGW
cmake .. -G "MinGW Makefiles"
cmake --build .
```

## Usage

### GUI Mode (Windows)

1. **Launch Application**: Run `Pick6CPP.exe`
2. **Main Interface**: OBS-style capture interface
3. **Start Injection**: Click button to begin monitoring
4. **Automatic Detection**: System finds FiveM processes
5. **Projection Control**: Show/hide projection window
6. **Settings**: Configure FPS, keybinds, auto-projection

### Console Mode (Linux/Fallback)

1. **Launch**: Run `./Pick6CPP`
2. **Status Updates**: Real-time console output
3. **Keybind Control**: Use global hotkeys
4. **Exit**: Press Enter to quit

### Keybind Customization

1. **Access Settings**: Click "Keybind Settings" button
2. **Select Action**: Choose action to customize
3. **Record Keybind**: Press new key combination
4. **Apply Changes**: Settings saved automatically
5. **Global Hotkeys**: Work system-wide

### Stealth Features

- **Automatic**: Projection window hidden by default
- **Manual Control**: Toggle via settings
- **Process Hiding**: Loader can be hidden from task manager
- **Window Hiding**: Alt+Tab and taskbar exclusion

## Configuration Files

### `keybinds.cfg`
```ini
toggle_loader=76,1,0,0,Ctrl+L - Toggle Loader
toggle_projection=80,1,0,0,Ctrl+P - Toggle Projection
```

Format: `action=virtualkey,ctrl,alt,shift,description`

## Technical Details

### Stealth Implementation

#### Alt+Tab Hiding
```cpp
// Set WS_EX_TOOLWINDOW extended style
LONG_PTR exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
exStyle |= WS_EX_TOOLWINDOW;
SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);

// DWM exclusion
BOOL exclude = TRUE;
DwmSetWindowAttribute(hwnd, DWMWA_EXCLUDED_FROM_PEEK, &exclude, sizeof(exclude));
```

#### Taskbar Hiding
```cpp
// Remove from shell
exStyle |= WS_EX_TOOLWINDOW;
exStyle &= ~WS_EX_APPWINDOW;
SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);
```

### DirectX 11 Rendering
```cpp
// Swap chain creation
DXGI_SWAP_CHAIN_DESC scd = {};
scd.BufferCount = 1;
scd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
scd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
scd.OutputWindow = hwnd;
scd.Windowed = TRUE;
```

### Global Hotkeys
```cpp
// Register system hotkey
UINT modifiers = MOD_CONTROL | MOD_ALT;
RegisterHotKey(hwnd, hotkeyId, modifiers, VK_L);

// Handle WM_HOTKEY messages
case WM_HOTKEY:
    HandleHotkeyAction(wParam);
    break;
```

## Comparison with C# Version

| Feature | C# Version | C++ Version |
|---------|------------|-------------|
| UI Framework | Windows Forms | Win32 API / GTK3 |
| Rendering | GDI+ | DirectX 11 / Cairo |
| Performance | Good | Excellent |
| Memory Usage | Higher | Lower |
| Startup Time | Slower | Faster |
| Platform Support | Windows/.NET | Windows/Linux |
| Stealth Features | Basic | Advanced |
| Keybind System | Limited | Full-featured |
| Build Dependencies | .NET Runtime | None (static) |

## Future Enhancements

### Planned Features
- **Vulkan Rendering**: Hardware-accelerated projection
- **Multi-process Injection**: Multiple game instances
- **Network Streaming**: Remote projection support
- **Advanced Filtering**: Real-time image processing
- **Plugin System**: Extensible architecture

### Platform Expansion
- **macOS Support**: Metal rendering backend
- **WebAssembly**: Browser-based projection
- **Android/iOS**: Mobile companion apps

## Security Notes

- **Legitimate Use**: Designed for legal screen capture
- **Stealth Features**: For user privacy, not malicious hiding
- **Process Injection**: Requires appropriate permissions
- **Antivirus**: May trigger false positives due to injection techniques

## License

MIT License - See LICENSE file for details.

## Contributing

1. **Fork Repository**: Create personal fork
2. **Feature Branch**: Create feature/bugfix branch
3. **Test Changes**: Verify on Windows and Linux
4. **Pull Request**: Submit changes for review
5. **Documentation**: Update relevant documentation

## Support

- **Issues**: GitHub issue tracker
- **Discussions**: GitHub discussions
- **Wiki**: Detailed documentation
- **Examples**: Sample configurations

---

**Note**: This C++ implementation provides significantly enhanced performance and features compared to the original C# version, with advanced stealth capabilities and customizable keybind system for professional game capture workflows.