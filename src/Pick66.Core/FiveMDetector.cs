using System.Diagnostics;

namespace Pick66.Core;

/// <summary>
/// Utilities for detecting and managing FiveM processes
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
    /// Find all running FiveM processes
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
    /// Check if any FiveM process is currently running
    /// </summary>
    public static bool IsFiveMRunning()
    {
        return FindFiveMProcesses().Any();
    }

    /// <summary>
    /// Get the primary FiveM process (first one found with a window)
    /// </summary>
    public static ProcessInfo? GetPrimaryFiveMProcess()
    {
        return FindFiveMProcesses().FirstOrDefault();
    }

    /// <summary>
    /// Monitor for FiveM process changes
    /// </summary>
    public static void StartProcessMonitoring(Action<List<ProcessInfo>> onProcessesChanged)
    {
        var timer = new System.Timers.Timer(2000); // Check every 2 seconds
        timer.Elapsed += (s, e) =>
        {
            var processes = FindFiveMProcesses();
            onProcessesChanged(processes);
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