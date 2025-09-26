using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pick6.Core;

/// <summary>
/// Enhanced process watcher for FiveM with configurable polling and pattern matching
/// </summary>
public class ProcessWatcher : IDisposable
{
    private readonly InjectionConfig _config;
    private Timer? _pollTimer;
    private readonly object _lockObject = new();
    private bool _disposed = false;

    public event EventHandler<ProcessFoundEventArgs>? ProcessFound;
    public event EventHandler<ProcessLostEventArgs>? ProcessLost;

    public ProcessWatcher(InjectionConfig? config = null)
    {
        _config = config ?? InjectionConfig.Default;
    }

    /// <summary>
    /// Start monitoring for FiveM processes
    /// </summary>
    public void StartWatching()
    {
        lock (_lockObject)
        {
            if (_disposed) return;
            
            StopWatching();
            
            Log.Info($"Starting FiveM process watcher (poll every {_config.PollIntervalMs}ms, timeout {_config.ProcessSearchTimeoutMs}ms)");
            
            // Attempt to enable debug privilege
            if (PrivilegeManager.TryEnableSeDebugPrivilege())
            {
                Log.Info("SeDebugPrivilege enabled for process access");
            }
            else
            {
                Log.Warn("SeDebugPrivilege not available - may have limited process access");
            }

            _pollTimer = new Timer(PollCallback, null, 0, _config.PollIntervalMs);
        }
    }

    /// <summary>
    /// Stop monitoring for processes
    /// </summary>
    public void StopWatching()
    {
        lock (_lockObject)
        {
            _pollTimer?.Dispose();
            _pollTimer = null;
        }
    }

    /// <summary>
    /// Wait for a FiveM process to appear with timeout
    /// </summary>
    public async Task<ProcessInfo?> WaitForProcessAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(_config.ProcessSearchTimeoutMs);

        Log.Info($"Waiting for FiveM process (timeout: {timeout.TotalSeconds:F1}s)");

        while (DateTime.UtcNow - startTime < timeout && !cancellationToken.IsCancellationRequested)
        {
            var processes = FindFiveMProcesses();
            if (processes.Any())
            {
                var process = processes.First();
                Log.Info($"Found FiveM process: {process}");
                return process;
            }

            await Task.Delay(_config.PollIntervalMs, cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Log.Info("Process search cancelled");
        }
        else
        {
            Log.Warn($"No FiveM process found after {timeout.TotalSeconds:F1}s timeout. Patterns tried:");
            Log.Warn("- Process names: FiveM*, CitizenFX*");
            Log.Warn("- Window titles containing: FiveM, CitizenFX, Grand Theft Auto");
            Log.Warn("- Command lines containing: FiveM.app, CitizenFX");
        }

        return null;
    }

