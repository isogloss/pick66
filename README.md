# Pick66 - Windows-Only Game Capture Application

Pick66 is a high-performance OBS-style game capture application designed specifically for Windows. It's optimized for capturing and projecting game content with minimal performance impact.

## 🚀 One-Click Installation

### Prerequisites
- **Windows 10/11** (x64)
- **.NET 8 SDK** - Download from [https://dot.net](https://dot.net)

### Quick Install

1. **Clone or download** this repository
2. **Double-click** `setup.bat` in the repository folder
3. **Follow the prompts** - the installer will:
   - Build the application
   - Create a self-contained executable (~21MB)
   - Install to your Downloads folder
   - Optionally launch the application

That's it! No complex PowerShell scripts or command-line knowledge required.

## 📋 Features

### ⚙️ Installation & Distribution
- **One-Click Installer**: Simple batch file installation
- **Self-Contained Executable**: No .NET runtime dependencies required
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