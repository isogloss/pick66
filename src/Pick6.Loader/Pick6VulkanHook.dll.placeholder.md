# Pick6VulkanHook.dll Placeholder

This is a placeholder file. The actual `Pick6VulkanHook.dll` must be built from the Vulkan hook project.

## Building Pick6VulkanHook.dll

The Pick6VulkanHook.dll is the core component that hooks into the Vulkan rendering pipeline to capture frames from FiveM.

### Requirements
- Visual Studio 2019 or later with C++ development tools
- Vulkan SDK
- CMake (optional, depending on the build system used)

### Build Instructions
1. Navigate to the Vulkan hook project directory
2. Open the solution file or build with CMake
3. Build in Release mode for x64
4. Copy the resulting `Pick6VulkanHook.dll` to this directory

### Expected Size
The compiled DLL should be approximately 100-500 KB in size.

### Critical Note
**This DLL is REQUIRED for the application to function.** Without it, the application will fail with:
```
[Error] Core hook DLL not found: <path>\Pick6VulkanHook.dll
```

## Deployment
When deploying the application:
1. Build this DLL
2. Place it in the same directory as `loader.exe`
3. The PathResolver will find it automatically
