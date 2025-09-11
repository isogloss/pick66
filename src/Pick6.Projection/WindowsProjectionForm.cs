using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Pick6.Core;
using Pick6.Core.Timing;
using Pick6.Core.Diagnostics;

namespace Pick6.Projection;

/// <summary>
/// Windows Forms-based borderless projection window
/// This provides a more complete Windows implementation when running on Windows
/// </summary>
public class WindowsProjectionForm
{
    private bool _isProjecting = false;
    private IntPtr _windowHandle = IntPtr.Zero;
    private Bitmap? _currentFrame;
    private readonly object _frameLock = new();
    private Thread? _renderThread;
    private int _targetFPS = 60;
    private int _screenIndex = 0;
    private readonly FramePacer _framePacer = new();
    private readonly FrameStatistics _statistics = new();
    
    // High-performance rendering fields
    private IntPtr _memoryDC = IntPtr.Zero;
    private IntPtr _currentHBitmap = IntPtr.Zero;
    private IntPtr _oldBitmap = IntPtr.Zero;
    private bool _enableFpsLogging = false;
    private int _frameCount = 0;
    private DateTime _lastFpsLogTime = DateTime.Now;
    private bool _matchCaptureFPS = false;

    public event EventHandler? ProjectionStarted;
    public event EventHandler? ProjectionStopped;

    /// <summary>
    /// Frame timing and performance statistics for projection rendering
    /// </summary>
    public FrameStatistics Statistics => _statistics;

