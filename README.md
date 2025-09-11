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

The executable will be created at: `src/Pick6.Loader/bin/Release/net8.0/win-x64/publish/pick6_loader.exe`

This single file can be distributed to users without requiring .NET installation or any manual dependencies.