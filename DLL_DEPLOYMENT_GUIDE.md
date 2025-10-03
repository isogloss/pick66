# DLL Deployment Guide for Pick6

## Overview

The Pick6 application embeds native DLL files as resources within the single executable for truly portable distribution. The DLLs are automatically extracted to a temporary directory at runtime when needed for injection.

### Embedded DLLs
1. **Pick6VulkanHook.dll** - Core Vulkan frame capture hook (REQUIRED)
2. **Pick6Native.dll** - Native C++ injection library (optional, but recommended)
3. **Proxy DLLs** (optional, for proxy injection strategies):
   - `dxgi.dll` or `Pick6DxgiProxy.dll`
   - `d3d11.dll` or `Pick6D3d11Proxy.dll`
   - `vulkan-1.dll` or `Pick6VulkanProxy.dll`

## How It Works

The application uses **single-file publishing with embedded resources**:
- All native DLLs are embedded as resources within `pick6.exe`
- On first use, DLLs are automatically extracted to `%TEMP%/Pick6/Native/`
- The extracted DLLs are used for injection into target processes
- This provides a truly single-file distribution while maintaining full functionality

## Distribution

Simply distribute the single `pick6.exe` file. No additional DLLs are needed:

```
Pick6/
└── pick6.exe  (contains all embedded DLLs)
```

### Option 3: Embed DLLs as Resources (Current Implementation)

The project is configured to embed DLLs as resources:

```xml
<ItemGroup>
  <!-- Native DLLs embedded as resources -->
  <EmbeddedResource Include="Pick6VulkanHook.dll" Condition="Exists('Pick6VulkanHook.dll')">
    <LogicalName>Pick6VulkanHook.dll</LogicalName>
  </EmbeddedResource>
  <EmbeddedResource Include="Pick6Native.dll" Condition="Exists('Pick6Native.dll')">
    <LogicalName>Pick6Native.dll</LogicalName>
  </EmbeddedResource>
</ItemGroup>
```

**Benefits:**
- True single-file distribution (no separate DLLs)
- DLLs are automatically extracted at runtime
- Works correctly with DLL injection

**How it works:**
The `ResourceExtractor` class extracts embedded DLLs to `%TEMP%/Pick6/Native/` at runtime, and the `PathResolver` class locates them for injection.

## Build Instructions

### Building Native Components

The native DLLs must be built separately:

1. **Pick6VulkanHook.dll** - See Vulkan hook project documentation
2. **Pick6Native.dll** - Build using CMake:
   ```bash
   cd src/Pick6.Native
   build.bat
   ```

### Including DLLs in Build

After building native DLLs, copy them to the Loader project directory:

```bash
copy src\Pick6.Native\build\Release\Pick6Native.dll src\Pick6.Loader\
copy path\to\Pick6VulkanHook.dll src\Pick6.Loader\
```

Then build the loader (DLLs will be embedded as resources):

```bash
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
  --configuration Release ^
  --runtime win-x64 ^
  --self-contained true ^
  --output output\
```

The result is a single `pick6.exe` file with all DLLs embedded. The DLLs are automatically extracted to `%TEMP%/Pick6/Native/` at runtime when needed for injection.

## .gitignore Considerations

The current `.gitignore` excludes all `.dll` files:
```
*.dll
```

This means native DLLs are not tracked in the repository. You have two options:

### Option A: Keep DLLs Out of Git (Current Approach)
- Build native DLLs separately
- Copy them to the output directory during deployment
- Include build instructions in CI/CD pipeline

### Option B: Allow Specific DLLs in Git
Update `.gitignore` to allow essential DLLs:

```gitignore
# Exclude all DLLs by default
*.dll

# But include our native DLLs
!Pick6VulkanHook.dll
!Pick6Native.dll
!*Proxy.dll
```

## Troubleshooting

### Error: "Core hook DLL not found"

This error occurs when `Pick6VulkanHook.dll` is missing or failed to extract from embedded resources.

**Solution**: 
1. Ensure `Pick6VulkanHook.dll` was embedded during the build
2. Check that you have write permissions to `%TEMP%/Pick6/Native/`
3. Run as administrator
4. Check application logs for extraction errors

### Error: "Pick6Native.dll not found"

This is a warning, not a fatal error. The application will fall back to managed C# injection.

**Solution**: If you want to use native injection, ensure `Pick6Native.dll` was embedded during the build.

## Automated Deployment

For automated builds and deployments, consider:

1. **Build Script**: Create a script that builds all components and copies DLLs
2. **Post-Build Event**: Add post-build events to the `.csproj` file
3. **CI/CD Pipeline**: Use GitHub Actions to build and package all components

Example post-build event:

```xml
<Target Name="CopyNativeDlls" AfterTargets="Build">
  <Copy SourceFiles="$(SolutionDir)Pick6.Native\build\Release\Pick6Native.dll" 
        DestinationFolder="$(OutputPath)" 
        SkipUnchangedFiles="true" 
        Condition="Exists('$(SolutionDir)Pick6.Native\build\Release\Pick6Native.dll')" />
</Target>
```

## Summary

The key takeaway: **Native DLLs must be deployed alongside the executable**. The new `PathResolver` class ensures the application looks in the correct location, but the DLLs must actually be present there.
