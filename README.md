# Pick66 - Unified Lottery Generator & Game Capture Application

**Modern .NET 8 application combining lottery number generation with high-performance FiveM game capture capabilities.**

## 🚀 Quick Start

### One-Command Installation

```powershell
# Clone repository and install
git clone https://github.com/isogloss/pick66.git
cd pick66
.\install.ps1 -Launch
```

This creates a self-contained ~21MB executable in your Downloads folder - **no additional dependencies required**.

### Manual Installation Options

```powershell
# Basic installation to Downloads folder
.\install.ps1

# Clean build with launch
.\install.ps1 -Clean -Launch

# Custom installation path
.\install.ps1 -OutputPath "C:\MyApps"
```

## 📋 Features Overview

### 🎯 Lottery Number Generation
- **Fisher-Yates Algorithm**: Cryptographically sound random number generation
- **Flexible Parameters**: Configure numbers per ticket, ranges, uniqueness constraints
- **Batch Generation**: Create multiple tickets with progress tracking
- **Export Options**: Save results to files or copy to clipboard
- **Validation**: Automatic parameter validation with user-friendly error messages

### 🎮 FiveM Game Capture
- **High-Performance Capture**: Vulkan injection + GDI fallback, >120 FPS support
- **Real-time Projection**: Borderless fullscreen projection to secondary monitors
- **Auto-Detection**: Automatic FiveM process discovery and injection
- **Performance Monitoring**: Live FPS tracking, dropped frame detection, performance analysis
- **Hardware Acceleration**: Optimized capture with GPU acceleration support

### 🖥️ Modern Interface
- **Tabbed Layout**: Separate interfaces for lottery generation and game capture
- **Dark/Light Themes**: User-configurable themes with system integration
- **Real-time Logging**: Live log display with color-coded severity levels
- **Settings Persistence**: Configuration stored in `%AppData%/Pick66/settings.json`
- **Non-blocking Notifications**: Error handling without modal dialog spam

### ⚙️ Installation & Distribution
- **Single-File Executable**: Self-contained Windows x64 executable (~21MB)
- **PowerShell Install Script**: Automated build, publish, and installation
- **SHA256 Verification**: Cryptographic integrity checking
- **No Dependencies**: Includes .NET 8 runtime, runs on any Windows 10/11 system
- **Downloads Folder Deployment**: Installs to user-accessible location

## 💻 Usage Examples

### GUI Mode (Default)
```bash
# Launch modern tabbed interface
pick66.exe
```

### Lottery Generation (Console)
```bash
# Generate single ticket (6 numbers from 1-49, unique)
pick66.exe --lottery --numbers 6 --min 1 --max 49 --unique

# Generate 10 tickets with custom parameters
pick66.exe --lottery --count 10 --numbers 5 --min 1 --max 35
```

### Game Capture (Command Line)
```bash
# Auto-start capture and projection at 144 FPS
pick66.exe --fps 144 --auto-start

# Capture only (no projection window)
pick66.exe --fps 60 --no-projection

# Target specific monitor for projection
pick66.exe --fps 120 --monitor 1 --auto-start
```

### System Administration
```bash
# Check for updates and exit
pick66.exe --check-updates-only

# Show help and exit
pick66.exe --help

# Verbose logging for troubleshooting
pick66.exe --log-level Debug
```

## 🏗️ Architecture & Technical Details

### Project Structure
```
Pick66/
├── src/
│   ├── Pick6.Core/           # Game capture engine
│   ├── Pick6.Projection/     # Windows projection system  
│   ├── Pick6.Loader/         # Main application entry point
│   ├── Pick6.ModGui/         # ImGui-style interface
│   ├── Pick66.Core/          # Lottery number generation
│   ├── Pick66.Console/       # Console demonstration
│   └── Pick66.Tests/         # Unit tests (16 tests)
├── install.ps1               # PowerShell installation script
├── README.md                 # This documentation
└── UPGRADE.md               # Migration guide
```

### Core Technologies
- **Platform**: .NET 8 with Windows-specific optimizations
- **UI Framework**: Windows Forms with ImGui-style theming
- **Graphics**: Hardware-accelerated capture via Vulkan/GDI
- **Architecture**: Event-driven with async/await patterns
- **Packaging**: Single-file self-contained deployment
- **Testing**: XUnit with 16 comprehensive unit tests

### Build Configuration
- **Target Framework**: .NET 8 (LTS)
- **Runtime**: Windows x64 self-contained  
- **Compilation**: ReadyToRun with partial trimming
- **Size Optimization**: ~21MB single-file executable
- **Security**: SHA256 integrity verification

## 🔧 Advanced Configuration

### Settings Management
All settings are stored in `%AppData%/Pick66/settings.json`:

```json
{
  "isDarkTheme": true,
  "autoStartProjection": false,
  "targetFps": 60,
  "resolutionWidth": 0,
  "resolutionHeight": 0,
  "monitorIndex": 0,
  "hardwareAcceleration": true,
  "outputDirectory": "output",
  "hotkeyToggleProjection": "Ctrl+P",
  "hotkeyStopAndRestore": "Ctrl+Shift+P",
  "lotteryNumbersPerTicket": 6,
  "lotteryMinNumber": 1,
  "lotteryMaxNumber": 49,
  "lotteryUniqueNumbers": true
}
```

### Global Hotkeys (Windows)
- **Ctrl+L**: Toggle application window visibility
- **Ctrl+P**: Toggle projection window (configurable)
- **Ctrl+Shift+P**: Stop projection and restore (configurable)
- **F12**: Emergency close projection and show application

### Performance Optimization
```bash
# High-FPS gaming setup
pick66.exe --fps 165 --monitor 2 --auto-start

# Streaming optimized (capture only)
pick66.exe --fps 120 --no-projection --log-level Warning

# Development/debugging
set PICK6_DIAG=1
pick66.exe --fps 60 --log-level Debug
```

## 🔧 Development & Building

### Prerequisites
- **.NET 8 SDK** (or later)
- **Windows 10/11** for testing GUI features
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
- **Unit Tests**: 16 comprehensive tests covering core lottery logic
- **Manual Testing**: GUI interface testing on Windows systems
- **Performance Testing**: FPS and memory usage validation

## 🐛 Troubleshooting

### Installation Issues

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