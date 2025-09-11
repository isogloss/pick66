# Pick66 - Projection Interface Application

**Modern .NET 8 WPF application providing professional projection interface capabilities with colored theme.**

## 🚀 Quick Start

### One-Command Installation

```powershell
# Clone repository and install
git clone https://github.com/isogloss/pick66.git
cd pick66
.\install.ps1 -Launch
```

This creates a self-contained executable in your Downloads folder - **no additional dependencies required**.

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

### 🎯 Projection Interface
- **Modern WPF Interface**: Professional colored theme with blue accents
- **Start/Stop Controls**: Simple projection control with status monitoring
- **Activity Logging**: Real-time log display with timestamps
- **Status Indicators**: Visual status indicators with colored states
- **System Information**: Display of projection status and system details

### 🖥️ Modern Interface
- **Borderless Design**: Clean, modern window styling
- **Colored Theme**: Professional dark theme with blue accent colors
- **Real-time Logging**: Live activity display with color-coded messages
- **Settings Persistence**: Configuration stored in `%AppData%/Pick66/settings.json`
- **Non-blocking Notifications**: Error handling without modal dialog spam

### ⚙️ Installation & Distribution
- **Single-File Executable**: Self-contained Windows x64 executable (~21MB)
- **PowerShell Install Script**: Automated build, publish, and installation
- **SHA256 Verification**: Cryptographic integrity checking
- **No Dependencies**: Includes .NET 8 runtime, runs on any Windows 10/11 system
- **Downloads Folder Deployment**: Installs to user-accessible location

## 💻 Usage

### WPF Application
```bash
# Launch projection interface
pick66.exe
```

The application provides a modern WPF interface with:
- Start/Stop projection controls
- Real-time activity logging
- Status monitoring with visual indicators
- Professional colored theme

## 🏗️ Architecture & Technical Details

### Project Structure
```
Pick66/
├── src/
│   ├── Pick6.Core/           # Game capture engine
│   ├── Pick6.Projection/     # Windows projection system  
│   ├── Pick6.Loader/         # Main application entry point
│   ├── Pick6.ModGui/         # ImGui-style interface
│   └── Pick66.App/           # Projection interface WPF application
├── install.ps1               # PowerShell installation script
├── README.md                 # This documentation
└── UPGRADE.md               # Migration guide
```

### Core Technologies
- **Platform**: .NET 8 with Windows-specific optimizations
- **UI Framework**: WPF with modern colored theming
- **Graphics**: Projection interface with visual status indicators
- **Architecture**: MVVM pattern with async/await operations
- **Packaging**: Single-file self-contained deployment
- **Testing**: Professional colored theme with high contrast

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