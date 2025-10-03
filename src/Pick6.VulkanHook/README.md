# Pick6.VulkanHook

This is the injection DLL for the Pick6 game capture system. It is injected into the FiveM process to hook DirectX/Vulkan rendering functions and capture frames.

## Architecture

This DLL is designed to be injected into the target game process using the injection mechanisms in `Pick6.Core/VulkanInjector.cs`.

### Components

1. **VulkanHookEntryPoint.cs** - Main entry point that initializes and manages the hook lifecycle
2. **HookManager.cs** - Manages DirectX/Vulkan function hooks (Present, etc.)
3. **FrameWriter.cs** - Handles inter-process communication via shared memory to send captured frames back to the main application

## How It Works

1. The main application injects this DLL into the FiveM process
2. When loaded, the `AutoInitializer` static constructor automatically runs
3. The hook manager installs hooks on DirectX/Vulkan Present functions
4. Each frame, the hook callback captures the frame data
5. Frame data is written to shared memory where the main application can read it

## Inter-Process Communication

The DLL uses Windows shared memory (memory-mapped files) to communicate with the main application:

- Shared memory name: `Pick6_Frames_{ProcessId}`
- Frame format: Header (magic, width, height, format, size, timestamp) followed by raw pixel data
- Magic number: `0x50494B36` ("PIK6")

## Building

This project builds to `Pick6VulkanHook.dll` which must be placed in the same directory as the main application executable.

```bash
dotnet build src/Pick6.VulkanHook/Pick6.VulkanHook.csproj -c Release
```

## Notes

- This is currently a C# implementation for demonstration purposes
- For production use, a native C++ implementation using libraries like MinHook would be more appropriate
- The actual DirectX/Vulkan hooking logic is currently a placeholder and would need to be implemented
- Requires Windows API access for shared memory and process injection
