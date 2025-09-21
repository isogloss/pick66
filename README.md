# Pick66 - Windows Game Capture Application

Pick66 is a streamlined game capture application for Windows.

## 🚀 Installation

**Simple 1-Step Process:**
1. Clone or download this repository
2. Run `install.bat`

That's it! The installer will automatically:
- Build the application using .NET 8
- Create a ready-to-run executable on your Desktop
- Set up all necessary components

## 🔧 Requirements

**Minimal requirements:**
- Windows 10/11 (x64)
- .NET 8 SDK (download from [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0) if not installed)

*Installation takes 1-3 minutes.*

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