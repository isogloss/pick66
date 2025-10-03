using System.Runtime.InteropServices;

namespace Pick6.VulkanHook;

/// <summary>
/// DLL entry point for injection into target process
/// This DLL will be injected into the FiveM process to hook DirectX/Vulkan
/// </summary>
public static class VulkanHookEntryPoint
{
    private static HookManager? _hookManager;

    /// <summary>
    /// Initialize the hook - called after DLL is loaded
    /// This is the main initialization point for the hook
    /// </summary>
    public static void Initialize()
    {
        try
        {
            _hookManager = new HookManager();
            _hookManager.InstallHooks();
            System.Diagnostics.Debug.WriteLine("Pick6VulkanHook: Initialization successful");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Pick6VulkanHook: Initialization failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Clean up the hook - called before DLL is unloaded
    /// </summary>
    public static void Cleanup()
    {
        try
        {
            _hookManager?.RemoveHooks();
            _hookManager = null;
            System.Diagnostics.Debug.WriteLine("Pick6VulkanHook: Cleanup successful");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Pick6VulkanHook: Cleanup failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Static constructor that runs when the assembly is loaded
/// This provides automatic initialization when the DLL is injected
/// </summary>
internal static class AutoInitializer
{
    static AutoInitializer()
    {
        // Automatically initialize when DLL is loaded
        try
        {
            VulkanHookEntryPoint.Initialize();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Pick6VulkanHook: Auto-initialization failed: {ex.Message}");
        }
    }

    // Force static constructor to run
    internal static void EnsureInitialized()
    {
        // This method intentionally left empty
        // Its existence ensures the static constructor runs
    }
}