    /// <summary>
    /// Start borderless fullscreen projection
    /// </summary>
    public void StartProjection(int screenIndex = 0)
    {
        if (_isProjecting) return;

        _screenIndex = screenIndex;
        _isProjecting = true;
        CreateBorderlessWindow(screenIndex);
        StartRenderLoop();
        
        ProjectionStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Set the target FPS for the projection
    /// </summary>
    public void SetTargetFPS(int fps)
    {
        _targetFPS = Math.Max(15, Math.Min(240, fps));
        
        // Reset frame pacing with new FPS if projection is active
        if (_isProjecting)
        {
            _framePacer.Reset(_targetFPS, PacingMode.HybridSpin);
            _statistics.Reset();
        }
    }

    /// <summary>
    /// Enable or disable FPS logging for debugging
    /// </summary>
    public void SetFpsLogging(bool enabled)
    {
        _enableFpsLogging = enabled;
        if (enabled)
        {
            _frameCount = 0;
            _lastFpsLogTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Enable or disable match capture FPS mode
    /// When enabled, projection FPS will automatically match the capture engine's target FPS
    /// </summary>
    public void SetMatchCaptureFPS(bool enabled)
    {
        _matchCaptureFPS = enabled;
    }

    /// <summary>
    /// Update the projection FPS based on capture engine settings (used in match capture FPS mode)
    /// </summary>
    public void UpdateCaptureFPS(int captureFPS)
    {
        if (_matchCaptureFPS && captureFPS > 0)
        {
            SetTargetFPS(captureFPS);
        }
    }

    /// <summary>
    /// Enable or disable stealth mode (hidden from Alt+Tab and taskbar)
    /// </summary>
    [SupportedOSPlatform("windows")]
    public void SetStealthMode(bool enabled)
    {
        if (_windowHandle == IntPtr.Zero) return;

        if (enabled)
        {
            EnableStealthMode();
        }
        else
        {
            DisableStealthMode();
        }
    }

    [SupportedOSPlatform("windows")]
    private void EnableStealthMode()
    {
        if (_windowHandle == IntPtr.Zero) return;

        // Hide from Alt+Tab by removing from taskbar and setting as tool window
        var exStyle = GetWindowLong(_windowHandle, GWL_EXSTYLE);
        exStyle |= (int)WS_EX_TOOLWINDOW;
        exStyle &= ~(int)WS_EX_APPWINDOW;
        SetWindowLong(_windowHandle, GWL_EXSTYLE, exStyle);

        // Force window to update
        SetWindowPos(_windowHandle, IntPtr.Zero, 0, 0, 0, 0, 
                     SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    [SupportedOSPlatform("windows")]
    private void DisableStealthMode()
    {
        if (_windowHandle == IntPtr.Zero) return;

        // Show in Alt+Tab by adding to taskbar and removing tool window style
        var exStyle = GetWindowLong(_windowHandle, GWL_EXSTYLE);
        exStyle &= ~(int)WS_EX_TOOLWINDOW;
        exStyle |= (int)WS_EX_APPWINDOW;
        SetWindowLong(_windowHandle, GWL_EXSTYLE, exStyle);

        // Force window to update
        SetWindowPos(_windowHandle, IntPtr.Zero, 0, 0, 0, 0, 
                     SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    /// <summary>
    /// Stop projection
    /// </summary>
    public void StopProjection()
    {
        if (!_isProjecting) return;

        _isProjecting = false;
        _renderThread?.Join(1000);
        
        // Clean up GDI resources
        CleanupGDIResources();
        
        if (_windowHandle != IntPtr.Zero)
        {
            DestroyWindow(_windowHandle);
            _windowHandle = IntPtr.Zero;
        }
        
        ProjectionStopped?.Invoke(this, EventArgs.Empty);
    }

    [SupportedOSPlatform("windows")]
    private void CleanupGDIResources()
    {
        if (_currentHBitmap != IntPtr.Zero)
        {
            if (_memoryDC != IntPtr.Zero && _oldBitmap != IntPtr.Zero)
            {
                SelectObject(_memoryDC, _oldBitmap);
            }
            DeleteObject(_currentHBitmap);
            _currentHBitmap = IntPtr.Zero;
            _oldBitmap = IntPtr.Zero;
        }

        if (_memoryDC != IntPtr.Zero)
        {
            DeleteDC(_memoryDC);
            _memoryDC = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Update the frame being displayed
    /// </summary>
    [SupportedOSPlatform("windows")]
    public void UpdateFrame(Bitmap frame)
    {
        if (!_isProjecting) return;

        lock (_frameLock)
        {
            _currentFrame?.Dispose();
            _currentFrame = new Bitmap(frame);
            
            // Update the memory DC for fast blitting
            UpdateMemoryDC(frame);
        }
    }

    [SupportedOSPlatform("windows")]
    private void UpdateMemoryDC(Bitmap frame)
    {
        if (_windowHandle == IntPtr.Zero) return;

        var windowDC = GetDC(_windowHandle);
        if (windowDC != IntPtr.Zero)
        {
            try
            {
                // Create memory DC if it doesn't exist
                if (_memoryDC == IntPtr.Zero)
                {
                    _memoryDC = CreateCompatibleDC(windowDC);
                }

                // Check if we need to recreate the bitmap (size changed)
                var needNewBitmap = _currentHBitmap == IntPtr.Zero;
                if (!needNewBitmap)
                {
                    // TODO: Check if frame size changed and recreate if necessary
                    // For now, we'll recreate on every frame for simplicity but this could be optimized
                    needNewBitmap = true;
                }

                if (needNewBitmap)
                {
                    // Clean up previous bitmap first
                    if (_currentHBitmap != IntPtr.Zero)
                    {
                        if (_memoryDC != IntPtr.Zero && _oldBitmap != IntPtr.Zero)
                        {
                            SelectObject(_memoryDC, _oldBitmap);
                        }
                        DeleteObject(_currentHBitmap);
                        _currentHBitmap = IntPtr.Zero;
                    }

                    // Create new compatible bitmap for the frame
                    _currentHBitmap = CreateCompatibleBitmap(windowDC, frame.Width, frame.Height);
                    if (_currentHBitmap != IntPtr.Zero)
                    {
                        _oldBitmap = SelectObject(_memoryDC, _currentHBitmap);
                    }
                }

                // Update the bitmap content with the new frame
                if (_memoryDC != IntPtr.Zero && _currentHBitmap != IntPtr.Zero)
                {
                    using (var memoryGraphics = Graphics.FromHdc(_memoryDC))
                    {
                        memoryGraphics.Clear(Color.Black); // Clear previous content
                        memoryGraphics.DrawImage(frame, 0, 0, frame.Width, frame.Height);
                    }
                }
            }
            finally
            {
                ReleaseDC(_windowHandle, windowDC);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void CreateBorderlessWindow(int screenIndex)
    {
        // Get screen dimensions
        var screenBounds = GetScreenBounds(screenIndex);
        
        // Create borderless window class
        var wndClass = new WNDCLASS
        {
            style = CS_HREDRAW | CS_VREDRAW,
            lpfnWndProc = WindowProc,
            hInstance = GetModuleHandle(null),
            hCursor = LoadCursor(IntPtr.Zero, IDC_ARROW),
            hbrBackground = (IntPtr)(COLOR_WINDOW + 1),
            lpszClassName = "Pick6ProjectionWindow"
        };

        RegisterClass(ref wndClass);

        // Create the window
        _windowHandle = CreateWindowEx(
            WS_EX_TOPMOST | WS_EX_TOOLWINDOW, // Start with stealth mode enabled
            "Pick6ProjectionWindow",
            "Pick6 Projection",
            WS_POPUP,
            screenBounds.X,
            screenBounds.Y,
            screenBounds.Width,
            screenBounds.Height,
            IntPtr.Zero,
            IntPtr.Zero,
            GetModuleHandle(null),
            IntPtr.Zero
        );

        if (_windowHandle != IntPtr.Zero)
        {
            ShowWindow(_windowHandle, SW_SHOW);
            UpdateWindow(_windowHandle);
            SetForegroundWindow(_windowHandle);
            
            // Enable stealth mode by default
            EnableStealthMode();
        }
    }

    [SupportedOSPlatform("windows")]
    private void StartRenderLoop()
    {
        _renderThread = new Thread(() =>
        {
            // Set high thread priority for smooth rendering
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            
            // Enable high-resolution timing (1ms precision)
            timeBeginPeriod(1);
            
            try
            {
                // Initialize frame pacing and statistics
                _framePacer.Reset(_targetFPS, PacingMode.HybridSpin);
                _statistics.Reset();
                
                // Enable diagnostics logging if requested
                var enableDiagnostics = Environment.GetEnvironmentVariable("PICK6_DIAG") == "1";

                while (_isProjecting && _windowHandle != IntPtr.Zero)
                {
                    // Only render if we have the latest frame (skip stale frames)
                    RenderFrame();

                    // Wait for next frame and get timing statistics
                    var frameElapsed = _framePacer.WaitNextFrame();
                    _statistics.RecordFrame(frameElapsed.ElapsedMs, frameElapsed.TargetIntervalMs);

                    // Optional diagnostic logging
                    if (enableDiagnostics && _statistics.TotalFrames % 60 == 0) // Log every ~1 second at 60fps
                    {
                        var hasFrame = _currentFrame != null ? "with frame" : "no frame";
                        Log.Debug($"[Projection] {_statistics.GetSummary()} - {hasFrame}");
                    }

                    // Legacy FPS logging for backward compatibility
                    if (_enableFpsLogging)
                    {
                        _frameCount++;
                        var elapsed = DateTime.Now - _lastFpsLogTime;
                        if (elapsed.TotalSeconds >= 1.0)
                        {
                            var hasFrame = _currentFrame != null ? "with frame" : "no frame";
                            Log.Debug($"Projection FPS: {_statistics.InstantFps:F1} (avg: {_statistics.AverageFps:F1}, target: {_targetFPS}) - {hasFrame}");
                            _frameCount = 0;
                            _lastFpsLogTime = DateTime.Now;
                        }
                    }
                }
            }
            finally
            {
                // Restore timer resolution
                timeEndPeriod(1);
            }
        })
        { IsBackground = true };
        
        _renderThread.Start();
    }

    [SupportedOSPlatform("windows")]
    private void RenderFrame()
    {
        if (_windowHandle == IntPtr.Zero) return;

        lock (_frameLock)
        {
            // Get window dimensions
            var windowRect = new RECT();
            if (!GetClientRect(_windowHandle, out windowRect)) return;
            
            var windowWidth = windowRect.Right - windowRect.Left;
            var windowHeight = windowRect.Bottom - windowRect.Top;
            
            if (windowWidth <= 0 || windowHeight <= 0) return;

            // Fast GDI-based blitting for better performance
            var windowDC = GetDC(_windowHandle);
            if (windowDC != IntPtr.Zero)
            {
                try
                {
                    if (_currentFrame != null && _memoryDC != IntPtr.Zero && _currentHBitmap != IntPtr.Zero)
                    {
                        // Use StretchBlt from memory DC for much faster rendering
                        StretchBlt(windowDC, 0, 0, windowWidth, windowHeight,
                                 _memoryDC, 0, 0, _currentFrame.Width, _currentFrame.Height, SRCCOPY);
                    }
                    else if (_currentFrame != null)
                    {
                        // Fallback to GDI+ if memory DC is not available
                        using (var graphics = Graphics.FromHdc(windowDC))
                        {
                            graphics.DrawImage(_currentFrame, 0, 0, windowWidth, windowHeight);
                        }
                    }
                    else
                    {
                        // No frame available - render a black screen with text
                        var blackBrush = CreateSolidBrush(0x000000); // Black
                        var rect = new RECT { Left = 0, Top = 0, Right = windowWidth, Bottom = windowHeight };
                        FillRect(windowDC, ref rect, blackBrush);
                        DeleteObject(blackBrush);
                        
                        // Add text indicating waiting for frames
                        SetBkMode(windowDC, 1); // TRANSPARENT
                        SetTextColor(windowDC, 0xFFFFFF); // White
                        var text = "Pick6 Projection - Waiting for frames...";
                        TextOut(windowDC, 50, windowHeight / 2, text, text.Length);
                    }
                }
                finally
                {
                    ReleaseDC(_windowHandle, windowDC);
                }
            }
        }
    }

    private Rectangle GetScreenBounds(int screenIndex)
    {
        return MonitorHelper.GetMonitorBounds(screenIndex);
    }

    [SupportedOSPlatform("windows")]
    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_DESTROY:
                PostQuitMessage(0);
                return IntPtr.Zero;
                
            case WM_KEYDOWN:
                if (wParam.ToInt32() == VK_ESCAPE)
                {
                    StopProjection();
                    return IntPtr.Zero;
                }
                break;
                
            case WM_PAINT:
                RenderFrame();
                break;
        }
        
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    #region Win32 API
    [DllImport("user32.dll")]
    private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
    
    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    [DllImport("user32.dll")]
    private static extern bool UpdateWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    
    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    
    [DllImport("user32.dll")]
    private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);
    
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
    
    [DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);
    
    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    
    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    // High-resolution timer functions
    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint uPeriod);

    // GDI functions for fast blitting
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool StretchBlt(IntPtr hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest,
        IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(uint crColor);

    [DllImport("user32.dll")]
    private static extern int FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

    [DllImport("gdi32.dll")]
    private static extern int SetBkMode(IntPtr hdc, int mode);

    [DllImport("gdi32.dll")]
    private static extern uint SetTextColor(IntPtr hdc, uint color);

    [DllImport("gdi32.dll")]
    private static extern bool TextOut(IntPtr hdc, int x, int y, string lpString, int c);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct WNDCLASS
    {
        public uint style;
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const uint WS_POPUP = 0x80000000;
    private const uint WS_EX_TOPMOST = 0x00000008;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_APPWINDOW = 0x00040000;
    private const uint CS_HREDRAW = 0x0002;
    private const uint CS_VREDRAW = 0x0001;
    private const int SW_SHOW = 5;
    private const uint WM_DESTROY = 0x0002;
    private const uint WM_PAINT = 0x000F;
    private const uint WM_KEYDOWN = 0x0100;
    private const int VK_ESCAPE = 0x1B;
    private const int IDC_ARROW = 32512;
    private const int COLOR_WINDOW = 5;
    private const int GWL_EXSTYLE = -20;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SRCCOPY = 0x00CC0020;
    #endregion
}