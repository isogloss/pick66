# Pick66 Installation Instructions

## For End Users (No Technical Knowledge Required)

### Quick Start
1. **Double-click `install.bat`** - that's it!
2. The installer will automatically:
   - Find or download the Pick66 executable
   - Install it to your Downloads folder
   - Ask if you want to launch it

### What You Don't Need
- ❌ .NET SDK installation
- ❌ Visual Studio
- ❌ Command line knowledge
- ❌ PowerShell expertise
- ❌ Manual configuration

### Troubleshooting
If the installer doesn't work:
1. Try running `install-standalone.bat` directly
2. Try the PowerShell version: right-click and "Run with PowerShell" on `install-standalone.ps1`
3. Check your internet connection (needed if no pre-built executable is included)

## For Developers

### If You Have .NET SDK
```cmd
setup.bat
# or
setup.ps1 -Launch
```

### Manual Build
```cmd
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output dist
```

## Installation Locations
- **Application**: `%USERPROFILE%\Downloads\Pick66\pick6_loader.exe`
- **Settings**: `%APPDATA%\Pick6\settings.json`

## System Requirements
- Windows 10/11 (x64)
- 4GB RAM minimum, 8GB recommended
- DirectX 11 compatible GPU recommended
- No additional software required!