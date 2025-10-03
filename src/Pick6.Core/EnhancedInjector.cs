using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pick6.Core;

/// <summary>
/// Enhanced multi-strategy injector with fallback mechanisms
/// </summary>
public class EnhancedInjector : IDisposable
{
    private readonly InjectionConfig _config;
    private readonly VulkanInjector _vulkanInjector;
    private readonly ProcessWatcher _processWatcher;
    private bool _disposed = false;

    public EnhancedInjector(InjectionConfig? config = null)
    {
        _config = config ?? InjectionConfig.Default;
        _vulkanInjector = new VulkanInjector();
        _processWatcher = new ProcessWatcher(_config);
    }

    /// <summary>
    /// Find and inject into FiveM process with fallback strategies
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

            // Attempt injection with fallback strategies
            return await AttemptInjectionWithFallbackAsync(process, cancellationToken);
        }
        finally
        {
            _processWatcher.StopWatching();
        }
    }

    /// <summary>
    /// Inject into a specific process with fallback strategies
    /// </summary>
    public async Task<InjectionResult> InjectIntoProcessAsync(ProcessInfo process, CancellationToken cancellationToken = default)
    {
        Log.Info($"Attempting injection into process: {process}");
        return await AttemptInjectionWithFallbackAsync(process, cancellationToken);
    }

    private async Task<InjectionResult> AttemptInjectionWithFallbackAsync(ProcessInfo process, CancellationToken cancellationToken)
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
                var result = await AttemptSingleStrategyAsync(process, strategy, cancellationToken);
                results.Add(result);

                if (result.Success)
                {
                    Log.Info($"✅ Injection successful using {strategy} method: {result.Message}");
                    return result;
                }
                else
                {
                    Log.Warn($"❌ {strategy} injection failed: {result.Message}");
                    
                    // Record failure information
                    failureInfos.Add(new StrategyFailureInfo
                    {
                        Strategy = strategy,
                        ErrorMessage = result.Message,
                        Exception = result.Exception
                    });
                    
                    // If not the last strategy, log fallback message
                    if (strategy != _config.InjectionOrder.Last())
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

            // Small delay between attempts
            await Task.Delay(500, cancellationToken);
        }

        // All strategies failed - create comprehensive error message
        var lastResult = results.LastOrDefault() ?? InjectionResult.Failed(InjectionStrategy.Direct, "No injection strategies attempted");
        var comprehensiveMessage = "All injection strategies failed. Please check:\n" +
                                   "  1. Run the application as Administrator\n" +
                                   "  2. Disable antivirus/security software temporarily\n" +
                                   "  3. Ensure Pick6VulkanHook.dll and proxy DLLs are present\n" +
                                   "  4. Check that the game directory is writable";
        
        Log.Error(comprehensiveMessage);
        
        // Return result with detailed failure information
        return InjectionResult.FailedWithDetails(lastResult.Strategy, comprehensiveMessage, failureInfos);
    }

    private async Task<InjectionResult> AttemptSingleStrategyAsync(ProcessInfo process, InjectionStrategy strategy, CancellationToken cancellationToken)
    {
        switch (strategy)
        {
            case InjectionStrategy.Direct:
                return await AttemptDirectInjectionAsync(process, cancellationToken);

            case InjectionStrategy.DxgiProxy:
                return await AttemptProxyInjectionAsync(process, "dxgi.dll", cancellationToken);

            case InjectionStrategy.D3D11Proxy:
                return await AttemptProxyInjectionAsync(process, "d3d11.dll", cancellationToken);

            case InjectionStrategy.VulkanProxy:
                return await AttemptProxyInjectionAsync(process, "vulkan-1.dll", cancellationToken);

            default:
                return InjectionResult.Failed(strategy, $"Unknown injection strategy: {strategy}");
        }
    }

    private async Task<InjectionResult> AttemptDirectInjectionAsync(ProcessInfo process, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Make method async-compatible

        try
        {
            // Use existing VulkanInjector for direct injection
            var success = _vulkanInjector.InjectIntoProcess(process.ProcessId);
            
            if (success)
            {
                return InjectionResult.Succeeded(InjectionStrategy.Direct, "Direct LoadLibrary injection completed");
            }
            else
            {
                return InjectionResult.Failed(InjectionStrategy.Direct, "Direct LoadLibrary injection failed - check DLL path and process access");
            }
        }
        catch (Exception ex)
        {
            return InjectionResult.Failed(InjectionStrategy.Direct, $"Direct injection exception: {ex.Message}", ex);
        }
    }

    private async Task<InjectionResult> AttemptProxyInjectionAsync(ProcessInfo process, string proxyDllName, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Make method async-compatible

        string? targetProxyPath = null;
        string? originalBackupPath = null;
        bool needsCleanup = false;

        try
        {
            // Get the target process directory
            var processPath = GetProcessMainModuleFilePath(process.ProcessId);
            if (string.IsNullOrEmpty(processPath))
            {
                return InjectionResult.Failed(GetStrategyForProxyDll(proxyDllName), 
                    $"Could not determine process directory for {proxyDllName} proxy");
            }

            var processDir = Path.GetDirectoryName(processPath);
            if (string.IsNullOrEmpty(processDir))
            {
                return InjectionResult.Failed(GetStrategyForProxyDll(proxyDllName), 
                    $"Invalid process directory for {proxyDllName} proxy");
            }

            Log.Verbose($"Target process directory: {processDir}");

            // Check if directory is writable
            if (!IsDirectoryWritable(processDir))
            {
                return InjectionResult.Failed(GetStrategyForProxyDll(proxyDllName), 
                    $"Process directory is not writable: {processDir}. Please run as Administrator or check permissions.");
            }

            targetProxyPath = Path.Combine(processDir, proxyDllName);
            originalBackupPath = Path.Combine(processDir, $"{Path.GetFileNameWithoutExtension(proxyDllName)}.original.dll");

            // Check if proxy deployment is needed
            if (!IsProxyDeploymentNeeded(targetProxyPath))
            {
                Log.Info($"Proxy {proxyDllName} already deployed and current");
                return InjectionResult.Succeeded(GetStrategyForProxyDll(proxyDllName), 
                    $"Proxy {proxyDllName} already active");
            }

            // Deploy proxy DLL
            Log.Verbose($"Starting proxy deployment for {proxyDllName}");
            needsCleanup = true;  // Enable cleanup on failure from this point
            
            var deployResult = await DeployProxyDllAsync(proxyDllName, targetProxyPath, originalBackupPath, cancellationToken);
            if (!deployResult.Success)
            {
                // Cleanup on failure
                await CleanupFailedProxyDeploymentAsync(targetProxyPath, originalBackupPath);
                return deployResult;
            }

            Log.Info($"Proxy {proxyDllName} deployed successfully - will be loaded when process loads the graphics module");
            return InjectionResult.Succeeded(GetStrategyForProxyDll(proxyDllName), 
                $"Proxy {proxyDllName} deployed - waiting for module load");
        }
        catch (Exception ex)
        {
            // Cleanup on exception
            if (needsCleanup && targetProxyPath != null && originalBackupPath != null)
            {
                await CleanupFailedProxyDeploymentAsync(targetProxyPath, originalBackupPath);
            }
            
            return InjectionResult.Failed(GetStrategyForProxyDll(proxyDllName), 
                $"Proxy {proxyDllName} deployment failed: {ex.Message}", ex);
        }
    }

    private async Task<InjectionResult> DeployProxyDllAsync(string proxyDllName, string targetPath, string backupPath, CancellationToken cancellationToken)
    {
        await Task.Yield();

        try
        {
            // Get our proxy DLL path
            var ourProxyPath = GetProxyDllPath(proxyDllName);
            if (!File.Exists(ourProxyPath))
            {
                return InjectionResult.Failed(GetStrategyForProxyDll(proxyDllName), 
                    $"Proxy DLL not found: {ourProxyPath}");
            }
            
            Log.Verbose($"Source proxy DLL: {ourProxyPath}");
            
            // Create backup of original if it exists and backup doesn't exist yet
            if (File.Exists(targetPath) && !File.Exists(backupPath))
            {
                Log.Info($"Creating backup: {Path.GetFileName(targetPath)} -> {Path.GetFileName(backupPath)}");
                Log.Verbose($"Backup path: {backupPath}");
                
                try
                {
                    File.Copy(targetPath, backupPath, false);
                    Log.Verbose("Backup created successfully");
                }
                catch (Exception ex)
                {
                    return InjectionResult.Failed(GetStrategyForProxyDll(proxyDllName), 
                        $"Failed to create backup of original {proxyDllName}: {ex.Message}", ex);
                }
            }
            else if (File.Exists(backupPath))
            {
                Log.Verbose($"Backup already exists: {backupPath}");
            }

            // Copy our proxy DLL to target location
            Log.Info($"Deploying proxy DLL: {Path.GetFileName(ourProxyPath)} -> {targetPath}");
            
            try
            {
                File.Copy(ourProxyPath, targetPath, true);
                Log.Verbose("Proxy DLL copied successfully");
            }
            catch (Exception ex)
            {
                return InjectionResult.Failed(GetStrategyForProxyDll(proxyDllName), 
                    $"Failed to copy proxy DLL to target location: {ex.Message}", ex);
            }

            return InjectionResult.Succeeded(GetStrategyForProxyDll(proxyDllName), 
                $"Proxy {proxyDllName} deployed successfully");
        }
        catch (Exception ex)
        {
            return InjectionResult.Failed(GetStrategyForProxyDll(proxyDllName), 
                $"Failed to deploy proxy {proxyDllName}: {ex.Message}", ex);
        }
    }

    private bool IsProxyDeploymentNeeded(string targetProxyPath)
    {
        try
        {
            if (!File.Exists(targetProxyPath))
            {
                return true; // Need to deploy
            }

            // Simple check: if file exists, assume it's our proxy (could be enhanced with version checking)
            var ourProxyPath = GetProxyDllPath(Path.GetFileName(targetProxyPath));
            if (!File.Exists(ourProxyPath))
            {
                Log.Warn($"Our proxy DLL not found: {ourProxyPath}");
                return false;
            }

            // Compare file sizes as a simple check
            var targetInfo = new FileInfo(targetProxyPath);
            var ourInfo = new FileInfo(ourProxyPath);
            
            return targetInfo.Length != ourInfo.Length;
        }
        catch
        {
            return true; // If in doubt, redeploy
        }
    }

    private string GetProxyDllPath(string proxyDllName)
    {
        // Check for proxy DLLs in multiple locations
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        
        // Try exact filename first (pre-built)
        var exactPath = Path.Combine(baseDir, proxyDllName);
        if (File.Exists(exactPath))
        {
            return exactPath;
        }
        
        // Try with Pick6 prefix naming convention
        var prefixedName = $"Pick6{Path.GetFileNameWithoutExtension(proxyDllName)}Proxy.dll";
        var prefixedPath = Path.Combine(baseDir, prefixedName);
        if (File.Exists(prefixedPath))
        {
            return prefixedPath;
        }
        
        // Try in proxy subdirectory
        var proxyDir = Path.Combine(baseDir, "proxies");
        if (Directory.Exists(proxyDir))
        {
            var proxySubPath = Path.Combine(proxyDir, proxyDllName);
            if (File.Exists(proxySubPath))
            {
                return proxySubPath;
            }
        }
        
        // Return exact path as fallback (even if doesn't exist, for error message clarity)
        return exactPath;
    }

    private InjectionStrategy GetStrategyForProxyDll(string proxyDllName)
    {
        return proxyDllName.ToLower() switch
        {
            "dxgi.dll" => InjectionStrategy.DxgiProxy,
            "d3d11.dll" => InjectionStrategy.D3D11Proxy,
            "vulkan-1.dll" => InjectionStrategy.VulkanProxy,
            _ => InjectionStrategy.Direct
        };
    }

    private string? GetProcessMainModuleFilePath(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }
    
    private string GetHookDllPath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "Pick6VulkanHook.dll");
    }
    
    private bool IsDirectoryWritable(string directoryPath)
    {
        try
        {
            // Try to create a temporary file to test write access
            var testFile = Path.Combine(directoryPath, $".pick6_write_test_{Guid.NewGuid()}.tmp");
            
            try
            {
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }
    
    private async Task CleanupFailedProxyDeploymentAsync(string targetProxyPath, string originalBackupPath)
    {
        await Task.Yield();
        
        try
        {
            Log.Verbose("Cleaning up failed proxy deployment");
            
            // If we created a new proxy file, try to remove it
            if (File.Exists(targetProxyPath))
            {
                try
                {
                    // Restore original if backup exists
                    if (File.Exists(originalBackupPath))
                    {
                        Log.Verbose($"Restoring original file from backup: {originalBackupPath}");
                        File.Copy(originalBackupPath, targetProxyPath, true);
                        File.Delete(originalBackupPath);
                        Log.Verbose("Original file restored successfully");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to restore original file during cleanup: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Error during proxy deployment cleanup: {ex.Message}");
        }
    }

    private async Task WaitForGraphicsModulesAsync(int processId, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMilliseconds(_config.ModuleWaitTimeoutMs);
        var startTime = DateTime.UtcNow;

        var moduleNames = new[] { "dxgi.dll", "d3d11.dll", "vulkan-1.dll" };

        while (DateTime.UtcNow - startTime < timeout && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                
                foreach (ProcessModule module in process.Modules)
                {
                    var moduleName = Path.GetFileName(module.ModuleName ?? "").ToLower();
                    if (moduleNames.Contains(moduleName))
                    {
                        Log.Info($"Graphics module detected: {moduleName}");
                        return;
                    }
                }
            }
            catch
            {
                // Process may have exited or be inaccessible
                break;
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
        _vulkanInjector?.RemoveInjection();
    }
}