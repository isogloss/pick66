# Pick66 - Windows Game Capture Application

Pick66 is a streamlined game capture application for Windows.

## 🚀 Installation

**Simple 1-Step Process:**
1. Clone or download this repository
2. Run `install.bat`

That's it! The installer will automatically:
- Check for .NET 8 SDK requirements 
- Restore all necessary dependencies
- Build a fully self-contained executable on your Desktop
- Bundle all required .NET runtime components
- Set up all necessary components

## 🔧 Requirements

**Minimal requirements:**
- Windows 10/11 (x64)
- .NET 8 SDK for building (auto-bundled in final executable)

*The installed application runs standalone without requiring .NET to be installed on the target machine.*

*Installation takes 1-2 minutes (optimized from previous 3+ minutes).*

## 🎮 Usage

After installation, the executable will be on your Desktop:
```
%USERPROFILE%\Desktop\Pick66\loader.exe
```

Simply double-click `loader.exe` to launch Pick66.

## 🏗️ Manual Build

```cmd
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output %USERPROFILE%\Desktop\Pick66
```

## 📋 Project Structure

- `install.bat` - Simple installer script
- `src/Pick6.Core/` - Core capture engine
- `src/Pick6.Loader/` - Main application
- `src/Pick6.Projection/` - Display projection

---

**Windows-only application with minimal dependencies.**