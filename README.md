# Pick6 - High-Performance OBS Game Capture Clone

Real-time game capture and projection application for FiveM with automatic, non-interactive operation and >120 FPS support.

## Core Functionalities

- **High-Performance FiveM Capture**: Auto-detection and capture of FiveM processes via Vulkan injection or window capture at >120 FPS
- **Real-time Projection**: High-performance borderless fullscreen projection supporting 144, 165, 240+ FPS targets
- **Automatic Operation**: Non-interactive auto-start mode - detects FiveM, starts capture, starts projection automatically
- **Monitor Selection**: Choose target display for projection output via command line
- **Update System**: Automatic update checking at startup with graceful offline fallback
- **Advanced CLI**: Complete command-line interface with logging levels, FPS targets, and projection control
- **Global Hotkeys**: System-wide keyboard shortcuts for projection control (Windows)

## Quick Start (New Auto Mode)

**Basic usage** - automatically start capture and projection:
```bash
pick6.exe --fps 144
```

**Capture only** (no projection window):
```bash
pick6.exe --fps 60 --no-projection
```

**High FPS with debug logging**:
```bash
pick6.exe --fps 240 --log-level Debug
```

**Check for updates and exit**:
```bash
pick6.exe --check-updates-only
```

## Command Line Options

### Basic Options
- `--fps <number>` - Set target FPS (1-600, default: 60) - **120 FPS ceiling removed**
- `--resolution <w> <h>` - Set output resolution (e.g., 1920 1080)  
- `--monitor <index>` - Target monitor index (0-based)
- `--no-projection` - Capture only, disable projection window

### Logging & Debug
- `--log-level <level>` - Set log level: Debug, Info, Warning, Error
  - Debug mode shows average FPS statistics every 5 seconds

### Update System  
- `--check-updates` - Check for updates at startup
- `--check-updates-only` - Check for updates and exit

### Mode Control
- `--gui` - Force GUI mode (Windows only)
- `--interactive` - Use legacy interactive menu mode
- `--help, -h` - Show help message

## Default Behavior

- **With no arguments**: Opens GUI mode on Windows
- **With command-line arguments**: Auto-starts capture/projection in console mode (non-interactive)

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
- The loader uses partial trimming mode to preserve dynamic loading capabilities