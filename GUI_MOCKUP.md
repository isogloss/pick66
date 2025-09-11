# Pick66 Launcher GUI Mockup

Since the Windows Forms GUI can only run on Windows, here's a visual representation of the launcher interface:

```
┌──────────────────── Pick66 Launcher - D3D11/DXGI Proxy Injection ─────────────────────┐
│                                                                                        │
│  ┌─ Target Game Directory ────────────────────────────────────────────────────────┐   │
│  │                                                                                 │   │
│  │  Game Directory:                                                                │   │
│  │  ┌─────────────────────────────────────────────────────┐ ┌─────────────┐      │   │
│  │  │ C:\FiveM                                            │ │ Browse...   │      │   │
│  │  └─────────────────────────────────────────────────────┘ └─────────────┘      │   │
│  │                                                                                 │   │
│  │  Executable:                                                                    │   │
│  │  ┌─────────────────────────────────────────────────────┐ ┌─────────────┐      │   │
│  │  │ C:\FiveM\FiveM.exe                                  │ │ Browse...   │      │   │
│  │  └─────────────────────────────────────────────────────┘ └─────────────┘      │   │
│  │                                                                                 │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                        │
│  ┌─ Proxy Configuration ──────────────────────────────────────────────────────────┐   │
│  │                                                                                 │   │
│  │  Proxy DLL: ┌─────────────────┐                                                │   │
│  │             │ Auto-detect  ▼  │                                                │   │
│  │             └─────────────────┘                                                │   │
│  │                                                                                 │   │
│  │  ☑ Enable ImGui overlay (Alt+F12 to toggle)                                   │   │
│  │  ☑ Automatically backup existing DLLs                                         │   │
│  │                                                                                 │   │
│  │                                          ┌─────────────────┐ ┌─────────────┐  │   │
│  │                                          │ Install Proxy   │ │ Launch Game │  │   │
│  │                                          │ Hook            │ │             │  │   │
│  │                                          └─────────────────┘ │             │  │   │
│  │                                                              │             │  │   │
│  │                                          ┌─────────────────┐ │             │  │   │
│  │                                          │ Uninstall Proxy │ │             │  │   │
│  │                                          └─────────────────┘ └─────────────┘  │   │
│  │                                                                                 │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                        │
│  ┌─ Status ───────────────────────────────────────────────────────────────────────┐   │
│  │                                                                                 │   │
│  │  Ready - Select target directory to begin                                      │   │
│  │                                                                                 │   │
│  │  ┌─────────────────────────────────────────────────────────────────────────┐  │   │
│  │  │ Proxy Status: Not installed                                             │  │   │
│  │  │                                                                         │  │   │
│  │  │                                                                         │  │   │
│  │  │                                                                         │  │   │
│  │  │                                                                         │  │   │
│  │  └─────────────────────────────────────────────────────────────────────────┘  │   │
│  │                                                                                 │   │
│  └─────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                        │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

## GUI State Examples

### Initial State
```
Status: Ready - Select target directory to begin (Blue)
Proxy Status: Not installed (Gray)
Buttons: [Install] Enabled, [Uninstall] Disabled, [Launch] Disabled
```

### Directory Selected
```
Status: Directory selected - Configure options and install proxy (Blue)
Proxy Status: No proxy installed in C:\FiveM (Orange)
Buttons: [Install] Enabled, [Uninstall] Disabled, [Launch] Disabled
```

### Installing
```
Status: Installing proxy DLL... (Orange)
Proxy Status: Installing DXGI proxy to C:\FiveM\dxgi.dll (Orange)
Buttons: [Install] Disabled, [Uninstall] Disabled, [Launch] Disabled
```

### Successfully Installed
```
Status: Proxy installed successfully! (Green)
Proxy Status: Installed (DXGI)
DLL Path: C:\FiveM\dxgi.dll
Overlay: Enabled
Backup: Yes (C:\FiveM\dxgi.dll.pick66_backup)
Buttons: [Install] Disabled, [Uninstall] Enabled, [Launch] Enabled
```

### In-Game Overlay (Alt+F12)
```
┌─ Pick66 Overlay ─────────────────────┐
│                                      │
│  Pick66 D3D11/DXGI Proxy             │
│  ──────────────────────────────────   │
│                                      │
│  Performance:                        │
│    FPS: 60.1                         │
│    Frame Time: 16.650 ms             │
│    Frame Count: 3542                 │
│                                      │
│  ──────────────────────────────────   │
│                                      │
│  Proxy Information:                  │
│    Version: 1.0.0                    │
│    Overlay Enabled: Yes              │
│                                      │
│  ──────────────────────────────────   │
│                                      │
│  Controls:                           │
│    Alt+F12: Toggle this overlay      │
│                                      │
│  ┌────────────────┐                  │
│  │ Close Overlay  │                  │
│  └────────────────┘                  │
│                                      │
└──────────────────────────────────────┘
```

## Key Features Demonstrated

1. **User-Friendly Interface**: Clear sections for target selection, configuration, and status
2. **Real-Time Status**: Dynamic status updates during installation process  
3. **Safety Features**: Automatic backup indication and proxy type detection
4. **One-Click Operation**: Simple install/uninstall with clear visual feedback
5. **In-Game Integration**: Professional ImGui overlay with useful information

## Technical Implementation Notes

- **Windows Forms**: Native Windows GUI with proper DPI scaling
- **File Dialogs**: Standard Windows folder/file browsers
- **Status Updates**: Color-coded status messages (Blue/Orange/Green/Red)
- **Error Handling**: Comprehensive error messages and recovery options
- **Configuration**: Persistent settings and installation tracking

This GUI provides a professional, user-friendly interface for managing the D3D11/DXGI proxy installation while maintaining full transparency about what changes are being made to the system.