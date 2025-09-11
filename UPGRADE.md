# Pick66 Upgrade Guide

This document provides guidance for upgrading from previous versions of Pick66 and understanding the transformation to a projection interface application.

## Major Changes in This Version

### ⚠️ Breaking Change: Lottery Functionality Removed

**Important:** This version of Pick66 has completely removed all lottery number generation functionality. The application is now focused exclusively on projection interface capabilities.

### What's New

#### Enhanced WPF Application
- **Modern Projection Interface**: Professional WPF application with colored theme
- **Blue Accent Theme**: Modern dark theme with blue accent colors (#3B82F6)
- **Start/Stop Controls**: Simple projection control interface
- **Activity Logging**: Real-time activity monitoring and logging
- **Status Indicators**: Visual status indicators with colored states

#### Enhanced Installation Process
**New PowerShell Install Script** (`install.ps1`)
- Targets the new WPF projection interface application
- Self-contained Windows executable 
- Single-command installation to Downloads folder
- No .NET runtime installation required
- Optional launch and clean build flags

### Removed Functionality

**⚠️ Important:** The following features have been completely removed:
- **Lottery Number Generation**: Pick66.Core library and all related functionality
- **Console Demo**: Pick66.Console application 
- **Unit Tests**: Pick66.Tests project (lottery-specific tests)
- **Fisher-Yates Algorithm**: Random number generation logic
- **Ticket Generation**: All ticket-related UI and logic

### Preserved Functionality

Game capture and projection functionality from the Pick6 modules remains available:
- **Game Capture**: Pick6 FiveM capture system 
- **Settings Persistence**: Configuration storage capabilities
- **Mod Menu GUI**: ImGui-style interfaces

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
# Launch the projection interface application
pick66.exe
```

## Migrating from Previous Versions

### ⚠️ Breaking Changes

**Lottery Functionality Removed**: If you were using Pick66 for lottery number generation, you will need to use alternative software as this functionality has been completely removed.

### Settings Migration

Settings for non-removed functionality are preserved:
- **Location**: Application-managed configuration
- **Format**: WPF application settings
- **Migration**: Automatic interface adaptation

### Configuration Files

Configuration is now managed through the application interface:
- Previous lottery settings are no longer applicable
- Projection interface settings are managed internally
- No manual configuration files required

### Build Process Changes

The build process now targets the WPF application:

**Previous Process**:
```bash
dotnet build -c Release
dotnet publish src/Pick6.Loader -c Release -r win-x64 --self-contained
```

**New Process**:
```powershell
.\install.ps1
```

### Project Structure

The solution structure has been updated:
- `Pick6.Core`: Game capture functionality (preserved)
- `Pick66.App`: Projection interface WPF application (new)
- `Pick6.Loader`: Main application entry point (preserved)
- `Pick6.ModGui`: User interface (preserved)
- `Pick66.Tests`: Unit tests

## Feature Comparison

| Feature | Previous | Current | Status |
|---------|----------|---------|--------|
| Projection Interface | ❌ | ✅ | **New** |
| Colored Theme | ❌ | ✅ | **New** |
| Activity Logging | ❌ | ✅ | **New** |
| WPF Application | ❌ | ✅ | **New** |
| Game Capture | ✅ | ✅ | Preserved |
| Single-file Build | ❌ | ✅ | **New** |
| Install Script | ❌ | ✅ | **Enhanced** |
| Status Indicators | ❌ | ✅ | **New** |

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