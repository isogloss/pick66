using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Pick6.Core.Timing;
using Pick6.Core.Diagnostics;

namespace Pick6.Core;

/// <summary>
/// Core capture engine for game window capture
/// Now supports both Vulkan injection and GDI capture methods
/// </summary>
public class GameCaptureEngine
{
    private IntPtr _targetWindow = IntPtr.Zero;
    private bool _isCapturing = false;
    private Thread? _captureThread;
    private readonly object _lockObject = new();
    private VulkanFrameCapture? _vulkanCapture;
    private bool _useVulkanCapture = true;
    private readonly FramePacer _framePacer = new();
    private readonly FrameStatistics _statistics = new();

    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Capture settings
    /// </summary>
    public CaptureSettings Settings { get; set; } = new();

    /// <summary>
    /// Frame timing and performance statistics
    /// </summary>
    public FrameStatistics Statistics => _statistics;

    /// <summary>
    /// Start capturing frames from the target process
    /// Tries Vulkan injection first, falls back to GDI capture
    /// </summary>
    public bool StartCapture(string processName)
    {
        lock (_lockObject)
        {
            if (_isCapturing) return false;

            // First try Vulkan injection approach
            if (_useVulkanCapture && TryStartVulkanCapture(processName))
            {
                return true;
            }

            // Fall back to GDI window capture
            return StartGdiCapture(processName);
            return false;
        }
    }

    /// <summary>
    /// Stop capturing frames
    /// </summary>
    public void StopCapture()
    {
        lock (_lockObject)
        {
            _isCapturing = false;
            _captureThread?.Join(1000);

            _vulkanCapture?.StopCapture();
            _vulkanCapture = null;
        }
    }

    private bool TryStartVulkanCapture(string processName)
    {
        try
        {
            // Find Vulkan processes
            var vulkanProcesses = VulkanInjector.FindVulkanProcesses();
            var targetProcess = vulkanProcesses.FirstOrDefault(p => 
                p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

            if (targetProcess == null)
            {
                ErrorOccurred?.Invoke(this, $"No Vulkan process found for: {processName}");
                return false;
            }

            _vulkanCapture = new VulkanFrameCapture();
            _vulkanCapture.Settings = Settings;
            
            // Forward events
            _vulkanCapture.FrameCaptured += (s, e) => FrameCaptured?.Invoke(this, e);
            _vulkanCapture.ErrorOccurred += (s, msg) => ErrorOccurred?.Invoke(this, msg);

            if (_vulkanCapture.StartCapture(targetProcess.ProcessId))
            {
                _isCapturing = true;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Vulkan capture failed: {ex.Message}");
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private bool StartGdiCapture(string processName)
    {
        _targetWindow = FindGameWindow(processName);
        if (_targetWindow == IntPtr.Zero)
        {
            ErrorOccurred?.Invoke(this, $"Could not find window for process: {processName}");
            return false;
        }

        _isCapturing = true;
        _captureThread = new Thread(GdiCaptureLoop) { IsBackground = true };
        _captureThread.Start();
        return true;
    }

    private IntPtr FindGameWindow(string processName)
    {
        var processes = System.Diagnostics.Process.GetProcessesByName(processName);
        foreach (var process in processes)
        {
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }
        }
        return IntPtr.Zero;
    }

    [SupportedOSPlatform("windows")]
    private void GdiCaptureLoop()
    {
        // Initialize frame pacing
        _framePacer.Reset(Settings.TargetFPS, PacingMode.HybridSpin);
        _statistics.Reset();
        
        // Enable diagnostics logging if requested
        var enableDiagnostics = Environment.GetEnvironmentVariable("PICK6_DIAG") == "1";

        while (_isCapturing)
        {
            try
            {
                var frame = CaptureFrame(_targetWindow);
                if (frame != null)
                {
                    FrameCaptured?.Invoke(this, new FrameCapturedEventArgs(frame));
                }

                // Wait for next frame and get timing statistics
                var frameElapsed = _framePacer.WaitNextFrame();
                _statistics.RecordFrame(frameElapsed.ElapsedMs, frameElapsed.TargetIntervalMs);

                // Optional diagnostic logging
                if (enableDiagnostics && _statistics.TotalFrames % 60 == 0) // Log every ~1 second at 60fps
                {
                    Console.WriteLine($"[GDI Capture] {_statistics.GetSummary()}");
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"GDI Capture error: {ex.Message}");
                Thread.Sleep(100); // Brief pause on error
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private Bitmap? CaptureFrame(IntPtr windowHandle)
    {
        if (!IsWindow(windowHandle)) return null;

        var rect = new RECT();
        if (!GetWindowRect(windowHandle, ref rect)) return null;

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0) return null;

        // Apply scaling if configured
        if (Settings.ScaleWidth > 0 && Settings.ScaleHeight > 0)
        {
            width = Settings.ScaleWidth;
            height = Settings.ScaleHeight;
        }

        var bitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            var hdcBitmap = graphics.GetHdc();
            var hdcWindow = GetWindowDC(windowHandle);

            BitBlt(hdcBitmap, 0, 0, width, height, hdcWindow, 0, 0, SRCCOPY);

            graphics.ReleaseHdc(hdcBitmap);
            ReleaseDC(windowHandle, hdcWindow);
        }

        return bitmap;
    }

    #region Win32 API
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight,
        IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    private const int SRCCOPY = 0x00CC0020;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    #endregion
}

/// <summary>
/// Event args for frame captured events
/// </summary>
public class FrameCapturedEventArgs : EventArgs
{
    public Bitmap Frame { get; }

    public FrameCapturedEventArgs(Bitmap frame)
    {
        Frame = frame;
    }
}

/// <summary>
/// Capture configuration settings
/// </summary>
public class CaptureSettings
{
    public int TargetFPS { get; set; } = 60;
    public int ScaleWidth { get; set; } = 0; // 0 = use original
    public int ScaleHeight { get; set; } = 0; // 0 = use original
    public bool UseHardwareAcceleration { get; set; } = true;
}
