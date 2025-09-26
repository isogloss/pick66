# Pick66 Installation Guide

Pick66 is a Windows-only application, but the installer is cross-platform compatible.

## Quick Start

### Cross-Platform Installation (Recommended)
```bash
# PowerShell Core (works on Windows, Linux, macOS)
pwsh install.ps1                    # Full installation
pwsh install.ps1 -Mode fast         # Fast incremental build
pwsh install.ps1 -Mode build        # Build only (no publish)
```

### Windows-Only Installation
```cmd
install.bat                         # Full installation
install.bat fast                    # Fast incremental build  
install.bat build                   # Build only (no publish)
```

## Requirements

- **.NET 8 SDK** or higher
- **PowerShell Core** (for cross-platform installation)
- **Windows 10/11** (runtime - the app itself only runs on Windows)

## Installation Modes

| Mode | Description | Output |
|------|-------------|---------|
| `full` | Complete clean build and publish | `loader.exe` ready to run |
| `fast` | Incremental build (falls back to full if needed) | `loader.exe` ready to run |
| `build` | Build only, no publish/packaging | Development build artifacts |

## Output Directory

By default, the installer creates the application in:
- **Windows**: `%USERPROFILE%\Desktop\Pick66\`
- **Linux/macOS**: `~/Desktop/Pick66/`

Override with environment variable:
```bash
export PICK66_OUTPUT="/custom/path"
pwsh install.ps1
```

## Troubleshooting

### "Installer instantly closes"
- **Cause**: Running Windows `.bat` file on Linux/macOS
- **Solution**: Use `pwsh install.ps1` instead

### ".NET 8 SDK required but not found"
- **Solution**: Install from https://dotnet.microsoft.com/download/dotnet/8.0

### "PowerShell not found" 
- **Solution**: Install PowerShell Core from https://github.com/PowerShell/PowerShell

### Build fails with framework errors
- **Cause**: Missing Windows-specific .NET components
- **Solution**: Ensure .NET 8 SDK with Windows workloads is installed

## Platform Support

- **Development**: Windows, Linux, macOS (via cross-compilation)
- **Runtime**: Windows 10/11 x64 only
- **Installer**: Cross-platform PowerShell or Windows batch script

The application targets `net8.0-windows10.0.17763.0` and uses Windows-specific APIs, so it can only run on Windows despite being buildable on other platforms.