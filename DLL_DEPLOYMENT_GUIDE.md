# DLL Deployment Guide for Pick6

## Critical Issue: Native DLLs Required

The Pick6 application requires several native DLL files to function properly:

### Required DLLs
1. **Pick6VulkanHook.dll** - Core Vulkan frame capture hook
2. **Pick6Native.dll** - Native C++ injection library (optional, but recommended)
3. **Proxy DLLs** (optional, for proxy injection strategies):
   - `dxgi.dll` or `Pick6DxgiProxy.dll`
   - `d3d11.dll` or `Pick6D3d11Proxy.dll`
   - `vulkan-1.dll` or `Pick6VulkanProxy.dll`

## The Problem

The application uses **single-file publishing** which:
- Extracts the executable to `C:\Users\...\AppData\Local\Temp\.net\...`
- Native DLLs cannot be bundled into the single-file package
- DLLs must be deployed separately in the same directory as the executable

## Solutions

### Option 1: Deploy DLLs Alongside Executable (Recommended)

The DLLs must be placed in the same directory as `loader.exe`:

```
Pick6/
├── loader.exe
├── Pick6VulkanHook.dll  (REQUIRED)
├── Pick6Native.dll       (optional)
├── dxgi.dll             (optional)
├── d3d11.dll            (optional)
└── vulkan-1.dll         (optional)
```

### Option 2: Multi-File Publishing

Modify `Pick6.Loader.csproj` to disable single-file publishing:

```xml
<PropertyGroup>
  <PublishSingleFile>false</PublishSingleFile>
  <!-- Remove or set to false: -->
  <!-- <IncludeNativeLibrariesForSelfExtract>false</IncludeNativeLibrariesForSelfExtract> -->
</PropertyGroup>
```

This will publish all DLLs in the output directory, making deployment simpler.

### Option 3: Mark DLLs to Extract Next to Executable

Add to `Pick6.Loader.csproj`:

```xml
<ItemGroup>
  <!-- Native DLLs that must be extracted next to the executable -->
  <Content Include="Pick6VulkanHook.dll">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
  </Content>
  <Content Include="Pick6Native.dll" Condition="Exists('Pick6Native.dll')">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
  </Content>
</ItemGroup>
```

When `ExcludeFromSingleFile` is set to true, these DLLs will be extracted to the same directory as the executable, not to the temp directory.

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

Then build the loader:

```bash
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
  --configuration Release ^
  --runtime win-x64 ^
  --self-contained true ^
  --output output\
```

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

This error occurs when `Pick6VulkanHook.dll` is missing. The error message now includes:
- Full path where the DLL was expected
- Path resolution diagnostics
- Troubleshooting steps

**Solution**: Ensure `Pick6VulkanHook.dll` is in the same directory as `loader.exe`.

### Error: "Pick6Native.dll not found"

This is a warning, not a fatal error. The application will fall back to managed C# injection.

**Solution**: If you want to use native injection, place `Pick6Native.dll` in the same directory as `loader.exe`.

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
