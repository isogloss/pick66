# Pick66 - FiveM Game Capture (OBS-Style Interface)

**Modern .NET 8 application providing OBS-style game capture functionality specifically designed for FiveM with DXGI/D3D11 hooks.**

## 🚀 Quick Install

### Prerequisites
- **.NET 8 SDK** (download from https://dot.net)
- **Windows 10/11** (target platform)
- **FiveM** (the game this application is designed to capture)

### Fast Installation

**Option 1: Windows Wrapper (Recommended)**
```cmd
# Clone repository
git clone https://github.com/isogloss/pick66.git
cd pick66

# Double-click install.cmd OR run from command prompt:
install.cmd -Launch
```

**Option 2: PowerShell Direct**
```powershell
# Clone repository and install
git clone https://github.com/isogloss/pick66.git
cd pick66

# From PowerShell 7+ console:
pwsh -ExecutionPolicy Bypass -File ./install.ps1 -Launch
```

This creates a self-contained executable in your Downloads folder - **no additional dependencies required**.

## 📋 Features Overview

### 🎮 FiveM Game Capture
- **OBS-Style Interface**: Simple, familiar interface similar to OBS Game Capture
- **DXGI/D3D11 Hooks**: Direct capture through DXGI.dll and D3D11.dll injection for optimal performance
- **FiveM-Only Focus**: Specifically designed and optimized for FiveM processes
- **Real-time Capture**: Low-latency frame capture with configurable FPS
- **Automatic Detection**: Auto-detection of FiveM processes with enhanced monitoring

### 🖥️ Modern Interface
- **Clean Windows Forms UI**: Professional interface with OBS-style controls
- **Real-time Status**: Live FiveM process detection and capture status
- **Performance Monitoring**: Live FPS counter and capture statistics
- **Activity Logging**: Real-time activity display with timestamped messages
- **Simple Controls**: Start/Stop capture with automatic projection options

### ⚡ High-Performance Capture
- **DXGI Backend**: Primary capture method using DXGI.dll hooks for best performance
- **D3D11 Integration**: Direct D3D11.dll injection for frame access
- **GDI Fallback**: Automatic fallback to GDI capture if DXGI is unavailable
- **Shared Memory**: High-performance frame data transfer between processes
- **Frame Pacing**: Precision frame timing with hybrid spin-wait technology

### 📦 Installation & Distribution
- **Single-File Executable**: Self-contained Windows x64 executable (~21MB)
- **No Dependencies**: Includes .NET 8 runtime, runs on any Windows 10/11 system
- **PowerShell Install Script**: Automated build, publish, and installation
- **Downloads Folder Deployment**: Installs to user-accessible location

### 🔧 Installation Troubleshooting

If you encounter PowerShell parsing errors:
1. **Use the wrapper**: `install.cmd -Launch` (recommended)
2. **Try PowerShell 7+**: `pwsh -ExecutionPolicy Bypass -File install.ps1 -Launch`
3. **Check syntax**: Run `.\validate-powershell.ps1` to verify script integrity
4. **Report issues**: File a bug report with the exact error message

## 💻 Usage

### FiveM Game Capture
```bash
# Launch FiveM capture interface
pick66.exe
```

The application provides an OBS-style interface with:
- **Start Game Capture**: Begin capturing FiveM with DXGI/D3D11 hooks
- **Stop Capture**: Clean shutdown of capture and projection
- **Real-time Status**: Live FiveM process detection and capture monitoring
- **Activity Logging**: Timestamped log of all capture operations
- **Performance Display**: Live FPS counter and capture statistics

### Workflow
1. **Start FiveM**: Launch FiveM first (application will detect it automatically)
2. **Open Pick66**: Launch the capture application
3. **Start Capture**: Click "Start Game Capture" button
4. **Automatic Detection**: Application finds and hooks into FiveM process
5. **Live Monitoring**: View real-time status and performance metrics

## 🏗️ Architecture & Technical Details

### Project Structure
```
Pick66/
├── src/
│   ├── Pick6.Core/           # DXGI/D3D11 capture engine
│   ├── Pick6.Projection/     # Windows projection system  
│   ├── Pick6.Loader/         # Main application entry point
│   └── Pick6.ModGui/         # OBS-style FiveM capture interface
├── install.ps1               # PowerShell installation script
├── README.md                 # This documentation
└── UPGRADE.md               # Migration guide
```

### Core Technologies
- **Platform**: .NET 8 with Windows-specific optimizations
- **UI Framework**: Windows Forms with OBS-style theming
- **Capture Backend**: DXGI.dll and D3D11.dll injection hooks
- **Graphics Pipeline**: Direct FiveM graphics API interception
- **Architecture**: Modular backend system with fallback support
- **Packaging**: Single-file self-contained deployment

### Capture System
- **Primary**: DXGI/D3D11 injection hooks for FiveM processes
- **Fallback**: GDI window capture when injection is unavailable
- **Target**: FiveM processes only (CitizenFX, GTAProcess variants)
- **Performance**: Shared memory communication for high-speed frame transfer
- **Frame Pacing**: Hybrid spin-wait with configurable FPS (15-120 FPS)

### Build Configuration
- **Target Framework**: .NET 8 (LTS)
- **Runtime**: Windows x64 self-contained  
- **Compilation**: ReadyToRun with partial trimming
- **Size Optimization**: ~21MB single-file executable
- **Security**: SHA256 integrity verification

## 🔧 Advanced Configuration

### Settings Management
Application configuration is managed through the user interface. Settings include:
- Projection status preferences
- Display and monitoring options  
- User interface themes and preferences

### Application Features
- Modern colored theme with blue accents
- Professional borderless window design
- Real-time activity logging and monitoring
- Start/stop projection controls
- Status indicators with visual feedback

## 🔧 Development & Building

### Prerequisites
- **.NET 8 SDK** (or later)
- **Windows 10/11** for WPF application
- **PowerShell 5.1+** for installation script

### Development Build
```bash
# Clone repository
git clone https://github.com/isogloss/pick66.git
cd pick66

# Restore packages and build
dotnet restore
dotnet build

# Run tests
dotnet test
```

### Release Build
```powershell
# Automated release build and installation
.\install.ps1 -Clean

# Manual release build
dotnet build --configuration Release
dotnet publish src/Pick6.Loader/Pick6.Loader.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./publish \
  /p:PublishSingleFile=true \
  /p:IncludeAllContentForSelfExtract=true
```

### Testing
- **Manual Testing**: WPF interface testing on Windows systems
- **Functional Testing**: Projection control validation and status monitoring
- **UI Testing**: Colored theme and visual indicator testing

## 🔄 PowerShell Compatibility

### Supported PowerShell Versions
- **Windows PowerShell 5.1**: ✅ Fully supported with compatibility mode
- **PowerShell 7.0+**: ✅ Recommended for optimal performance and features

### Version-Specific Behavior
The installation script automatically detects your PowerShell version:

- **PowerShell 5.1**: Runs in compatibility mode with a friendly warning recommending PowerShell 7+
- **PowerShell 7+**: Runs with all features enabled and optimal performance
- **Below 5.1**: Shows error message and installation guidance

### Installation Examples by Version

**Windows PowerShell 5.1**:
```cmd
# Using the Windows wrapper (recommended)
install.cmd -Launch

# Or directly with PowerShell 5.1
powershell.exe -ExecutionPolicy Bypass -File install.ps1 -Launch
```

**PowerShell 7+**:
```powershell
# Direct execution (optimal)
pwsh -ExecutionPolicy Bypass -File install.ps1 -Launch

# Or from PowerShell 7+ prompt
./install.ps1 -Launch
```

### Migration Recommendation
While Windows PowerShell 5.1 is fully supported, we recommend upgrading to PowerShell 7+ for:
- Better performance during build operations
- Enhanced error handling and diagnostics  
- Future feature compatibility
- Improved security and cross-platform support

Download PowerShell 7+ from: https://github.com/PowerShell/PowerShell

## 🐛 Troubleshooting

### Installation Issues

**PowerShell Version Issues**:
```cmd
# Error: "#requires" statement for Windows PowerShell 7.0
# Solution: Script now works with PowerShell 5.1+, just run normally:
powershell.exe -ExecutionPolicy Bypass -File install.ps1

# For PowerShell 7+ (optimal):
pwsh -ExecutionPolicy Bypass -File install.ps1
```

**PowerShell Execution Policy**:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**.NET SDK Missing**:
- Download from https://dot.net (version 8.0 or later required)
- Verify: `dotnet --version`

**Build Failures**:
```powershell
# Clean and retry
.\install.ps1 -Clean

# Manual verification
dotnet build --configuration Release
```

### Runtime Issues

**Antivirus False Positives**:
- Single-file executables may trigger antivirus warnings
- Add exception for Downloads folder or the specific executable
- Windows Defender SmartScreen may require "Run anyway"

**Performance Issues**:
- Run as Administrator for better injection support
- Close other applications to free system resources
- Lower target FPS if sustained performance issues occur
- Check logs in GUI for specific error messages

**Settings Not Persisting**:
- Verify write permissions to `%AppData%/Pick66`
- Run once as Administrator if needed
- Delete settings file to reset to defaults

### Getting Help

1. **Check Application Logs**: Available in GUI log panel
2. **Review Documentation**: [UPGRADE.md](UPGRADE.md) for migration issues
3. **GitHub Issues**: Include error details and system information
4. **Manual Build**: Try `dotnet build` to isolate build issues

## What's New - Version 2.0

### 🚀 **Non-Interactive Auto Mode** 
- **Default operation**: Automatically detects FiveM → starts capture → starts projection
- **No more menu prompts**: Streamlined for production deployment and automation
- **Legacy support**: `--interactive` flag preserves old menu-based operation

### ⚡ **>120 FPS Support**
- **Removed FPS ceiling**: Now supports 144, 165, 240+ FPS targets  
- **Precision timing**: Replaced Thread.Sleep with Stopwatch-based high-precision frame timing
- **CPU protection**: Upper limit of 600 FPS prevents runaway CPU usage
- **Reduced allocations**: Optimized capture loop for sustained high frame rates

### 📊 **Advanced Logging & Monitoring**
- **Debug mode**: `--log-level Debug` shows average FPS statistics every 5 seconds
- **Performance tracking**: Monitor actual vs target frame rates
- **Structured logging**: Info/Warning/Error levels for production deployment

### 🔄 **Enhanced Update System**
- **Non-blocking startup**: Update check with 30-second timeout, graceful offline fallback
- **Command-line updates**: `--check-updates-only` for CI/CD integration
- **Auto-update ready**: Existing dynamic payload system enhanced for reliability

### 🏗️ **Extensible Architecture**
- **ICaptureBackend interface**: Prepared for future DXGI backend (high-performance DirectX capture)
- **Modular design**: Clean separation between capture backends (GDI, future DXGI)
- **Future-ready**: Compile-time guards for advanced capture technologies

## Building & Distribution

### Single-File High-Performance Build

Create a self-contained Windows executable optimized for high FPS operation:

```bash
dotnet publish src/Pick6.Loader -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true
```

**Output**: `src/Pick6.Loader/bin/Release/net8.0-windows/win-x64/publish/pick6_loader.exe`

This single ~16MB executable includes:
- ✅ Zero .NET runtime dependencies  
- ✅ High-performance >120 FPS capture engine
- ✅ Automatic update system
- ✅ Complete command-line interface

### Production Deployment Examples

**Gaming Setup** - Auto-start on FiveM launch:
```bash
pick6.exe --fps 144 --monitor 1 --log-level Info
```

**Streaming Setup** - High FPS capture without projection:  
```bash
pick6.exe --fps 165 --no-projection --log-level Warning
```

**CI/CD Integration** - Update checks in automation:
```bash
pick6.exe --check-updates-only
if %ERRORLEVEL% EQU 0 (
    pick6.exe --fps 60 --log-level Error
)
```

**Debug/Development** - Full logging and FPS monitoring:
```bash  
pick6.exe --fps 240 --log-level Debug --resolution 1920 1080
```

## Auto-Update System

Pick6 includes a built-in auto-update system that can dynamically download and load payload assemblies from a remote server, enabling updates without redistributing the entire loader executable.

### Quick Start

1. **Build single-file loader**: Use the commands above to create the self-contained executable
2. **Enable auto-updates**: Set `ENABLE_DYNAMIC_PAYLOAD = true` in `src/Pick6.Loader/Program.cs`
3. **Configure manifest URL**: Update `MANIFEST_URL` to point to your JSON manifest file
4. **Build payload**: Use `.\Tools\Build-Payload.ps1 -Version "1.0.0" -OutputPath "./dist"`
5. **Deploy**: Upload the payload ZIP and manifest to your web server

### Key Features

- **Zero-dependency distribution**: Single 16MB executable with no external requirements
- **Secure updates**: SHA256 integrity verification for all downloads
- **Graceful fallback**: Network failures don't break existing functionality  
- **Version management**: Automatic version tracking and incremental updates
- **Developer tools**: PowerShell build script and GitHub Actions automation

### File Structure

```
%APPDATA%/Pick6/
├── payload_version.txt      # Current payload version
└── payload/                 # Cached payload assemblies
    ├── Pick6.Core.dll
    ├── Pick6.Projection.dll
    └── payload-manifest.json
```

### Documentation

- **[docs/auto-update.md](docs/auto-update.md)**: Complete setup and deployment guide
- **[docs/auto-update-manifest.sample.json](docs/auto-update-manifest.sample.json)**: Example manifest format
- **[Tools/Build-Payload.ps1](Tools/Build-Payload.ps1)**: Automated payload build script
- **[.github/workflows/build-loader.yml](.github/workflows/build-loader.yml)**: CI/CD automation

### Security Notes

- Always use HTTPS for manifest and payload URLs
- Verify SHA256 hashes match exactly in your deployment process  
- Test payloads thoroughly before publishing

## Performance Monitoring & Diagnostics

### Enhanced Console Menu

The console interface now provides comprehensive performance monitoring and configuration:

```
pick6.exe --interactive  # Force interactive menu mode
```

**Menu Sections:**
- **A. Capture Settings**: FPS presets (30/60/120/144), resolution templates, hardware acceleration
- **B. Projection**: Start/stop, FPS matching, monitor selection  
- **C. Performance & Diagnostics**: Live statistics, performance warnings, diagnostic exports
- **D. Output/Quality**: Future encoding and recording features (placeholder)
- **E. Injection & Process**: FiveM detection, reinjection controls, method information
- **F. System**: Keybinds, help, and utility functions

### Performance Features

**Real-time Monitoring:**
```bash
# Option 13 in console menu - Live statistics display
Capture:    FPS: 59.8 (avg: 59.2) | P95: 16.9ms | Dropped: 2/3580 (0.1%)
Projection: Active (stats not available via current interface)
Uptime:     00:02:45
Memory:     89.2 MB
```

**Performance Analysis (Option 14):**
- Automatic detection of frame rate issues
- Memory usage warnings
- Targeted recommendations for optimization

**Environment Variables:**
```bash
# Enable detailed frame timing diagnostics
export PICK6_DIAG=1   # Linux/Mac
set PICK6_DIAG=1      # Windows CMD
$env:PICK6_DIAG=1     # Windows PowerShell
```

**Diagnostic Export:**
- Full system analysis saved to timestamped files
- Capture engine statistics and configuration
- FiveM process detection details
- System information and environment variables

### Frame Pacing Technology

**FramePacer Modes:**
- **HybridSpin**: Coarse sleep + precision spin-wait (default)
- **SleepPrecision**: Thread.Sleep only (lower CPU usage)
- **Unlimited**: No frame limiting (maximum throughput)
- **VSync**: Display synchronization (future feature)

**Frame Statistics:**
- Ring buffer tracking of last 240 frames (4 seconds at 60 FPS)
- Instant FPS, moving average FPS, 95th percentile frame times
- Dropped frame detection (frames >1.5x target interval)
- Real-time performance warnings when sustained performance drops below 70% of target

### Troubleshooting Performance Issues

**Common Performance Patterns:**
1. **Half-rate issues**: Check for frame duplication in projection path
2. **Dropped frames**: Monitor P95 frame times, consider lowering FPS/resolution
3. **Memory leaks**: Use diagnostic export to track memory usage over time
4. **CPU overload**: Switch to SleepPrecision pacing mode for lower CPU usage

**Optimization Tips:**
- Run as administrator for better injection support
- Use Vulkan injection when available (better than window capture)  
- Enable hardware acceleration for improved capture performance
- Close other applications to free up system resources
- Lower target FPS or resolution if sustained performance issues occur
- The loader uses partial trimming mode to preserve dynamic loading capabilities

## GUI Menu & Settings

The Pick6 application now includes a persistent GUI menu that provides easy access to Start/Stop projection controls and user settings management.

### Usage Instructions

When you launch Pick6 (via `pick6_loader.exe`), you'll see the main GUI window with:

- **Start Injection** button: Begins the projection/injection workflow
- **Stop** button: Cleanly stops the projection and returns to idle state  
- **Settings** button: Opens the settings dialog for configuration
- **Hide** button: Minimizes the window (use global hotkeys to restore)
- **Status display**: Shows current state (Idle/Starting/Running/Stopping/Error)
- **Log output**: Real-time display of the last 200 log entries

### Settings Management

The settings dialog allows you to configure:

- **Auto-start projection**: Automatically start projection when the application launches
- **Verbose logging**: Enable detailed logging output
- **Refresh interval**: Projection refresh rate in milliseconds (50-10000ms)
- **Toggle hotkey**: Global hotkey to toggle projection (default: Ctrl+P)  
- **Stop & restore hotkey**: Global hotkey to stop projection and restore menu (default: Ctrl+Shift+P)
- **Output directory**: Directory for captures and logs

### Settings Storage

User settings are automatically persisted to: `%AppData%\Pick6\settings.json`

Settings are validated when loaded/saved:
- Refresh interval is clamped to 50-10000ms range
- Invalid values trigger warnings in the logs but preserve previous valid settings
- Missing or corrupted settings file automatically uses defaults

### Global Hotkeys

The following global hotkeys work system-wide (even when the GUI is minimized):

- **Ctrl+L**: Toggle loader window visibility
- **Ctrl+P**: Toggle projection window (or custom hotkey from settings)
- **Ctrl+Shift+P**: Stop projection & restore menu (or custom hotkey from settings)
- **Ctrl+Shift+Esc**: Close projection immediately
- **F12**: Close projection + toggle loader

### Auto-Start Functionality

When "Auto-start projection" is enabled in settings:
- The application will automatically begin projection monitoring after startup
- No manual intervention needed - just launch the app and it starts working
- Ideal for automated deployment scenarios