# Pick6 - OBS Game Capture Clone

Real-time game capture and projection application for FiveM with borderless fullscreen projection.

## Core Functionalities

- **FiveM Game Capture**: Auto-detection and capture of FiveM processes via Vulkan injection or window capture
- **Real-time Projection**: High-performance borderless fullscreen projection at 60+ FPS
- **Monitor Selection**: Choose target display for projection output
- **Global Hotkeys**: System-wide keyboard shortcuts for projection control (Windows)
- **Unified Interface**: Single executable with GUI mode (Windows) and console mode (cross-platform)

## What's New in This Commit

- **Fixed Real-time Projection**: Resolved issue where projection showed only one frame - now continuously updates at target FPS
- **Keybinds Customization Menu**: Added console UI for configuring global hotkeys with conflict detection
- **Single-file Packaging**: Added publish profile and instructions for self-contained executable distribution

## Single-File Distribution Build

To create a single, self-contained Windows executable (.exe) with no dependencies:

**Method 1: Direct command**
```bash
dotnet publish src/Pick6.Loader -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true
```

**Method 2: Using publish profile**
```bash
dotnet publish src/Pick6.Loader -p:PublishProfile=WindowsSingleFile
```

The executable will be created at: `src/Pick6.Loader/bin/Release/net8.0-windows/win-x64/publish/pick6_loader.exe`

This single file can be distributed to users without requiring .NET installation or any manual dependencies.

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