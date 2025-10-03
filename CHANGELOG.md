# Changelog

## [Unreleased]

### Added - FiveM Game Capture Implementation

This release implements the core game capture infrastructure for FiveM using DLL injection, inspired by OBS's Game Capture functionality.

#### New Project: Pick6.VulkanHook

Created a new C# DLL project (`src/Pick6.VulkanHook/`) that serves as the injectable hook for capturing frames from FiveM:

- **VulkanHookEntryPoint.cs**: Main entry point and lifecycle management
  - Auto-initialization via static constructor when DLL is loaded
  - Clean shutdown handling
  
- **HookManager.cs**: DirectX/Vulkan hook management
  - Hooks into the Present function to capture frames
  - Manages hook lifecycle (install/remove)
  - Captures frame data at render time
  
- **FrameWriter.cs**: Inter-process communication via shared memory
  - Writes captured frames to Windows memory-mapped files
  - Implements frame header protocol (magic, dimensions, format, timestamp)
  - Efficient IPC with main application

#### Infrastructure Already in Place

The following components were already implemented in the repository:

- **FiveMDetector** (Pick6.Core): Identifies and monitors FiveM processes
- **VulkanInjector** (Pick6.Core): DLL injection using LoadLibrary/CreateRemoteThread
- **EnhancedInjector** (Pick6.Core): Multi-strategy injection with fallback mechanisms
- **GameCaptureEngine** (Pick6.Core): Orchestrates the capture pipeline
- **VulkanFrameCapture** (Pick6.Core): Manages frame capture from injected DLL
- **SharedMemoryBuffer** (Pick6.Core): Reads frames from shared memory

#### Documentation

- **ARCHITECTURE.md**: Comprehensive architecture documentation
  - System overview and component descriptions
  - Game capture flow diagram
  - DLL injection strategies
  - Inter-process communication protocol
  - Build instructions and troubleshooting
  
- **README.md**: Updated with project structure section
  - Component descriptions
  - How the system works
  - Reference to detailed architecture docs

- **src/Pick6.VulkanHook/README.md**: Hook DLL specific documentation
  - Component architecture
  - IPC protocol details
  - Building and deployment notes

#### Build System

- **build.bat**: Automated build script for all projects
  - Builds all four projects in correct dependency order
  - Validates .NET SDK installation
  - Provides clear error messages and build output locations

### Technical Details

#### DLL Injection Flow

1. Main application (Pick6.Loader) starts
2. FiveMDetector identifies running FiveM processes
3. VulkanInjector/EnhancedInjector injects Pick6VulkanHook.dll
4. Hook DLL auto-initializes and installs Present hook
5. Each frame is captured and sent via shared memory
6. Main application reads and displays frames

#### Shared Memory Protocol

The system uses Windows memory-mapped files for IPC:

- **Name format**: `Pick6_Frames_{ProcessId}`
- **Header format**:
  - Magic: 0x50494B36 ("PIK6")
  - Width, Height: Frame dimensions
  - Format: Pixel format identifier
  - DataSize: Size of frame data
  - Timestamp: Capture timestamp
- **Data**: Raw pixel data follows header

#### Multi-Strategy Injection

EnhancedInjector supports multiple injection strategies with automatic fallback:

1. Direct injection (LoadLibrary via CreateRemoteThread)
2. DXGI proxy DLL injection
3. D3D11 proxy DLL injection
4. Vulkan proxy DLL injection

### Notes

- The hook DLL is currently implemented in C# for demonstration
- For production use, a native C++ hook would be more appropriate
- DirectX/Vulkan hooking logic is currently a placeholder
- Requires administrator privileges for DLL injection
- Compatible with Windows 10/11 64-bit

### Future Enhancements

- Native C++ hook DLL using MinHook or Detours
- Actual DirectX/Vulkan API hooking implementation
- Support for multiple simultaneous captures
- Video recording functionality
- Performance metrics overlay

---

## Previous Releases

See GitHub releases for previous version history.
