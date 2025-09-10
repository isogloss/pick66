using System.Diagnostics;

namespace Pick6.Core;

/// <summary>
/// Utilities for detecting and managing FiveM processes
/// Enhanced with Vulkan detection capabilities
/// </summary>
public static class FiveMDetector
{
    private static readonly string[] FIVEM_PROCESS_NAMES = {
        "FiveM",
        "FiveM_b2060",
        "FiveM_b2189", 
        "FiveM_b2372",
        "FiveM_b2545",
        "FiveM_b2612",
        "FiveM_b2699",
        "FiveM_b2802",
        "FiveM_b2944",
        "CitizenFX"
    };

    /// <summary>
    /// Find all running FiveM processes (traditional method)
    /// </summary>
    public static List<ProcessInfo> FindFiveMProcesses()
    {
        var processes = new List<ProcessInfo>();

        foreach (var processName in FIVEM_PROCESS_NAMES)
        {
            try
            {
                var found = Process.GetProcessesByName(processName);
                foreach (var process in found)
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        processes.Add(new ProcessInfo
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            WindowTitle = process.MainWindowTitle,
                            WindowHandle = process.MainWindowHandle
                        });
                    }
                    process.Dispose();
                }
            }
            catch
            {
                // Process might have exited, continue
            }
        }

        return processes;
    }

    /// <summary>
    /// Find FiveM processes using Vulkan (enhanced method)
    /// </summary>
    public static List<VulkanProcessInfo> FindVulkanFiveMProcesses()
    {
        return VulkanInjector.FindVulkanProcesses();
    }

    /// <summary>
    /// Check if any FiveM process is currently running
    /// Checks both traditional and Vulkan processes
    /// </summary>
    public static bool IsFiveMRunning()
    {
        return FindFiveMProcesses().Any() || FindVulkanFiveMProcesses().Any();
    }

    /// <summary>
    /// Get the primary FiveM process (prioritizes Vulkan processes)
    /// </summary>
    public static ProcessInfo? GetPrimaryFiveMProcess()
    {
        // First check for Vulkan processes
        var vulkanProcesses = FindVulkanFiveMProcesses();
        if (vulkanProcesses.Any())
        {
            var vulkanProcess = vulkanProcesses.First();
            return new ProcessInfo
            {
                ProcessId = vulkanProcess.ProcessId,
                ProcessName = vulkanProcess.ProcessName,
                WindowTitle = vulkanProcess.WindowTitle,
                WindowHandle = vulkanProcess.WindowHandle
            };
        }

        // Fall back to traditional process detection
        return FindFiveMProcesses().FirstOrDefault();
    }

    /// <summary>
    /// Get comprehensive process information including Vulkan support
    /// </summary>
    public static FiveMProcessSummary GetProcessSummary()
    {
        var traditionalProcesses = FindFiveMProcesses();
        var vulkanProcesses = FindVulkanFiveMProcesses();

        return new FiveMProcessSummary
        {
            TraditionalProcesses = traditionalProcesses,
            VulkanProcesses = vulkanProcesses,
            TotalProcessCount = traditionalProcesses.Count + vulkanProcesses.Count,
            HasVulkanSupport = vulkanProcesses.Any()
        };
    }

    /// <summary>
    /// Monitor for FiveM process changes (enhanced with Vulkan detection)
    /// </summary>
    public static void StartProcessMonitoring(Action<FiveMProcessSummary> onProcessesChanged)
    {
        var timer = new System.Timers.Timer(2000); // Check every 2 seconds
        timer.Elapsed += (s, e) =>
        {
            var summary = GetProcessSummary();
            onProcessesChanged(summary);
        };
        timer.Start();
    }
}

/// <summary>
/// Information about a detected process
/// </summary>
public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public IntPtr WindowHandle { get; set; }

    public override string ToString()
    {
        return $"{ProcessName} - {WindowTitle} (PID: {ProcessId})";
    }
}

/// <summary>
/// Comprehensive summary of FiveM processes
/// </summary>
public class FiveMProcessSummary
{
    public List<ProcessInfo> TraditionalProcesses { get; set; } = new();
    public List<VulkanProcessInfo> VulkanProcesses { get; set; } = new();
    public int TotalProcessCount { get; set; }
    public bool HasVulkanSupport { get; set; }

    public override string ToString()
    {
        return $"FiveM Processes: {TotalProcessCount} (Vulkan: {VulkanProcesses.Count}, Traditional: {TraditionalProcesses.Count})";
    }
}