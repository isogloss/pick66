using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pick6.Core;

/// <summary>
/// Enhanced injector with native C++ DLL support
/// Automatically uses Pick6Native.dll when available for better injection compatibility
/// Falls back to managed C# injection if native DLL is not available
/// </summary>
public class HybridInjector : IDisposable
{
    private readonly InjectionConfig _config;
    private readonly ProcessWatcher _processWatcher;
    private readonly bool _nativeAvailable;
    private bool _disposed = false;

    public HybridInjector(InjectionConfig? config = null)
    {
        _config = config ?? InjectionConfig.Default;
        _processWatcher = new ProcessWatcher(_config);
        
        // Check if native DLL is available
        _nativeAvailable = NativeInjector.IsAvailable();
        
        if (_nativeAvailable)
        {
            Log.Info("Native injector available - will prefer native methods");
            NativeInjector.EnableSeDebugPrivilege();
        }
        else
        {
            Log.Info("Native injector not available - using managed C# injection only");
            Log.Verbose($"Expected native DLL path: {NativeInjector.GetExpectedDllPath()}");
        }
    }

    /// <summary>
    /// Find and inject into FiveM process with fallback strategies
    /// Uses native injector when available, falls back to C# implementation
    /// </summary>
    public async Task<InjectionResult> FindAndInjectAsync(CancellationToken cancellationToken = default)
    {
        Log.Info("Starting FiveM process detection and injection");

        // Pre-flight validation: Check if core hook DLL exists
        var hookDllPath = GetHookDllPath();
        if (!File.Exists(hookDllPath))
        {
            var errorMsg = $"Core hook DLL not found: {hookDllPath}. Please ensure Pick6VulkanHook.dll is in the application directory.";
            Log.Error(errorMsg);
            return InjectionResult.Failed(InjectionStrategy.Direct, errorMsg);
        }
        
        Log.Verbose($"Core hook DLL verified: {hookDllPath}");

        // Start process watcher
        _processWatcher.StartWatching();

        try
        {
            // Wait for a FiveM process to appear
            var process = await _processWatcher.WaitForProcessAsync(cancellationToken);
            if (process == null)
            {
                return InjectionResult.Failed(InjectionStrategy.Direct, "No FiveM process found within timeout period");
            }

            // Wait for graphics modules to load (if configured)
            if (_config.ModuleWaitTimeoutMs > 0)
            {
                Log.Info($"Waiting up to {_config.ModuleWaitTimeoutMs / 1000}s for graphics modules to load");
                await WaitForGraphicsModulesAsync(process.ProcessId, cancellationToken);
            }

            // Attempt injection with native or managed injector
            return await AttemptInjectionAsync(process, cancellationToken);
        }
        finally
        {
            _processWatcher.StopWatching();
        }
    }

    /// <summary>
    /// Inject into a specific process
    /// </summary>
    public async Task<InjectionResult> InjectIntoProcessAsync(ProcessInfo process, CancellationToken cancellationToken = default)
    {
        Log.Info($"Attempting injection into process: {process}");
        return await AttemptInjectionAsync(process, cancellationToken);
    }

    private async Task<InjectionResult> AttemptInjectionAsync(ProcessInfo process, CancellationToken cancellationToken)
    {
        var results = new List<InjectionResult>();
        var failureInfos = new List<StrategyFailureInfo>();

        foreach (var strategy in _config.InjectionOrder)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return InjectionResult.Failed(strategy, "Injection cancelled");
            }

            Log.Info($"Attempting {strategy} injection method");

            try
            {
                InjectionResult result;

                // Use native injector for direct injection if available
                if (strategy == InjectionStrategy.Direct && _nativeAvailable)
                {
                    result = await AttemptNativeDirectInjectionAsync(process, cancellationToken);
                }
                // Use native injector for proxy deployment if available
                else if (IsProxyStrategy(strategy) && _nativeAvailable)
                {
                    result = await AttemptNativeProxyInjectionAsync(process, strategy, cancellationToken);
                }
                // Fallback to managed C# injection
                else
                {
                    result = await AttemptManagedInjectionAsync(process, strategy, cancellationToken);
                }

                results.Add(result);

                if (result.Success)
                {
                    Log.Info($"✅ Injection successful using {strategy} method: {result.Message}");
                    return result;
                }
                else
                {
                    Log.Warn($"❌ {strategy} injection failed: {result.Message}");
                    
                    failureInfos.Add(new StrategyFailureInfo
                    {
                        Strategy = strategy,
                        ErrorMessage = result.Message,
                        Exception = result.Exception
                    });
                    
                    // Log fallback message if not the last strategy
                    if (strategy != _config.InjectionOrder[^1])
                    {
                        var nextStrategy = _config.InjectionOrder[Array.IndexOf(_config.InjectionOrder, strategy) + 1];
                        Log.Info($"Falling back to {nextStrategy} method...");
                    }
                }
            }
            catch (Exception ex)
            {
                var result = InjectionResult.Failed(strategy, $"Exception during {strategy} injection: {ex.Message}", ex);
                results.Add(result);
                failureInfos.Add(new StrategyFailureInfo
                {
                    Strategy = strategy,
                    ErrorMessage = $"Exception during {strategy} injection: {ex.Message}",
                    Exception = ex
                });
                Log.Error($"Exception during {strategy} injection: {ex.Message}");
            }

