# Pick6 Architecture Documentation

## Overview

Pick6 is a game capture and projection tool for FiveM that uses DLL injection to capture frames directly from the game's rendering pipeline. This document describes the architecture and implementation details.

## Project Structure

The solution consists of four main projects:

### 1. Pick6.Core

Core library containing the game capture engine and injection mechanisms.

**Key Components:**
- `FiveMDetector.cs` - Identifies and monitors FiveM processes
- `VulkanInjector.cs` - Handles DLL injection into target processes
- `EnhancedInjector.cs` - Multi-strategy injector with fallback mechanisms
- `GameCaptureEngine.cs` - Main capture engine that coordinates frame capture
- `VulkanFrameCapture.cs` - Manages frame capture via injected DLL
- `ProcessWatcher.cs` - Monitors for FiveM process changes
- `SharedMemoryBuffer` - IPC mechanism for receiving frames from injected DLL

### 2. Pick6.VulkanHook

Injectable DLL that hooks into the game's rendering pipeline to capture frames.

**Key Components:**
- `VulkanHookEntryPoint.cs` - Entry point and lifecycle management
- `HookManager.cs` - Installs and manages DirectX/Vulkan hooks
- `FrameWriter.cs` - Writes captured frames to shared memory for IPC

**How it works:**
1. Injected into the FiveM process by VulkanInjector
2. Automatically initializes when loaded via static constructor
3. Hooks DirectX/Vulkan Present function to capture each frame
4. Sends frame data back to main application via shared memory

### 3. Pick6.Projection

Window projection system for displaying captured frames.

**Key Components:**
- Borderless window rendering
- Multi-monitor support
- Frame display and scaling

### 4. Pick6.Loader

Main application executable with GUI interface.

**Key Components:**
- User interface (WinForms)
- Console menu system
- Settings management
- Auto-update functionality

## Game Capture Flow

```
1. User starts Pick6.Loader
2. FiveMDetector identifies running FiveM process
3. VulkanInjector/EnhancedInjector injects Pick6VulkanHook.dll into FiveM
4. Pick6VulkanHook initializes and hooks Present function
5. Each frame:
   a. FiveM calls Present to render frame
   b. Hook intercepts call and captures frame data
   c. FrameWriter writes to shared memory
   d. VulkanFrameCapture reads from shared memory
   e. GameCaptureEngine forwards to projection window
   f. Frame displayed on selected monitor
```

## DLL Injection Strategies

The EnhancedInjector supports multiple injection strategies with automatic fallback:

1. **Direct Injection** - LoadLibrary via CreateRemoteThread
2. **DXGI Proxy** - Proxy DLL injection via dxgi.dll
3. **D3D11 Proxy** - Proxy DLL injection via d3d11.dll  
4. **Vulkan Proxy** - Proxy DLL injection via vulkan-1.dll

Each strategy is attempted in order until one succeeds.

## Inter-Process Communication

The system uses Windows memory-mapped files (shared memory) for high-performance IPC:

### Shared Memory Layout

```
+------------------+
| FrameHeader      |  Header with metadata
|  - Magic (4B)    |  0x50494B36 ("PIK6")
|  - Width (4B)    |  Frame width in pixels
|  - Height (4B)   |  Frame height in pixels
|  - Format (4B)   |  Pixel format (RGBA, etc.)
|  - DataSize (4B) |  Size of frame data
|  - Timestamp (8B)|  Capture timestamp
+------------------+
| Frame Data       |  Raw pixel data
|  (variable)      |  Up to BUFFER_SIZE
+------------------+
```

### Shared Memory Name
`Pick6_Frames_{ProcessId}`

## Building the Solution

### Prerequisites
- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 or later (recommended)

### Build Commands

**Build all projects:**
```bash
dotnet build src/Pick6.Core/Pick6.Core.csproj -c Release
dotnet build src/Pick6.VulkanHook/Pick6.VulkanHook.csproj -c Release
dotnet build src/Pick6.Projection/Pick6.Projection.csproj -c Release
dotnet build src/Pick6.Loader/Pick6.Loader.csproj -c Release
```

**Publish single-file executable:**
```bash
dotnet publish src/Pick6.Loader/Pick6.Loader.csproj --configuration Release --runtime win-x64 --self-contained true
```

### Output Files

After building, ensure these files are in the same directory:
- `loader.exe` (or `Pick6.exe`) - Main executable
- `Pick6VulkanHook.dll` - Hook DLL for injection
- Any proxy DLLs (dxgi.dll, d3d11.dll, vulkan-1.dll) if using proxy injection

## Security Considerations

### Administrator Privileges
DLL injection requires administrator privileges. The application will automatically request elevation if needed.

### Antivirus Software
Some antivirus software may flag DLL injection as suspicious. This is expected behavior - the tool uses legitimate Windows APIs for game capture.

## Future Enhancements

### Native C++ Hook DLL
The current `Pick6.VulkanHook` is a C# implementation for demonstration. For production use, consider:
- Native C++ DLL using MinHook or Detours
- Direct DirectX/Vulkan API hooking
- Better performance and compatibility
- Smaller DLL footprint

### Additional Features
- Multiple simultaneous game capture
- Recording to video files
- Performance overlays
- Custom frame filters/effects

## Troubleshooting

### DLL Not Found
Ensure `Pick6VulkanHook.dll` is in the same directory as the executable.

### Injection Failed
- Run as Administrator
- Temporarily disable antivirus
- Check Windows Event Viewer for errors
- Verify FiveM is running with Vulkan enabled

### No Frames Captured
- Verify shared memory is being created
- Check process has graphics modules loaded
- Enable diagnostic logging with `PICK6_DIAG=1` environment variable

## Contributing

When contributing to the game capture system:
1. Maintain backwards compatibility with shared memory format
2. Add logging for debugging
3. Test with multiple FiveM versions
4. Document any new injection strategies
5. Update this architecture document

## License

See LICENSE file in repository root.
