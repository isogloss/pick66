# Pick66 - Windows Game Capture Application

Pick66 is a streamlined game capture application for Windows.

## 🚀 Installation

**Simple 1-Step Process:**
1. Download `BINGO.bat` only
2. Run `BINGO.bat`

That's it! BINGO will automatically:
- Download the latest source code from GitHub
- Build the application
- Create `loader.exe` 
- Install to `dist/` folder

**No need to download the entire repository!** Just get the BINGO.bat file and run it.

## 🔧 Requirements

- Windows 10/11 (x64)
- .NET 8 SDK

## 🎮 Usage

After installation:
```cmd
dist\loader.exe
```

## 🏗️ Manual Build

```cmd
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output dist
```

## 📋 Project Structure

- `BINGO.bat` - Installer script
- `loader.exe` - Single executable (built from src/Pick6.Loader)
- `src/Pick6.Core/` - Core capture engine
- `src/Pick6.Projection/` - Display projection

---

**Windows-only application with minimal dependencies.**