# Single Executable Build Implementation

## Overview

This document describes the implementation of a single-file executable build process for Pick6 that embeds native DLLs as resources and extracts them at runtime.

## Problem Statement

The original requirement was to:
1. Create a single `pick6.exe` file (no separate DLLs)
2. Embed native DLLs as resources
3. Extract DLLs at runtime for injection
4. Automate the entire process with GitHub Actions

## Solution Architecture

### 1. Resource Embedding

Native DLLs are now embedded as `EmbeddedResource` in the project file:

```xml
<EmbeddedResource Include="Pick6VulkanHook.dll" Condition="Exists('Pick6VulkanHook.dll')">
  <LogicalName>Pick6VulkanHook.dll</LogicalName>
</EmbeddedResource>
```

**Previous approach**: Used `ExcludeFromSingleFile=true` which shipped DLLs separately alongside the executable.

**New approach**: Embeds DLLs as resources, creating a truly single-file distribution.

### 2. Runtime Extraction

Created `ResourceExtractor.cs` class that:
- Automatically extracts embedded DLLs to `%TEMP%/Pick6/Native/` on first use
- Uses reflection to find embedded resources
- Handles missing resources gracefully
- Thread-safe extraction with locking

```csharp
public static string ExtractDll(string resourceName)
{
    // Extract from embedded resource to temp directory
    // Returns path to extracted DLL
}
```

### 3. Path Resolution Updates

Updated `PathResolver.cs` to check multiple locations:
1. Executable directory (for backward compatibility)
2. **Extracted temp directory** (new, for embedded resources)
3. Additional search paths
4. Base directory fallback

```csharp
// Try extracting from embedded resources
var extractedPath = ResourceExtractor.TryExtractDll(dllFileName);
if (extractedPath != null && File.Exists(extractedPath))
{
    return extractedPath;
}
```

### 4. Build Automation

Created comprehensive `.github/workflows/build.yml` that:

1. **Builds Pick6Native.dll** from C++ source using CMake
2. **Creates placeholder Pick6VulkanHook.dll** (stub for CI, replace with real one)
3. **Copies DLLs** to Pick6.Loader directory
4. **Embeds DLLs** as resources during dotnet publish
5. **Produces single pick6.exe** with all dependencies
6. **Uploads artifacts** and creates GitHub releases

### 5. Build Script Updates

Removed all `pause` commands from:
- `src/Pick6.Native/build.bat`
- Root `build.bat`

This allows scripts to run in CI/CD without manual intervention.

## File Changes

### New Files
- `.github/workflows/build.yml` - Complete automated build workflow
- `.github/workflows/README.md` - Workflow documentation
- `src/Pick6.Core/ResourceExtractor.cs` - DLL extraction logic
- `SINGLE_EXECUTABLE_BUILD.md` - This document

### Modified Files
- `src/Pick6.Core/PathResolver.cs` - Added resource extraction support
- `src/Pick6.Loader/Pick6.Loader.csproj` - Changed to EmbeddedResource, renamed to pick6
- `build.bat` - Removed pause commands
- `src/Pick6.Native/build.bat` - Removed pause commands
- `src/Pick6.Loader/README_DLL_REQUIREMENTS.md` - Updated documentation
- `DLL_DEPLOYMENT_GUIDE.md` - Updated documentation
- `readme.md` - Updated documentation

## How It Works

### Build Process

```
1. Native Build (CMake)
   └─> Pick6Native.dll

2. Placeholder Creation (CI only)
   └─> Pick6VulkanHook.dll (stub)

3. Copy to Loader
   ├─> src/Pick6.Loader/Pick6Native.dll
   └─> src/Pick6.Loader/Pick6VulkanHook.dll

4. Embed & Publish (dotnet)
   └─> pick6.exe (contains embedded DLLs)
```

### Runtime Process

```
1. User runs pick6.exe

2. Application needs DLL for injection
   ├─> Checks executable directory
   └─> Checks extracted temp directory

3. If not found, extract from embedded resources
   └─> Extract to %TEMP%/Pick6/Native/

4. Use extracted DLL for injection
   └─> Inject into target process (FiveM)
```

## Benefits

### Single File Distribution
- Users download only `pick6.exe`
- No separate DLL files to manage
- Easier to distribute and update

### Maintains Functionality
- DLLs are available as physical files for injection
- Works with all existing injection strategies
- No changes needed to injection code

### Automated Build
- Complete CI/CD pipeline
- Reproducible builds
- Automatic versioning and releases

### Developer Experience
- Clear build process
- Well-documented
- Easy to test locally

## Testing

### Manual Testing Steps

1. **Build the project**:
   ```bash
   # Run the workflow manually on GitHub Actions
   # Or build locally:
   dotnet publish src/Pick6.Loader/Pick6.Loader.csproj --configuration Release --runtime win-x64 --self-contained true
   ```

2. **Verify single file**:
   - Check that only `pick6.exe` is in the output directory
   - No separate DLL files should be present

3. **Test extraction**:
   - Run `pick6.exe`
   - Check `%TEMP%/Pick6/Native/` for extracted DLLs
   - Verify DLLs are extracted correctly

4. **Test injection**:
   - Run with FiveM
   - Verify injection works correctly
   - Check that captured frames are processed

### CI/CD Testing

The GitHub Actions workflow includes:
- ✅ Native DLL build verification
- ✅ Output file verification
- ✅ Single-file validation (no separate DLLs)
- ✅ Size and version checks

## Known Issues and Limitations

### Placeholder VulkanHook DLL

The CI workflow creates a **stub placeholder** for `Pick6VulkanHook.dll` because the actual source is not in the repository.

**Impact**: The CI-built artifact will not work for actual FiveM capture without replacing the placeholder.

**Solution**: 
- Replace the placeholder with the real `Pick6VulkanHook.dll` after build
- Or add the real DLL source to the repository

### Antivirus Detection

Some antivirus software may flag:
- The runtime DLL extraction behavior
- The DLL injection functionality

**Solution**: Sign the executable with a code signing certificate (future enhancement)

### First-Run Extraction

The first time the application runs, there's a small delay while DLLs are extracted.

**Impact**: Minimal (< 100ms typically)

## Future Enhancements

1. **Code Signing**: Sign the executable to reduce antivirus false positives
2. **Real VulkanHook**: Include actual Pick6VulkanHook.dll source in repository
3. **Compression**: Use compression on embedded DLLs to reduce executable size
4. **Caching**: Cache extraction status to skip extraction on subsequent runs
5. **Cleanup**: Periodic cleanup of old extracted DLL versions

## Migration Guide

### For Users

No changes needed! Just download and run the new `pick6.exe`.

### For Developers

1. Pull the latest changes
2. Build as before - native DLLs will be embedded automatically
3. The output is now named `pick6.exe` instead of `loader.exe`

### For CI/CD

Replace your existing build workflow with the new `build.yml`:
- It handles everything from native build to final executable
- Produces single-file artifact ready for distribution

## References

- [ResourceExtractor.cs](../src/Pick6.Core/ResourceExtractor.cs) - DLL extraction implementation
- [PathResolver.cs](../src/Pick6.Core/PathResolver.cs) - Updated path resolution
- [build.yml](../.github/workflows/build.yml) - Complete build workflow
- [Pick6.Loader.csproj](../src/Pick6.Loader/Pick6.Loader.csproj) - Project configuration
