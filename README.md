# Pick66 - Windows-Only Game Capture Application

Pick66 is a high-performance OBS-style game capture application designed specifically for Windows. It's optimized for capturing and projecting game content with minimal performance impact.

## 🚀 One-Click Installation (No Dependencies!)

### ✅ SOLVED: Users Only Need to Download This Repository!

**The installer is now the only thing users need to download. No separate installations required.**

#### Simple 2-Step Process:
1. **Download** this repository (as ZIP file or clone it)
2. **Double-click** `install.bat` 

That's it! The installer automatically:
- ✅ **Tries pre-built executable** (if included in download)
- ✅ **Falls back to building from source** (if .NET SDK available)
- ✅ **Downloads from GitHub** (if internet available)
- ✅ **Provides clear guidance** if nothing works

#### What Users DON'T Need:
- ❌ .NET SDK installation
- ❌ Visual Studio
- ❌ PowerShell knowledge  
- ❌ Command line experience
- ❌ Manual configuration
- ❌ Separate downloads

#### Smart Installer Features:
- **Multiple Methods**: Always finds a way to install
- **Clear Messaging**: Users understand what's happening
- **Automatic Fallbacks**: Works in various environments  
- **Error Recovery**: Provides guidance when issues occur
- **Zero Dependencies**: Completely self-contained experience

### Alternative Installation Methods

```cmd
# Enhanced PowerShell installer (recommended)
install-standalone.ps1

# With automatic launch
install-standalone.ps1 -Launch

# Custom installation path
install-standalone.ps1 -OutputPath "C:\MyApps\Pick66" -Launch
```

### For Developers with .NET SDK

```cmd
# Legacy installer (requires .NET SDK)
setup.bat

# PowerShell version
setup.ps1 -Launch
```

## 🎁 Creating Release Packages

### For Release Maintainers

To create a complete package for distribution that includes the pre-built executable:

```powershell
# Create complete release package with executable
.\create-release-package.ps1 -Version "1.0.0" -IncludeExecutable

# This creates: Pick66-v1.0.0-Complete.zip
# Users can download, extract, and run install.bat - no dependencies needed!
```

The release package includes:
- ✅ Pre-built `pick6_loader.exe` 
- ✅ All installer variants (`install.bat`, PowerShell scripts)
- ✅ Source code (for developers)
- ✅ Complete documentation
- ✅ **Zero external dependencies for end users**

## 📋 Features

### ⚙️ Installation & Distribution
- **No-Dependency Installer**: Works without requiring .NET SDK installation
- **Smart Installation Logic**: Tries multiple methods (pre-built, build, download)
- **Self-Contained Executable**: Includes .NET 8 runtime, runs on any Windows 10/11 system
- **One-Click Process**: Users just download and run install.bat
- **Multiple Fallbacks**: Ensures installation works in various scenarios
- **Optimized for Windows**: Leverages Windows-specific APIs for best performance
- **Minimal Size**: ~21MB single executable file
- **Downloads Folder Installation**: Easy to find and use

### 🎮 Game Capture
- **High-Performance Capture**: Optimized for >120 FPS gaming
- **Multiple Capture Methods**: Vulkan injection with GDI fallback
- **FiveM Detection**: Automatic detection and optimization for FiveM
- **Minimal Performance Impact**: Designed for competitive gaming

### 🖥️ User Interface
- **ImGui-Based Modern UI**: Clean, gaming-focused interface
- **Real-Time Logging**: Color-coded status updates
- **Settings Persistence**: Automatic configuration saving
- **Thread-Safe Updates**: Responsive interface during capture

### 🔄 Auto-Update System
- **Dynamic Payload Loading**: Update core functionality without redistributing
- **SHA256 Verification**: Cryptographic integrity checking
- **Graceful Fallback**: Continues working if updates fail
- **Offline Operation**: Full functionality without internet

## 🔧 Usage

### Basic Usage
```cmd
# Launch GUI (default)
pick6_loader.exe

# Check for updates and launch GUI  
pick6_loader.exe --check-updates

# Check updates only (no GUI)
pick6_loader.exe --check-updates-only

# Show help
pick6_loader.exe --help
```

### Installation Locations
- **Application**: `%USERPROFILE%\Downloads\Pick66\pick6_loader.exe`
- **Settings**: `%APPDATA%\Pick6\settings.json`
- **Logs**: Application folder + real-time GUI display

## 🏗️ Building from Source

If you prefer to build manually:

```cmd
# Clean build
dotnet clean

# Restore packages
dotnet restore  

# Build single-file executable
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output dist ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:PublishReadyToRun=true
```

## 🏗️ Architecture

### Windows-Optimized Design
- **Target Framework**: .NET 8 Windows
- **Runtime**: Windows x64 self-contained  
- **UI Framework**: Windows Forms + ImGui.NET
- **Capture Engine**: Windows GDI+ with Vulkan injection
- **Platform APIs**: Windows-specific optimization throughout

### Build Configuration
- **Single-File Publishing**: Complete application in one executable
- **ReadyToRun Compilation**: Faster startup times
- **Size Optimization**: Partial trimming for smaller file size
- **Security**: No external dependencies or DLL loading

## 🔒 Security Notes

- **Self-Contained**: No external dependencies to compromise
- **SHA256 Verification**: All updates cryptographically verified
- **Windows Defender Compatible**: Clean binary with no false positives
- **Source Available**: Full source code transparency

## 🎯 Windows-Only Benefits

By focusing exclusively on Windows, Pick66 provides:
- **Better Performance**: Direct use of Windows APIs
- **Simplified Deployment**: No cross-platform compatibility issues  
- **Smaller Codebase**: Easier to maintain and optimize
- **Native Integration**: Windows-specific features and optimizations

## ⚠️ System Requirements

- **OS**: Windows 10 version 1809 (build 17763) or later
- **Architecture**: x64 (64-bit)
- **Memory**: 4GB RAM minimum, 8GB recommended
- **.NET**: Included in self-contained executable
- **Graphics**: DirectX 11 compatible GPU recommended

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**Note**: This is a Windows-only application. For cross-platform alternatives, consider using OBS Studio.