# Pick66 Streamlining Complete

## Summary of Changes

This repository has been successfully streamlined for **Windows-only** operation with a **one-click installer**. All Linux support has been removed to simplify development, deployment, and maintenance.

## What Was Removed

### Cross-Platform Complexity
- ❌ Bash build scripts (`build.sh`, `build-release.sh`)
- ❌ Complex PowerShell installer with Linux compatibility (`install.ps1`, `install.cmd`)
- ❌ Cross-platform conditional compilation
- ❌ Linux-specific documentation and references
- ❌ Problematic `Pick66.App` WPF project
- ❌ Platform-specific build configurations

### Development Overhead
- ❌ Conditional `#if WINDOWS` preprocessor directives
- ❌ Platform detection logic in build files
- ❌ Multiple target framework configurations
- ❌ Cross-platform testing complexity

## What Was Added/Improved

### Simple Installation
- ✅ **`setup.bat`** - One-click batch installer for all Windows users
- ✅ **`setup.ps1`** - Simple PowerShell installer for advanced users
- ✅ **Direct Downloads folder installation** - No complex path management

### Streamlined Build System
- ✅ **Windows-only project files** - All projects target `net8.0-windows`
- ✅ **Consistent configuration** - Single target framework across solution
- ✅ **Simplified dependencies** - Windows Forms and Windows-specific packages only
- ✅ **Self-contained executable** - No runtime dependencies required

### Improved Documentation
- ✅ **Windows-focused README** - Clear installation and usage instructions
- ✅ **System requirements** - Explicit Windows 10/11 requirement
- ✅ **Benefits explained** - Why Windows-only is better for this application

## Installation Process

### For End Users
1. **Download or clone** the repository
2. **Double-click** `setup.bat` 
3. **Follow prompts** - automatic build and installation
4. **Launch** from Downloads\Pick66\pick6_loader.exe

### For PowerShell Users
```powershell
.\setup.ps1 -Launch
```

### For Developers  
```cmd
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output dist
```

## Benefits of Windows-Only Approach

### Performance
- **Native Windows APIs** - Direct GDI+, Win32, DirectX integration
- **No abstraction overhead** - Direct platform API calls
- **Optimized for gaming** - Windows-specific performance optimizations

### Simplicity  
- **Single target platform** - No cross-platform compatibility issues
- **Smaller codebase** - Easier to maintain and debug
- **Focused testing** - Only need to test on Windows configurations

### User Experience
- **Native Windows feel** - Consistent with Windows design patterns
- **Better integration** - Windows-specific features and shortcuts
- **Simpler deployment** - Single executable, no runtime dependencies

## File Structure (After Streamlining)

```
pick66/
├── setup.bat              # One-click installer (primary)
├── setup.ps1              # PowerShell installer (alternative)
├── README.md               # Windows-only documentation
├── Pick6.sln               # Streamlined solution file
├── Directory.Build.props   # Windows-focused build configuration
└── src/
    ├── Pick6.Loader/       # Main entry point (Windows WinExe)
    ├── Pick6.Core/         # Capture engine (Windows library)
    ├── Pick6.ModGui/       # ImGui interface (Windows library)
    ├── Pick6.Projection/   # Display projection (Windows library)
    ├── Pick6.GUI/          # Legacy WinForms UI (Windows WinExe)
    ├── Pick6.UI/           # Shared utilities (Windows library)
    └── Pick6.Launcher/     # Console launcher (Windows Exe)
```

## Next Steps for Users

1. **Use `setup.bat`** for the simplest installation experience
2. **Run `pick6_loader.exe`** from the Downloads\Pick66 folder
3. **Enjoy the streamlined Windows-only experience** with better performance

## Next Steps for Developers

1. **Focus on Windows-specific optimizations** without cross-platform constraints
2. **Simplify CI/CD** - Only need Windows runners
3. **Enhanced Windows features** - Can now use Windows-only APIs without compatibility concerns

---

**Result**: Pick66 is now a focused, high-performance Windows game capture application with a simple one-click installation process.