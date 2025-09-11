using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Pick6.Core;

/// <summary>
/// Global keybind manager for Pick6 - inspired by C++ reference implementation
/// Provides system-wide hotkey support for loader and projection control
/// </summary>
public class GlobalKeybindManager : IDisposable
{
    private readonly Dictionary<int, Action> _registeredKeybinds = new();
    private readonly object _lock = new();
    private bool _isMonitoring = false;
    private Thread? _monitoringThread;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public delegate void KeybindAction();

    /// <summary>
    /// Register a global keybind
    /// </summary>
    public bool RegisterKeybind(int virtualKey, bool ctrl, bool alt, bool shift, string description, Action action)
    {
        if (!OperatingSystem.IsWindows()) return false;

        var keybindId = GenerateKeybindId(virtualKey, ctrl, alt, shift);
        
        lock (_lock)
        {
            if (_registeredKeybinds.ContainsKey(keybindId))
            {
                return false; // Already registered
            }

            _registeredKeybinds[keybindId] = action;
        }

        return RegisterHotKey(IntPtr.Zero, keybindId, GetModifiers(ctrl, alt, shift), (uint)virtualKey);
    }

    /// <summary>
    /// Start monitoring for global keybinds
    /// </summary>
    public void StartMonitoring()
    {
        if (_isMonitoring || !OperatingSystem.IsWindows()) return;

        _isMonitoring = true;
        _monitoringThread = new Thread(MonitoringLoop)
        {
            IsBackground = true,
            Name = "GlobalKeybindMonitor"
        };
        _monitoringThread.Start();
    }

    /// <summary>
    /// Stop monitoring for global keybinds
    /// </summary>
    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        _isMonitoring = false;
        _cancellationTokenSource.Cancel();
        
        if (OperatingSystem.IsWindows())
        {
            // Unregister all hotkeys
            lock (_lock)
            {
                foreach (var keybindId in _registeredKeybinds.Keys)
                {
                    UnregisterHotKey(IntPtr.Zero, keybindId);
                }
                _registeredKeybinds.Clear();
            }
        }

        _monitoringThread?.Join(1000);
        _monitoringThread = null;
    }

    private int GenerateKeybindId(int virtualKey, bool ctrl, bool alt, bool shift)
    {
        // Generate a unique ID based on key combination
        return (virtualKey << 16) | (ctrl ? 1 : 0) | (alt ? 2 : 0) | (shift ? 4 : 0);
    }

    private uint GetModifiers(bool ctrl, bool alt, bool shift)
    {
        uint modifiers = 0;
        if (ctrl) modifiers |= MOD_CONTROL;
        if (alt) modifiers |= MOD_ALT;
        if (shift) modifiers |= MOD_SHIFT;
        return modifiers;
    }

    [SupportedOSPlatform("windows")]
    private void MonitoringLoop()
    {
        try
        {
            while (_isMonitoring && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var msg = new MSG();
                var result = GetMessage(out msg, IntPtr.Zero, 0, 0);
                
                if (result == 0 || result == -1) break; // Quit message or error
                
                if (msg.message == WM_HOTKEY)
                {
                    var keybindId = msg.wParam.ToInt32();
                    Action? action = null;
                    
                    lock (_lock)
                    {
                        _registeredKeybinds.TryGetValue(keybindId, out action);
                    }
                    
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error executing keybind action: {ex.Message}");
                    }
                }
                
                DispatchMessage(ref msg);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Global keybind monitoring error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        StopMonitoring();
        _cancellationTokenSource.Dispose();
    }

    #region Win32 API
    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool DispatchMessage(ref MSG lpmsg);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint WM_HOTKEY = 0x0312;

    // Virtual key codes
    public const int VK_L = 0x4C;
    public const int VK_P = 0x50;
    public const int VK_ESCAPE = 0x1B;
    public const int VK_F1 = 0x70;
    public const int VK_F2 = 0x71;
    public const int VK_F3 = 0x72;
    #endregion
}

/// <summary>
/// Default keybind configurations for Pick6
/// </summary>
public static class DefaultKeybinds
{
    public static void RegisterDefaultKeybinds(GlobalKeybindManager manager, 
                                              Action? toggleLoader = null,
                                              Action? toggleProjection = null,
                                              Action? closeProjection = null)
    {
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            // Ctrl+L - Toggle loader window visibility
            if (toggleLoader != null)
            {
                manager.RegisterKeybind(GlobalKeybindManager.VK_L, true, false, false, 
                                      "Ctrl+L - Toggle Loader", toggleLoader);
            }

            // Ctrl+P - Toggle projection window
            if (toggleProjection != null)
            {
                manager.RegisterKeybind(GlobalKeybindManager.VK_P, true, false, false, 
                                      "Ctrl+P - Toggle Projection", toggleProjection);
            }

            // Ctrl+Shift+Esc - Close projection immediately
            if (closeProjection != null)
            {
                manager.RegisterKeybind(GlobalKeybindManager.VK_ESCAPE, true, false, true, 
                                      "Ctrl+Shift+Esc - Close Projection", closeProjection);
            }

            Console.WriteLine("✅ Default global keybinds registered:");
            Console.WriteLine("   Ctrl+L - Toggle Loader Window");
            Console.WriteLine("   Ctrl+P - Toggle Projection Window");
            Console.WriteLine("   Ctrl+Shift+Esc - Close Projection");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Could not register some global keybinds: {ex.Message}");
        }
    }
}