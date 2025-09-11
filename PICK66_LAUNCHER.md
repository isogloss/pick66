# Pick66 Launcher - D3D11/DXGI Proxy Implementation

## Overview

Pick66.Launcher is a Windows GUI application that implements a safe proxy DLL approach for game overlay and capture functionality. Unlike direct process injection, this approach uses proxy DLLs placed in the game directory to intercept D3D11/DXGI calls.

## Key Features

### ✅ Safe Proxy Approach
- **No Process Injection**: Uses proxy DLLs instead of injecting into running processes
- **Reversible Installation**: All changes can be undone with automatic backup/restore
- **Anti-cheat Friendly**: Does not use techniques that trigger anti-cheat systems
- **Local DLL Pattern**: Only copies DLLs to game directory, no system modifications

### ✅ Windows GUI Interface
- **Directory Selection**: Browse to select FiveM or GTA V installation directory
- **Automatic Detection**: Auto-detects game executables and existing DLLs
- **Proxy Type Selection**: Choose between DXGI.dll or D3D11.dll proxy (or auto-detect)
- **Status Monitoring**: Real-time status updates and installation verification

### ✅ ImGui Overlay System
- **In-game Overlay**: ImGui-based overlay rendered via D3D11
- **Hotkey Toggle**: Alt+F12 to show/hide overlay
- **Performance Metrics**: FPS, frame time, and capture statistics
- **Configuration Options**: Enable/disable overlay from launcher

### ✅ Backup and Restore
- **Automatic Backup**: Preserves existing d3d11.dll/dxgi.dll files
- **Safe Uninstall**: Restores original files when uninstalling
- **Version Tracking**: Configuration file tracks installation details

## Architecture

### Proxy DLL Implementation

#### DXGI Proxy (dxgi.dll)
```cpp
// Intercepts DXGI factory creation and swap chain operations
HRESULT CreateDXGIFactory(REFIID riid, void** ppFactory)
{
    // Call original system DXGI
    HRESULT result = s_originalCreateDXGIFactory(riid, ppFactory);
    
    // Wrap factory in proxy to intercept swap chain creation
    if (SUCCEEDED(result)) {
        *ppFactory = new DXGIFactoryProxy(*ppFactory);
    }
    
    return result;
}
```

#### D3D11 Hook Manager
```cpp
// Manages overlay and frame capture hooks
void OnSwapChainPresent(IDXGISwapChain* swapChain)
{
    // Check for overlay toggle (Alt+F12)
    if (IsOverlayTogglePressed()) {
        overlayManager->Toggle();
    }
    
    // Render ImGui overlay
    overlayManager->NewFrame();
    overlayManager->Render();
    
    // Present original frame + overlay
    swapChain->Present(...);
    overlayManager->Present();
}
```

### Installation Process

1. **Target Directory Selection**: User selects FiveM/GTA V folder
2. **DLL Detection**: Launcher detects existing d3d11.dll/dxgi.dll files  
3. **Proxy Type Selection**: Auto-detects best proxy type or allows manual override
4. **Backup Creation**: Backs up existing DLLs with .pick66_backup extension
5. **Proxy Installation**: Copies our proxy DLL to game directory
6. **Configuration**: Creates pick66_config.txt with settings

### Uninstallation Process

1. **Configuration Reading**: Reads pick66_config.txt to identify installation
2. **Proxy Removal**: Deletes our proxy DLL from game directory
3. **Backup Restoration**: Restores original .pick66_backup files
4. **Cleanup**: Removes configuration file

## Usage

### GUI Interface

1. **Launch Pick66.Launcher.exe** (Windows only)
2. **Browse Directory**: Select your FiveM or GTA V installation folder
3. **Configure Options**:
   - Proxy Type: Auto-detect, DXGI, or D3D11
   - Enable ImGui overlay (Alt+F12 toggle)
   - Automatic backup of existing DLLs
4. **Install Proxy**: Click "Install Proxy Hook"
5. **Launch Game**: Use "Launch Game" button or run game normally
6. **Use Overlay**: Press Alt+F12 in-game to toggle overlay

### Command Line (Future)
```bash
Pick66.Launcher.exe --install "C:\FiveM" --proxy dxgi --overlay
Pick66.Launcher.exe --uninstall "C:\FiveM"
Pick66.Launcher.exe --status "C:\FiveM"
```

## Files and Structure

### Launcher Files
```
Pick66.Launcher.exe          # Main GUI application
ProxyDlls/
├── dxgi.dll                 # DXGI proxy DLL  
└── d3d11.dll                # D3D11 proxy DLL
```

### Game Directory (After Installation)
```
[Game Directory]/
├── dxgi.dll                 # Our proxy DLL
├── dxgi.dll.pick66_backup   # Original system DLL (if existed)
├── pick66_config.txt        # Installation configuration
└── pick66_proxy.log         # Debug log (if logging enabled)
```

### Configuration File (pick66_config.txt)
```
ProxyType=DXGI
OverlayEnabled=true
InstallDate=2024-01-15 14:30:00
Version=1.0.0
```

## ImGui Overlay Features

### Performance Panel
- **FPS Counter**: Real-time frames per second
- **Frame Time**: Milliseconds per frame
- **Frame Count**: Total rendered frames

### Information Panel  
- **Proxy Version**: Current proxy DLL version
- **Installation Status**: Proxy type and status
- **Controls Help**: Hotkey information

### Settings Panel (Future)
- **Overlay Position**: Configurable overlay location
- **Transparency**: Overlay opacity settings
- **Color Scheme**: Theme selection

## Security and Compatibility

### Anti-cheat Considerations
- **No Process Injection**: Avoids techniques that trigger EAC/BattlEye
- **Local DLL Only**: Only affects local game directory
- **Reversible**: Can be completely removed without trace
- **Authorized Use**: Positioned as development/diagnostic tool

### Game Compatibility
- **FiveM**: All major versions supported
- **GTA V**: Compatible with Steam/Epic/Rockstar versions
- **Future Games**: Extensible to any D3D11/DXGI application

### System Requirements
- **OS**: Windows 10/11 (64-bit)
- **Runtime**: .NET 8.0 or later
- **Graphics**: D3D11 compatible GPU
- **Permissions**: Write access to game directory

## Development Notes

### Building on Windows
```bash
# Build launcher
dotnet build src/Pick66.Launcher/Pick66.Launcher.csproj -c Release

# Build proxy DLLs (requires MSVC)
cd src/Pick66.ProxyDlls
mkdir build && cd build
cmake .. -A x64
cmake --build . --config Release
```

### Testing
- Use with offline/development FiveM instances
- Test installation/uninstallation cycles
- Verify backup/restore functionality
- Test overlay toggle and performance

### Future Enhancements
- **Frame Capture**: Screenshot and recording capabilities
- **Network Overlay**: Display server information
- **Plugin System**: Extensible overlay modules
- **Multi-game Support**: Expand beyond FiveM/GTA V

## Legal and Ethical Use

This tool is designed for:
- ✅ Development and testing environments
- ✅ Offline single-player use
- ✅ Authorized server debugging
- ✅ Performance analysis and diagnostics

This tool should NOT be used for:
- ❌ Competitive multiplayer advantage
- ❌ Bypassing anti-cheat systems
- ❌ Unauthorized server access
- ❌ Terms of Service violations

Users are responsible for ensuring compliance with game ToS and local laws.