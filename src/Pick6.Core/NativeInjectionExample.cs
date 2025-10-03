using System;
using System.Threading.Tasks;
using Pick6.Core;

namespace Pick6.Examples;

/// <summary>
/// Example demonstrating how to use the native C++ injector from C#
/// This shows integration between the managed C# code and native Pick6Native.dll
/// </summary>
public class NativeInjectionExample
{
    /// <summary>
    /// Example: Basic usage of the native injector
    /// </summary>
    public static void BasicExample()
    {
        Console.WriteLine("=== Basic Native Injector Example ===\n");

        // Check if native DLL is available
        if (!NativeInjector.IsAvailable())
        {
            Console.WriteLine($"ERROR: Native DLL not found at: {NativeInjector.GetExpectedDllPath()}");
            Console.WriteLine("Please build Pick6Native.dll and place it in the application directory.");
            return;
        }

        Console.WriteLine("✓ Native injector initialized\n");

        // Enable debug privilege
        if (NativeInjector.EnableSeDebugPrivilege())
        {
            Console.WriteLine("✓ SeDebugPrivilege enabled");
        }
        else
        {
            Console.WriteLine("⚠ Failed to enable SeDebugPrivilege (may need Administrator)");
        }

        // Find FiveM processes
        Console.WriteLine("\nSearching for FiveM processes...");
        uint[] processIds = NativeInjector.FindFiveMProcesses();

        if (processIds.Length == 0)
        {
            Console.WriteLine("No FiveM processes found");
            return;
        }

        Console.WriteLine($"Found {processIds.Length} FiveM process(es):\n");

        foreach (uint pid in processIds)
        {
            Console.WriteLine($"Process ID: {pid}");
            
            // Check if process is still running
            if (NativeInjector.IsProcessRunning(pid))
            {
                Console.WriteLine("  Status: Running");
                
                // Check which graphics APIs it's using
                if (NativeInjector.IsProcessUsingModule(pid, "vulkan"))
                {
                    Console.WriteLine("  Graphics: Vulkan");
                }
                else if (NativeInjector.IsProcessUsingModule(pid, "d3d11"))
                {
                    Console.WriteLine("  Graphics: Direct3D 11");
                }
                else if (NativeInjector.IsProcessUsingModule(pid, "dxgi"))
                {
                    Console.WriteLine("  Graphics: DXGI");
                }
            }
            else
            {
                Console.WriteLine("  Status: Not running");
            }
            
            Console.WriteLine();
        }

        // Cleanup
        NativeInjector.Cleanup();
    }

    /// <summary>
    /// Example: Direct DLL injection into a process
    /// </summary>
    public static void DirectInjectionExample(uint processId, string dllPath)
    {
        Console.WriteLine("=== Direct Injection Example ===\n");

        // Initialize
        if (!NativeInjector.Initialize())
        {
            Console.WriteLine("Failed to initialize native injector");
            return;
        }

        // Enable privileges
        NativeInjector.EnableSeDebugPrivilege();

        // Perform injection
        Console.WriteLine($"Injecting {dllPath} into process {processId}...");
        
        if (NativeInjector.InjectDirect(processId, dllPath, out string errorMessage))
        {
            Console.WriteLine("✓ Injection successful!");
        }
        else
        {
            Console.WriteLine($"✗ Injection failed: {errorMessage}");
        }

        NativeInjector.Cleanup();
    }

    /// <summary>
    /// Example: Proxy DLL deployment
    /// </summary>
    public static void ProxyDeploymentExample(uint processId, string proxyDllName)
    {
        Console.WriteLine("=== Proxy DLL Deployment Example ===\n");

        // Initialize
        if (!NativeInjector.Initialize())
        {
            Console.WriteLine("Failed to initialize native injector");
            return;
        }

        // Enable privileges
        NativeInjector.EnableSeDebugPrivilege();

        // Deploy proxy
        Console.WriteLine($"Deploying {proxyDllName} proxy to process {processId}...");
        
        if (NativeInjector.DeployProxy(processId, proxyDllName, out string errorMessage))
        {
            Console.WriteLine("✓ Proxy deployment successful!");
            Console.WriteLine("The proxy will be loaded when the process loads the graphics module.");
        }
        else
        {
            Console.WriteLine($"✗ Proxy deployment failed: {errorMessage}");
        }

        NativeInjector.Cleanup();
    }

