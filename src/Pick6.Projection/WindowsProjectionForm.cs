using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Pick6.Core;

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

    public event EventHandler? ProjectionStarted;
    public event EventHandler? ProjectionStopped;

    /// <summary>
    /// Start borderless fullscreen projection
    /// </summary>
    public void StartProjection(int screenIndex = 0)
    {
        if (_isProjecting || !OperatingSystem.IsWindows()) return;

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
        _targetFPS = Math.Max(15, Math.Min(120, fps));
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
        
        if (_windowHandle != IntPtr.Zero)
        {
            DestroyWindow(_windowHandle);
            _windowHandle = IntPtr.Zero;
        }
        
        ProjectionStopped?.Invoke(this, EventArgs.Empty);
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
            var frameTimeMs = 1000.0 / _targetFPS;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var lastRenderTime = 0.0;

            while (_isProjecting && _windowHandle != IntPtr.Zero)
            {
                var currentTime = stopwatch.Elapsed.TotalMilliseconds;
                
                if (currentTime - lastRenderTime >= frameTimeMs)
                {
                    RenderFrame();
                    lastRenderTime = currentTime;
                }
                
                // Small sleep to prevent 100% CPU usage
                Thread.Sleep(1);
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
            if (_currentFrame == null) return;

            var hdc = GetDC(_windowHandle);
            if (hdc != IntPtr.Zero)
            {
                using (var graphics = Graphics.FromHdc(hdc))
                {
                    var windowRect = new RECT();
                    GetClientRect(_windowHandle, out windowRect);
                    
                    // Draw the frame stretched to fill the window
                    graphics.DrawImage(_currentFrame, 0, 0, 
                        windowRect.Right - windowRect.Left,
                        windowRect.Bottom - windowRect.Top);
                }
                
                ReleaseDC(_windowHandle, hdc);
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
    #endregion
}