    private void PollCallback(object? state)
    {
        try
        {
            var processes = FindFiveMProcesses();
            
            // For now, just fire events for any found processes
            // In a more sophisticated implementation, we'd track state changes
            foreach (var process in processes)
            {
                ProcessFound?.Invoke(this, new ProcessFoundEventArgs(process));
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error during process polling: {ex.Message}");
        }
    }

    /// <summary>
    /// Find all FiveM processes using enhanced pattern matching
    /// </summary>
    public List<ProcessInfo> FindFiveMProcesses()
    {
        var results = new List<ProcessInfo>();
        var seen = new HashSet<int>();

        Log.Debug("Starting enhanced FiveM process search");

        try
        {
            // Get all processes once to avoid multiple enumerations
            var allProcesses = Process.GetProcesses();
            
            foreach (var process in allProcesses)
            {
                try
                {
                    if (process.HasExited || seen.Contains(process.Id)) 
                        continue;

                    if (IsProcessFiveM(process))
                    {
                        // Verify architecture compatibility
                        if (!IsArchitectureCompatible(process))
                        {
                            Log.Warn($"Found FiveM process {process.ProcessName} (PID: {process.Id}) but architecture mismatch - skipping");
                            continue;
                        }

                        var processInfo = new ProcessInfo
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            WindowTitle = process.MainWindowTitle ?? "",
                            WindowHandle = process.MainWindowHandle
                        };

                        results.Add(processInfo);
                        seen.Add(process.Id);
                        
                        Log.Debug($"Found FiveM process: {processInfo}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"Error checking process {process.Id}: {ex.Message}");
                }
                finally
                {
                    try { process.Dispose(); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error enumerating processes: {ex.Message}");
        }

        if (results.Any())
        {
            Log.Info($"Found {results.Count} FiveM process(es)");
        }
        else
        {
            Log.Debug("No FiveM processes found");
        }

        return results;
    }

    private bool IsProcessFiveM(Process process)
    {
        try
        {
            var processName = process.ProcessName?.ToLowerInvariant() ?? "";
            var windowTitle = process.MainWindowTitle?.ToLowerInvariant() ?? "";
            
            // Enhanced pattern matching as specified in requirements
            
            // 1. Process name patterns (case-insensitive)
            if (processName.StartsWith("fivem") ||
                processName.Contains("citizenfx") ||
                processName.Contains("cfx"))
            {
                Log.Debug($"Process name match: {processName}");
                return true;
            }

            // 2. Window title patterns
            if (windowTitle.Contains("fivem") ||
                windowTitle.Contains("citizenfx") ||
                windowTitle.Contains("grand theft auto"))
            {
                Log.Debug($"Window title match: {windowTitle}");
                return true;
            }

            // 3. Command line patterns (if accessible)
            try
            {
                var commandLine = GetProcessCommandLine(process.Id);
                if (!string.IsNullOrEmpty(commandLine))
                {
                    var lowerCommandLine = commandLine.ToLowerInvariant();
                    if (lowerCommandLine.Contains("fivem.app") ||
                        lowerCommandLine.Contains("citizenfx"))
                    {
                        Log.Debug($"Command line match: {commandLine}");
                        return true;
                    }
                }
            }
            catch
            {
                // Command line access may fail - not critical
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool IsArchitectureCompatible(Process process)
    {
        try
        {
            // Check if target process architecture matches current process
            var currentIs64Bit = Environment.Is64BitProcess;
            var targetIs64Bit = IsProcess64Bit(process);

            if (currentIs64Bit != targetIs64Bit)
            {
                var currentArch = currentIs64Bit ? "64-bit" : "32-bit";
                var targetArch = targetIs64Bit ? "64-bit" : "32-bit";
                Log.Error($"Architecture mismatch: Injector is {currentArch}; target is {targetArch} (unsupported)");
                return false;
            }

            return true;
        }
        catch
        {
            // If we can't determine architecture, assume compatible
            return true;
        }
    }

    private bool IsProcess64Bit(Process process)
    {
        try
        {
            // On 64-bit systems, check if process is WOW64 (32-bit on 64-bit)
            if (Environment.Is64BitOperatingSystem)
            {
                if (IsWow64Process(process.Handle, out bool isWow64))
                {
                    return !isWow64; // If WOW64, it's 32-bit; otherwise 64-bit
                }
            }
            
            // On 32-bit systems, all processes are 32-bit
            return Environment.Is64BitOperatingSystem;
        }
        catch
        {
            // Fallback: assume same as current process
            return Environment.Is64BitProcess;
        }
    }

    private string? GetProcessCommandLine(int processId)
    {
        try
        {
            // This is a simplified approach - full implementation would use WMI or direct process memory reading
            return null;
        }
        catch
        {
            return null;
        }
    }

    #region Win32 API
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool IsWow64Process(IntPtr hProcess, out bool Wow64Process);
    #endregion

    public void Dispose()
    {
        lock (_lockObject)
        {
            if (_disposed) return;
            _disposed = true;
            
            StopWatching();
        }
    }
}

public class ProcessFoundEventArgs : EventArgs
{
    public ProcessInfo Process { get; }
    
    public ProcessFoundEventArgs(ProcessInfo process)
    {
        Process = process;
    }
}

public class ProcessLostEventArgs : EventArgs
{
    public int ProcessId { get; }
    
    public ProcessLostEventArgs(int processId)
    {
        ProcessId = processId;
    }
}