    /// <summary>
    /// Example: Integration with existing EnhancedInjector
    /// Shows how to add native injection as a fallback strategy
    /// </summary>
    public static async Task<InjectionResult> HybridInjectionExample(ProcessInfo process)
    {
        Console.WriteLine("=== Hybrid C#/Native Injection Example ===\n");

        // Try native injector first
        if (NativeInjector.IsAvailable())
        {
            Console.WriteLine("Attempting native injection...");
            
            if (!NativeInjector.Initialize())
            {
                Console.WriteLine("Native injector initialization failed");
            }
            else
            {
                NativeInjector.EnableSeDebugPrivilege();

                // Get the hook DLL path
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var hookDllPath = System.IO.Path.Combine(baseDir, "Pick6VulkanHook.dll");

                if (System.IO.File.Exists(hookDllPath))
                {
                    if (NativeInjector.InjectDirect((uint)process.ProcessId, hookDllPath, out string error))
                    {
                        Console.WriteLine("✓ Native injection successful");
                        NativeInjector.Cleanup();
                        return InjectionResult.Succeeded(InjectionStrategy.Direct, 
                            "Native direct injection completed");
                    }
                    else
                    {
                        Console.WriteLine($"Native injection failed: {error}");
                    }
                }

                NativeInjector.Cleanup();
            }
        }

        // Fallback to C# injector
        Console.WriteLine("Falling back to C# injector...");
        
        using var enhancedInjector = new EnhancedInjector();
        return await enhancedInjector.InjectIntoProcessAsync(process);
    }

    /// <summary>
    /// Example: Complete workflow with error handling
    /// </summary>
    public static async Task CompleteWorkflowExample()
    {
        Console.WriteLine("=== Complete Workflow Example ===\n");

        try
        {
            // Step 1: Check native DLL availability
            Console.WriteLine("Step 1: Checking native DLL availability...");
            if (!NativeInjector.IsAvailable())
            {
                Console.WriteLine($"⚠ Native DLL not available at: {NativeInjector.GetExpectedDllPath()}");
                Console.WriteLine("Continuing with C# injector only...\n");
            }
            else
            {
                Console.WriteLine("✓ Native DLL available\n");
            }

            // Step 2: Initialize and enable privileges
            Console.WriteLine("Step 2: Enabling privileges...");
            if (NativeInjector.IsAvailable())
            {
                NativeInjector.Initialize();
                if (NativeInjector.EnableSeDebugPrivilege())
                {
                    Console.WriteLine("✓ SeDebugPrivilege enabled\n");
                }
                else
                {
                    Console.WriteLine("⚠ Failed to enable SeDebugPrivilege\n");
                }
            }

            // Step 3: Find target process
            Console.WriteLine("Step 3: Finding FiveM process...");
            uint[] processIds = NativeInjector.IsAvailable() 
                ? NativeInjector.FindFiveMProcesses() 
                : Array.Empty<uint>();

            if (processIds.Length == 0)
            {
                Console.WriteLine("No FiveM processes found. Waiting for process...\n");
                
                // Use C# process watcher
                using var injector = new EnhancedInjector();
                var result = await injector.FindAndInjectAsync();
                
                Console.WriteLine(result.Success 
                    ? $"✓ Injection successful: {result.Message}" 
                    : $"✗ Injection failed: {result.Message}");
                
                return;
            }

            Console.WriteLine($"✓ Found {processIds.Length} process(es)\n");

            // Step 4: Attempt injection
            uint targetPid = processIds[0];
            Console.WriteLine($"Step 4: Injecting into process {targetPid}...");

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var hookDllPath = System.IO.Path.Combine(baseDir, "Pick6VulkanHook.dll");

            if (System.IO.File.Exists(hookDllPath))
            {
                if (NativeInjector.InjectDirect(targetPid, hookDllPath, out string error))
                {
                    Console.WriteLine("✓ Direct injection successful!\n");
                }
                else
                {
                    Console.WriteLine($"✗ Direct injection failed: {error}");
                    Console.WriteLine("Attempting proxy injection...\n");
                    
                    // Try proxy injection as fallback
                    if (NativeInjector.DeployProxy(targetPid, "dxgi.dll", out error))
                    {
                        Console.WriteLine("✓ Proxy deployment successful!\n");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Proxy deployment failed: {error}\n");
                    }
                }
            }
            else
            {
                Console.WriteLine($"⚠ Hook DLL not found: {hookDllPath}\n");
            }

            // Step 5: Cleanup
            Console.WriteLine("Step 5: Cleanup...");
            if (NativeInjector.IsAvailable())
            {
                NativeInjector.Cleanup();
            }
            Console.WriteLine("✓ Complete\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
