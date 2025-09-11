using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Pick6.Core;

/// <summary>
/// Monitor information helper that works without Windows Forms dependency
/// </summary>
public static class MonitorHelper
{
    public class MonitorInfo
    {
        public int Index { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsPrimary { get; set; }
        public string DisplayName { get; set; } = "";

        public override string ToString()
        {
            var primary = IsPrimary ? " (Primary)" : "";
            return $"{Index}: Monitor {Index + 1} ({Bounds.Width}x{Bounds.Height}){primary}";
        }
    }

    /// <summary>
    /// Get all available monitors
    /// </summary>
    public static List<MonitorInfo> GetAllMonitors()
    {
        return GetWindowsMonitors();
    }

    private static List<MonitorInfo> GetWindowsMonitors()
    {
        var monitors = new List<MonitorInfo>();
        int index = 0;

        try
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                var info = new MONITORINFO();
                info.cbSize = Marshal.SizeOf(info);
                
                if (GetMonitorInfo(hMonitor, ref info))
                {
                    var bounds = new Rectangle(
                        info.rcMonitor.Left, 
                        info.rcMonitor.Top,
                        info.rcMonitor.Right - info.rcMonitor.Left,
                        info.rcMonitor.Bottom - info.rcMonitor.Top
                    );

                    var isPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;
                    
                    monitors.Add(new MonitorInfo
                    {
                        Index = index++,
                        Bounds = bounds,
                        IsPrimary = isPrimary,
                        DisplayName = $"Monitor {index} - {bounds.Width}x{bounds.Height}" + (isPrimary ? " (Primary)" : "")
                    });
                }
                
                return true;
            }, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not enumerate monitors: {ex.Message}");
            // Fallback to single monitor
            monitors.Add(new MonitorInfo 
            { 
                Index = 0, 
                Bounds = new Rectangle(0, 0, 1920, 1080), 
                IsPrimary = true,
                DisplayName = "Primary Monitor (1920x1080)"
            });
        }

        return monitors;
    }

    /// <summary>
    /// Get monitor bounds by index
    /// </summary>
    public static Rectangle GetMonitorBounds(int monitorIndex)
    {
        var monitors = GetAllMonitors();
        if (monitorIndex >= 0 && monitorIndex < monitors.Count)
        {
            return monitors[monitorIndex].Bounds;
        }
        
        // Return primary monitor or default
        return monitors.FirstOrDefault()?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
    }

    #region Win32 API
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITORINFOF_PRIMARY = 0x00000001;
    #endregion
}