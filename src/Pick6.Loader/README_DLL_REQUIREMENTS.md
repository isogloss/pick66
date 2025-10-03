# Pick6.Loader - Native DLL Requirements

## Overview

The Pick6 Loader embeds native DLLs as resources within the executable for **truly single-file distribution**. The DLLs are automatically extracted to a temporary directory at runtime when needed for injection.

## Required Files for Building

Before building the Pick6.Loader project, ensure these native DLLs are present in this directory:

### ⚠️ REQUIRED
- **Pick6VulkanHook.dll** - Core Vulkan frame capture hook

### ✅ OPTIONAL (Recommended)
- **Pick6Native.dll** - Native C++ injection library for better compatibility

### ✅ OPTIONAL (For Proxy Injection)
- **dxgi.dll** or **Pick6DxgiProxy.dll**
- **d3d11.dll** or **Pick6D3d11Proxy.dll**
- **vulkan-1.dll** or **Pick6VulkanProxy.dll**

## Why These DLLs Are Separate

The Pick6.Loader uses **single-file publishing** to create a standalone executable. However, native DLLs used for injection into other processes **cannot be bundled** into the single-file package because:

1. They need to be injected into the target process (FiveM)
2. The target process needs direct file system access to them
3. DLLs in memory from the single-file extraction cannot be injected

## How It Works

The project configuration embeds these DLLs as resources:

```xml
<EmbeddedResource Include="Pick6VulkanHook.dll" Condition="Exists('Pick6VulkanHook.dll')">
  <LogicalName>Pick6VulkanHook.dll</LogicalName>
</EmbeddedResource>
```

This ensures that when you publish the application:
1. `pick6.exe` is created as a **truly single-file executable**
2. The native DLLs are **embedded as resources** within the executable
3. The `ResourceExtractor` class automatically extracts them to `%TEMP%/Pick6/Native/` at runtime
4. The `PathResolver` class finds them and makes them available for injection

## Build Instructions

### Step 1: Build Native Components

```bash
# Build Pick6Native.dll
cd ..\Pick6.Native
build.bat
copy build\Release\Pick6Native.dll ..\Pick6.Loader\

# Build or obtain Pick6VulkanHook.dll
# (See Pick6VulkanHook.dll.placeholder.md for instructions)
copy path\to\Pick6VulkanHook.dll ..\Pick6.Loader\
```

### Step 2: Build Pick6.Loader

```bash
cd ..\Pick6.Loader
dotnet publish Pick6.Loader.csproj ^
  --configuration Release ^
  --runtime win-x64 ^
  --self-contained true ^
  --output ..\..\output\
```

### Step 3: Verify Output

Check that the output directory contains:
```
output/
└── pick6.exe         (single-file executable with embedded DLLs)
```

The native DLLs are embedded within pick6.exe and will be extracted automatically at runtime to a temporary directory when needed.

## Troubleshooting

### "Core hook DLL not found" Error

**Cause**: `Pick6VulkanHook.dll` is missing or failed to extract from embedded resources.

**Solution**:
1. Ensure `Pick6VulkanHook.dll` was included during the build (it should be embedded in pick6.exe)
2. Check that you have write permissions to the temp directory (`%TEMP%/Pick6/Native/`)
3. Try running the application as administrator
4. Check the application logs for extraction errors

### "Pick6Native.dll not found" Warning

**Cause**: `Pick6Native.dll` is missing or was not embedded (this is not fatal).

**Solution**: This is optional. The application will fall back to managed C# injection if this DLL is not present. To include it, ensure Pick6Native.dll exists in the Pick6.Loader directory when building.

### DLLs Not Embedded

**Cause**: The DLLs don't exist in the source directory when building.

**Solution**: Build the native components first, then build the Loader project.

## Development Workflow

For active development:

1. Build native DLLs once
2. Copy them to `Pick6.Loader` directory
3. They will be automatically included in subsequent builds

For CI/CD:

1. Build native components in separate jobs
2. Copy artifacts to the Loader project
3. Build and publish the Loader project
4. Package all output files together

## See Also

- [DLL_DEPLOYMENT_GUIDE.md](../../DLL_DEPLOYMENT_GUIDE.md) - Comprehensive deployment guide
- [src/Pick6.Native/BUILD.md](../Pick6.Native/BUILD.md) - Instructions for building Pick6Native.dll
