using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Pick6.Core.Timing;
using Pick6.Core.Diagnostics;

namespace Pick6.Core;

/// <summary>
/// Interface for capture backends (GDI, DXGI, etc.)
/// Supports OBS-style game capture functionality for FiveM
/// </summary>
public interface ICaptureBackend
{
    /// <summary>
    /// Event fired when a frame is captured
    /// </summary>
    event EventHandler<FrameCapturedEventArgs>? FrameCaptured;
    
    /// <summary>
    /// Event fired when an error occurs
    /// </summary>
    event EventHandler<string>? ErrorOccurred;
    
    /// <summary>
    /// Capture settings
    /// </summary>
    CaptureSettings Settings { get; set; }

    /// <summary>
    /// Frame timing and performance statistics
    /// </summary>
    FrameStatistics Statistics { get; }
    
    /// <summary>
    /// Start capturing frames from the specified FiveM process
    /// </summary>
    /// <param name="processName">Name of the FiveM process to capture</param>
    /// <returns>True if capture started successfully</returns>
    bool StartCapture(string processName);
    
    /// <summary>
    /// Stop capturing frames
    /// </summary>
    void StopCapture();
    
    /// <summary>
    /// Get the backend type name
    /// </summary>
    string BackendName { get; }
    
    /// <summary>
    /// Check if this backend is available on the current system
    /// </summary>
    bool IsAvailable { get; }
}

/// <summary>
/// GDI-based capture backend implementation
/// Fallback for FiveM capture when DXGI is not available
/// </summary>
public class GdiCaptureBackend : ICaptureBackend
{
    private IntPtr _targetWindow = IntPtr.Zero;
    private bool _isCapturing = false;
    private Thread? _captureThread;
    private readonly object _lockObject = new();
    private readonly FramePacer _framePacer = new();
    private readonly FrameStatistics _statistics = new();
    
    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;
    public event EventHandler<string>? ErrorOccurred;
    
    public CaptureSettings Settings { get; set; } = new();
    public FrameStatistics Statistics => _statistics;
    public string BackendName => "GDI";
    public bool IsAvailable => OperatingSystem.IsWindows();
    
    public bool StartCapture(string processName)
    {
        lock (_lockObject)
        {
            if (_isCapturing) return false;

            // Only support FiveM processes
            if (!IsFiveMProcess(processName))
            {
                ErrorOccurred?.Invoke(this, $"GDI capture only supports FiveM processes, got: {processName}");
                return false;
            }

            if (OperatingSystem.IsWindows())
            {
                return StartGdiCapture(processName);
            }
            
            ErrorOccurred?.Invoke(this, "GDI capture is only supported on Windows");
            return false;
        }
    }
    
    public void StopCapture()
    {
        lock (_lockObject)
        {
            _isCapturing = false;
            _captureThread?.Join(1000);
        }
    }

    [SupportedOSPlatform("windows")]
    private bool StartGdiCapture(string processName)
    {
        _targetWindow = FindFiveMWindow(processName);
        if (_targetWindow == IntPtr.Zero)
        {
            ErrorOccurred?.Invoke(this, $"Could not find FiveM window for process: {processName}");
            return false;
        }

        _isCapturing = true;
        _captureThread = new Thread(GdiCaptureLoop) { IsBackground = true };
        _captureThread.Start();
        return true;
    }

    /// <summary>
    /// Find FiveM window handle using enhanced detection
    /// </summary>
    private IntPtr FindFiveMWindow(string processName)
    {
        // Use FiveM detector to find processes
        var fiveMProcesses = FiveMDetector.FindFiveMProcesses();
        
        foreach (var processInfo in fiveMProcesses)
        {
            if (processInfo.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase) &&
                processInfo.WindowHandle != IntPtr.Zero)
            {
                return processInfo.WindowHandle;
            }
        }

        // Fallback to basic process lookup
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

    /// <summary>
    /// Check if the process name corresponds to FiveM
    /// </summary>
    private static bool IsFiveMProcess(string processName)
    {
        var lowerName = processName.ToLowerInvariant();
        return lowerName.Contains("fivem") || 
               lowerName.Contains("citizenfx") || 
               lowerName.Contains("gtaprocess");
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
/// Factory for creating capture backends
/// </summary>
public static class CaptureBackendFactory
{
    /// <summary>
    /// Create the best available capture backend for the current system
    /// Prioritizes DXGI for FiveM game capture, falls back to GDI
    /// </summary>
    public static ICaptureBackend CreateBestBackend()
    {
        // Prefer DXGI capture for OBS-style game capture functionality
        if (OperatingSystem.IsWindows())
        {
            var dxgi = new DxgiCaptureBackend();
            if (dxgi.IsAvailable)
            {
                return dxgi;
            }
        }
        
        // Fall back to GDI
        return new GdiCaptureBackend();
    }
    
    /// <summary>
    /// Get all available capture backends
    /// </summary>
    public static IEnumerable<ICaptureBackend> GetAvailableBackends()
    {
        var backends = new List<ICaptureBackend>();
        
        var gdi = new GdiCaptureBackend();
        if (gdi.IsAvailable)
            backends.Add(gdi);
            
#if FUTURE_DXGI_SUPPORT
        var dxgi = new DxgiCaptureBackend();
        if (dxgi.IsAvailable)
            backends.Add(dxgi);
#endif
        
        return backends;
    }
}