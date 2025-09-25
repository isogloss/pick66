# Pick66 Installer Improvements

## Issues Fixed

### 1. Dependency Bundle Issues ✅
**Problem**: Users were prompted to download .NET runtime despite using `--self-contained true`
**Root Cause**: Inconsistent target frameworks between projects prevented proper dependency bundling
**Solution**: 
- Unified all projects to target `net8.0-windows10.0.17763.0`
- Added `IncludeAllContentForSelfExtract=true` for complete runtime bundling
- Configured `SelfContained=true` and `UseAppHost=true` explicitly

### 2. Installer Performance Issues ✅
**Problem**: Installer was very slow (3+ minutes)
**Root Cause**: Full restore and publish operations combined, excessive verbosity
**Solution**:
- Separated restore and publish phases for better error handling
- Added `--no-restore` flag to publish step
- Reduced verbosity to `quiet` for faster execution
- Added incremental build support with fast installer variant

## Key Improvements

### Enhanced Self-Contained Deployment
```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
  <SelfContained>true</SelfContained>
  <UseAppHost>true</UseAppHost>
</PropertyGroup>
```

### Optimized Build Process
- **Before**: `dotnet publish` (full operation, ~3+ minutes)
- **After**: `dotnet restore` → `dotnet publish --no-restore` (~1-2 minutes)

### Project Structure Consistency
- All projects now use `net8.0-windows10.0.17763.0` target framework
- Centralized configuration in `Directory.Build.props`
- Eliminated target framework mismatches

## New Features

### 1. Fast Installer (`install-fast.bat`)
For development and repeated installs:
- Uses incremental compilation
- Skips ReadyToRun optimization for speed
- Falls back to full install if needed

### 2. Configuration Validation (`validate-config.bat`)
Validates installer configuration before building:
- Checks project files and dependencies
- Verifies self-contained deployment settings
- Tests build configuration

### 3. NuGet Optimization (`NuGet.config`)
- Optimized package source configuration
- Local package caching for faster restores

## Expected Results

### For End Users
- ✅ **No .NET prompts**: Fully self-contained executable
- ✅ **Faster installation**: 1-2 minutes vs previous 3+ minutes  
- ✅ **Single file deployment**: All dependencies bundled
- ✅ **Better error handling**: Clear separation of restore vs build issues

### For Developers
- ✅ **Fast rebuilds**: `install-fast.bat` for development
- ✅ **Better debugging**: Separate restore and build phases
- ✅ **Consistent builds**: Unified target frameworks
- ✅ **Validation tools**: Pre-build configuration checking

## Installation Commands

### Standard Installation (End Users)
```batch
install.bat
```

### Fast Installation (Development)
```batch
install-fast.bat
```

### Configuration Validation
```batch
validate-config.bat
```

## Technical Details

### Target Framework Changes
- **Before**: Mixed `net8.0` and `net8.0-windows` 
- **After**: Consistent `net8.0-windows10.0.17763.0`

### Publish Settings
```batch
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --no-restore ^
    --verbosity quiet ^
    -p:PublishSingleFile=true ^
    -p:IncludeAllContentForSelfExtract=true
```

### Performance Optimizations
- Separated `dotnet restore` (network/IO bound)
- Optimized `dotnet publish` (CPU bound)
- Reduced console output with `--verbosity quiet`
- Added build caching with NuGet optimization

These changes should resolve both the dependency issues and performance problems reported in the original issue.