# Pick66 Upgrade Guide

This document provides guidance for upgrading from previous versions of Pick66 and understanding the new features and installation process.

## What's New

### Enhanced Installation Process

**New PowerShell Install Script** (`install.ps1`)
- One-command installation to Downloads folder
- SHA256 hash verification for security
- Single-file, self-contained Windows executable (~21MB)
- No .NET runtime installation required
- Optional launch and clean build flags

### Preserved Functionality

All existing features remain fully functional:

- **Lottery Number Generation**: Full Pick66.Core functionality preserved
- **Game Capture**: Complete Pick6 FiveM capture system maintained
- **Command Line Interface**: All existing CLI arguments work unchanged
- **Settings Persistence**: Configuration continues to be stored in `%APPDATA%/Pick6`
- **Update System**: Auto-update mechanism remains available
- **Mod Menu GUI**: Current ImGui-style interface preserved

## Installation

### Quick Install (Recommended)

```powershell
# Clone or download the repository
git clone https://github.com/isogloss/pick66.git
cd pick66

# Install to Downloads folder and launch
.\install.ps1 -Launch
```

### Installation Options

```powershell
# Basic installation
.\install.ps1

# Clean build and install
.\install.ps1 -Clean

# Install and launch immediately
.\install.ps1 -Launch

# Custom output directory
.\install.ps1 -OutputPath "C:\MyApps"

# Combined options
.\install.ps1 -Clean -Launch
```

### Verification

After installation, verify the executable:

```cmd
# Check version and help
pick66.exe --help

# Test lottery generation
pick66.exe --check-updates-only
```

## Migrating from Previous Versions

### Settings Migration

Settings are automatically preserved across versions:
- **Location**: `%APPDATA%/Pick6/settings.json` (unchanged)
- **Format**: Compatible with previous versions
- **Migration**: Automatic on first run

### Configuration Files

No manual configuration changes required:
- Existing settings files remain compatible
- New settings use sensible defaults
- Validation ensures corrupted settings fall back to defaults

### Build Process Changes

The build process has been streamlined:

**Old Process**:
```bash
dotnet build -c Release
dotnet publish src/Pick6.Loader -c Release -r win-x64 --self-contained
```

**New Process**:
```powershell
.\install.ps1
```

### Project Structure

The solution structure remains largely unchanged:
- `Pick6.Core`: Game capture functionality
- `Pick66.Core`: Lottery number generation  
- `Pick6.Loader`: Main application entry point
- `Pick6.ModGui`: User interface
- `Pick66.Tests`: Unit tests

## Feature Comparison

| Feature | Previous | Current | Status |
|---------|----------|---------|--------|
| Lottery Generation | ✅ | ✅ | Preserved |
| Game Capture | ✅ | ✅ | Preserved |
| Console Interface | ✅ | ✅ | Preserved |
| GUI Interface | ✅ | ✅ | Enhanced |
| Single-file Build | ❌ | ✅ | **New** |
| Install Script | ❌ | ✅ | **New** |
| Hash Verification | ❌ | ✅ | **New** |
| Auto-launch Option | ❌ | ✅ | **New** |

## Troubleshooting

### Installation Issues

**"PowerShell execution policy" errors:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
.\install.ps1
```

**".NET SDK not found" errors:**
- Install .NET 8 SDK from https://dot.net
- Verify with `dotnet --version`

**"Project file not found" errors:**
- Ensure running from repository root directory
- Verify all source files are present

### Runtime Issues

**"Application won't start" errors:**
- Antivirus may quarantine single-file executables
- Add exception for Downloads folder or executable
- Re-run install script if file was quarantined

**"Settings not preserved" errors:**
- Check permissions on `%APPDATA%/Pick6` folder
- Run once as administrator if needed
- Settings will recreate with defaults if missing

## Command Line Compatibility

All previous command line arguments remain supported:

```bash
# Previous usage (still works)
pick66.exe --fps 144 --auto-start
pick66.exe --help
pick66.exe --check-updates-only

# GUI mode (default behavior)
pick66.exe
```

## Performance Notes

- **Startup**: First run may be slower due to .NET compilation
- **Size**: Single-file executable is larger (~21MB) but eliminates dependencies
- **Memory**: Similar memory usage to previous versions
- **FPS**: No performance impact on capture/projection systems

## Support

For issues with the upgrade process:

1. **Check logs**: Application logs are in the GUI log panel
2. **Verify build**: Run `dotnet build` manually to check for errors
3. **Clean install**: Use `.\install.ps1 -Clean` for fresh build
4. **Report issues**: GitHub issues with error details and environment info

## Advanced Usage

### Manual Build Process

If you prefer manual building:

```bash
# Clean previous builds
dotnet clean

# Build solution
dotnet build --configuration Release

# Publish single-file executable
dotnet publish src/Pick6.Loader/Pick6.Loader.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./publish \
  /p:PublishSingleFile=true \
  /p:IncludeAllContentForSelfExtract=true
```

### Custom Deployment

For enterprise or custom deployment scenarios:

```powershell
# Build to custom location
.\install.ps1 -OutputPath "\\shared\applications\pick66"

# Batch deployment script
foreach ($computer in $computers) {
    Copy-Item "pick66.exe" "\\$computer\c$\Applications\" -Force
}
```

---

For additional support or questions, refer to the main [README.md](README.md) or open an issue on GitHub.