            await Task.Delay(500, cancellationToken);
        }

        // All strategies failed
        var lastResult = results[^1];
        var comprehensiveMessage = "All injection strategies failed. Please check:\n" +
                                   "  1. Run the application as Administrator\n" +
                                   "  2. Disable antivirus/security software temporarily\n" +
                                   "  3. Ensure Pick6VulkanHook.dll and proxy DLLs are present\n" +
                                   "  4. Check that the game directory is writable";
        
        Log.Error(comprehensiveMessage);
        
        return InjectionResult.FailedWithDetails(lastResult.Strategy, comprehensiveMessage, failureInfos);
    }

    private async Task<InjectionResult> AttemptNativeDirectInjectionAsync(ProcessInfo process, CancellationToken cancellationToken)
    {
        await Task.Yield();

        try
        {
            var hookDllPath = GetHookDllPath();
            
            if (NativeInjector.InjectDirect((uint)process.ProcessId, hookDllPath, out string errorMessage))
            {
                return InjectionResult.Succeeded(InjectionStrategy.Direct, 
                    "Native direct LoadLibrary injection completed");
            }
            else
            {
                return InjectionResult.Failed(InjectionStrategy.Direct, 
                    $"Native direct injection failed: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            return InjectionResult.Failed(InjectionStrategy.Direct, 
                $"Native direct injection exception: {ex.Message}", ex);
        }
    }

    private async Task<InjectionResult> AttemptNativeProxyInjectionAsync(ProcessInfo process, InjectionStrategy strategy, CancellationToken cancellationToken)
    {
        await Task.Yield();

        try
        {
            var proxyDllName = GetProxyDllNameForStrategy(strategy);
            
            if (NativeInjector.DeployProxy((uint)process.ProcessId, proxyDllName, out string errorMessage))
            {
                return InjectionResult.Succeeded(strategy, 
                    $"Native proxy {proxyDllName} deployed successfully");
            }
            else
            {
                return InjectionResult.Failed(strategy, 
                    $"Native proxy deployment failed: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            return InjectionResult.Failed(strategy, 
                $"Native proxy deployment exception: {ex.Message}", ex);
        }
    }

    private async Task<InjectionResult> AttemptManagedInjectionAsync(ProcessInfo process, InjectionStrategy strategy, CancellationToken cancellationToken)
    {
        // Use the existing managed C# injection implementation
        using var managedInjector = new EnhancedInjector(_config);
        return await managedInjector.InjectIntoProcessAsync(process, cancellationToken);
    }

    private bool IsProxyStrategy(InjectionStrategy strategy)
    {
        return strategy == InjectionStrategy.DxgiProxy ||
               strategy == InjectionStrategy.D3D11Proxy ||
               strategy == InjectionStrategy.VulkanProxy;
    }

    private string GetProxyDllNameForStrategy(InjectionStrategy strategy)
    {
        return strategy switch
        {
            InjectionStrategy.DxgiProxy => "dxgi.dll",
            InjectionStrategy.D3D11Proxy => "d3d11.dll",
            InjectionStrategy.VulkanProxy => "vulkan-1.dll",
            _ => throw new ArgumentException($"Not a proxy strategy: {strategy}")
        };
    }

    private string GetHookDllPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "Pick6VulkanHook.dll");
    }

    private async Task WaitForGraphicsModulesAsync(int processId, CancellationToken cancellationToken)
    {
        if (!_nativeAvailable)
        {
            // Fallback to managed implementation
            // (Would need to extract this from EnhancedInjector)
            await Task.Delay(1000, cancellationToken);
            return;
        }

        var timeout = TimeSpan.FromMilliseconds(_config.ModuleWaitTimeoutMs);
        var startTime = DateTime.UtcNow;

        var moduleNames = new[] { "vulkan", "d3d11", "dxgi" };

        while (DateTime.UtcNow - startTime < timeout && !cancellationToken.IsCancellationRequested)
        {
            foreach (var moduleName in moduleNames)
            {
                if (NativeInjector.IsProcessUsingModule((uint)processId, moduleName))
                {
                    Log.Info($"Graphics module detected: {moduleName}");
                    return;
                }
            }

            await Task.Delay(500, cancellationToken);
        }

        Log.Debug("Graphics module wait timeout reached or process inaccessible");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _processWatcher?.Dispose();
        
        if (_nativeAvailable)
        {
            NativeInjector.Cleanup();
        }
    }
}
