using System.Diagnostics;

namespace Pick6.Core;

/// <summary>
/// Utilities for detecting and managing FiveM processes
/// Enhanced with Vulkan detection capabilities
/// </summary>
public static class FiveMDetector
{
    // Expanded explicit names to catch common variants; wildcard scan below is the main safety net
    private static readonly string[] FIVEM_PROCESS_NAMES = {
        "FiveM",
        "FiveM_GTAProcess",
        "CitizenFX",
        // Known build variants (with and without GTAProcess suffix)
        "FiveM_b2060","FiveM_b2060_GTAProcess",
        "FiveM_b2189","FiveM_b2189_GTAProcess",
        "FiveM_b2372","FiveM_b2372_GTAProcess",
        "FiveM_b2545","FiveM_b2545_GTAProcess",
        "FiveM_b2612","FiveM_b2612_GTAProcess",
        "FiveM_b2699","FiveM_b2699_GTAProcess",
        "FiveM_b2802","FiveM_b2802_GTAProcess",
        "FiveM_b2944","FiveM_b2944_GTAProcess"
    };

    /// <summary>
    /// Find all running FiveM processes (EXTREMELY broad):
    /// - Matches explicit known names
    /// - Wildcard-like scan of all processes by name/title tokens
    /// - Returns only processes with a visible main window
    /// </summary>
    public static List<ProcessInfo> FindFiveMProcesses()
    {
        var results = new List<ProcessInfo>();
        var seen = new HashSet<int>();

        // 1) Explicit name matches (existing behavior + expanded list)
        foreach (var name in FIVEM_PROCESS_NAMES)
        {
            TryAddByProcessName(name, results, seen);
        }

        // 2) Wildcard-like scan: extremely broad matching by name/title tokens
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (p.HasExited) continue;
                if (p.MainWindowHandle == IntPtr.Zero) continue; // keep capture semantics

                if (MatchesFiveM(p) && seen.Add(p.Id))
                {
                    results.Add(new ProcessInfo
                    {
                        ProcessId = p.Id,
                        ProcessName = p.ProcessName,
                        WindowTitle = p.MainWindowTitle,
                        WindowHandle = p.MainWindowHandle
                    });
                }
            }
            catch
            {
                // Process inaccessible or exited; ignore
            }
            finally
            {
                try { p.Dispose(); } catch { }
            }
        }

        return results;
    }

    private static void TryAddByProcessName(string processName, List<ProcessInfo> results, HashSet<int> seen)
    {
        try
        {
            var found = Process.GetProcessesByName(processName);
            foreach (var process in found)
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero && seen.Add(process.Id))
                    {
                        results.Add(new ProcessInfo
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            WindowTitle = process.MainWindowTitle,
                            WindowHandle = process.MainWindowHandle
                        });
                    }
                }
                catch { }
                finally { try { process.Dispose(); } catch { } }
            }
        }
        catch { }
    }

    private static bool MatchesFiveM(Process p)
    {
        var name = string.Empty;
        var title = string.Empty;
        try { name = p.ProcessName ?? string.Empty; } catch { }
        try { title = p.MainWindowTitle ?? string.Empty; } catch { }

        var nl = name.ToLowerInvariant();
        var tl = title.ToLowerInvariant();

        // Extremely broad token set by request
        bool nameHit =
            nl.Contains("fivem") ||
            nl.Contains("citizenfx") ||
            nl.Contains("cfx") ||
            nl.Contains("gtaprocess") ||
            nl.Contains("gta5") ||
            nl.Contains("gta_") ||
            nl.Contains("gta");

        bool titleHit =
            tl.Contains("fivem") ||
            tl.Contains("citizenfx") ||
            tl.Contains("grand theft auto") ||
            tl.Contains("gta");

        return nameHit || titleHit;
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