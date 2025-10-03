# GitHub Actions Workflows

This directory contains automated build workflows for the Pick6 project.

## Workflows

### build.yml (Primary Build Workflow)

**Purpose**: Complete automated build process that produces a single `pick6.exe` executable with embedded native DLLs.

**Triggers**:
- Push to `main` or `develop` branches
- Pull requests to `main`
- Manual workflow dispatch
- Git tags starting with `v*`

**Build Steps**:
1. **Setup Environment**: Installs .NET 8, CMake, and MSVC
2. **Build Pick6Native.dll**: Compiles C++ source with CMake
3. **Create Placeholder Pick6VulkanHook.dll**: Generates stub DLL for CI (replace with real one for production)
4. **Copy DLLs**: Moves native DLLs to Pick6.Loader directory
5. **Embed DLLs**: Builds single-file executable with DLLs as embedded resources
6. **Publish Artifact**: Uploads `pick6.exe` as build artifact
7. **Create Release** (on tags): Publishes to GitHub Releases

**Output**: 
- `pick6.exe` - Single executable with all dependencies embedded
- `Pick6-v{version}.exe` - Versioned copy for releases

**Key Features**:
- ✅ Truly single-file executable
- ✅ Native DLLs embedded as resources
- ✅ Automatic runtime extraction to `%TEMP%/Pick6/Native/`
- ✅ No separate DLL files needed for distribution

### build-loader.yml (Legacy)

**Status**: Maintained for compatibility

**Purpose**: Builds the loader application with separate DLL files.

**Note**: This workflow uses the older `ExcludeFromSingleFile` approach where DLLs are shipped separately alongside the executable.

### build-native.yml

**Purpose**: Builds only the native C++ components (Pick6Native.dll).

**Triggers**:
- Changes to `src/Pick6.Native/**`
- Manual workflow dispatch

**Output**: Pick6Native.dll and test executables

### build-suggested.yml (Legacy)

**Status**: Superseded by `build.yml`

**Purpose**: Earlier suggested workflow, replaced by the new `build.yml`.

## Usage

### For Development

To trigger a development build manually:
1. Go to Actions tab on GitHub
2. Select "Build Pick6 Single Executable" workflow
3. Click "Run workflow"
4. Download the artifact from the workflow run

### For Releases

To create a release:
1. Create and push a git tag: `git tag v1.0.0 && git push origin v1.0.0`
2. The workflow automatically builds and creates a GitHub release
3. The versioned executable is uploaded to the release

### Local Development

For local builds without CI:
```bash
# Build native components
cd src/Pick6.Native
build.bat

# Copy DLLs
copy build\Release\Pick6Native.dll ..\Pick6.Loader\

# Build single-file executable
dotnet publish src\Pick6.Loader\Pick6.Loader.csproj ^
  --configuration Release ^
  --runtime win-x64 ^
  --self-contained true ^
  --output dist\
```

## Important Notes

### Pick6VulkanHook.dll

The CI workflow creates a **placeholder stub** for `Pick6VulkanHook.dll` because the actual source is not in this repository. 

For production builds:
1. Build the actual Pick6VulkanHook.dll from the Vulkan hook project
2. Replace the placeholder in the build artifact
3. Or add the real DLL to the repository and update the workflow

### Build Requirements

The workflows require:
- Windows runner (for MSVC and Windows-specific tools)
- .NET 8 SDK
- CMake 3.15+
- Visual Studio 2022 build tools

### Artifact Retention

Build artifacts are retained for 90 days by default. This can be adjusted in the workflow file.

## Troubleshooting

### Build Fails at CMake Step

**Cause**: CMake configuration or Visual Studio installation issues

**Solution**: Check that Visual Studio 2022 is properly installed on the runner

### DLLs Not Embedded

**Cause**: DLL files don't exist when building the C# project

**Solution**: Ensure native build step completed successfully before C# build

### Placeholder DLL Issues

**Cause**: The placeholder VulkanHook DLL doesn't have the required exports

**Solution**: Replace with the actual Pick6VulkanHook.dll built from the Vulkan hook project
