using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Pick6.Core;

/// <summary>
/// Global keybind manager for Pick6 - inspired by C++ reference implementation
/// Provides system-wide hotkey support for loader and projection control
/// </summary>
public class GlobalKeybindManager : IDisposable
{
    private readonly Dictionary<int, KeybindInfo> _registeredKeybinds = new();
    private readonly object _lock = new();
    private bool _isMonitoring = false;
    private Thread? _monitoringThread;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public delegate void KeybindAction();

    /// <summary>
    /// Information about a registered keybind
    /// </summary>
    public class KeybindInfo
    {
        public int VirtualKey { get; set; }
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public string Description { get; set; } = "";
        public Action Action { get; set; } = () => { };
        public int KeybindId { get; set; }
    }

    /// <summary>
    /// Get all currently registered keybinds
    /// </summary>
    public List<KeybindInfo> GetRegisteredKeybinds()
    {
        lock (_lock)
        {
            return _registeredKeybinds.Values.ToList();
        }
    }

    /// <summary>
    /// Unregister a keybind by its ID
    /// </summary>
    public bool UnregisterKeybind(int keybindId)
    {
        if (!OperatingSystem.IsWindows()) return false;

        lock (_lock)
        {
            if (_registeredKeybinds.Remove(keybindId))
            {
                UnregisterHotKey(IntPtr.Zero, keybindId);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if a key combination is already registered
    /// </summary>
    public bool IsKeybindRegistered(int virtualKey, bool ctrl, bool alt, bool shift)
    {
        var keybindId = GenerateKeybindId(virtualKey, ctrl, alt, shift);
        lock (_lock)
        {
            return _registeredKeybinds.ContainsKey(keybindId);
        }
    }

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

            var keybindInfo = new KeybindInfo
            {
                VirtualKey = virtualKey,
                Ctrl = ctrl,
                Alt = alt,
                Shift = shift,
                Description = description,
                Action = action,
                KeybindId = keybindId
            };

            if (RegisterHotKey(IntPtr.Zero, keybindId, GetModifiers(ctrl, alt, shift), (uint)virtualKey))
            {
                _registeredKeybinds[keybindId] = keybindInfo;
                return true;
            }
        }

        return false;
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

    public static int GenerateKeybindId(int virtualKey, bool ctrl, bool alt, bool shift)
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
                        if (_registeredKeybinds.TryGetValue(keybindId, out var keybindInfo))
                        {
                            action = keybindInfo.Action;
                        }
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
    public const int VK_A = 0x41;
    public const int VK_B = 0x42;
    public const int VK_C = 0x43;
    public const int VK_D = 0x44;
    public const int VK_E = 0x45;
    public const int VK_F = 0x46;
    public const int VK_G = 0x47;
    public const int VK_H = 0x48;
    public const int VK_I = 0x49;
    public const int VK_J = 0x4A;
    public const int VK_K = 0x4B;
    public const int VK_M = 0x4D;
    public const int VK_N = 0x4E;
    public const int VK_O = 0x4F;
    public const int VK_Q = 0x51;
    public const int VK_R = 0x52;
    public const int VK_S = 0x53;
    public const int VK_T = 0x54;
    public const int VK_U = 0x55;
    public const int VK_V = 0x56;
    public const int VK_W = 0x57;
    public const int VK_X = 0x58;
    public const int VK_Y = 0x59;
    public const int VK_Z = 0x5A;

    /// <summary>
    /// Get virtual key code from a key name
    /// </summary>
    public static int GetVirtualKeyFromName(string keyName)
    {
        return keyName.ToUpper() switch
        {
            "A" => VK_A, "B" => VK_B, "C" => VK_C, "D" => VK_D, "E" => VK_E,
            "F" => VK_F, "G" => VK_G, "H" => VK_H, "I" => VK_I, "J" => VK_J,
            "K" => VK_K, "L" => VK_L, "M" => VK_M, "N" => VK_N, "O" => VK_O,
            "P" => VK_P, "Q" => VK_Q, "R" => VK_R, "S" => VK_S, "T" => VK_T,
            "U" => VK_U, "V" => VK_V, "W" => VK_W, "X" => VK_X, "Y" => VK_Y,
            "Z" => VK_Z, "ESCAPE" => VK_ESCAPE, "ESC" => VK_ESCAPE,
            "F1" => VK_F1, "F2" => VK_F2, "F3" => VK_F3,
            _ => -1
        };
    }

    /// <summary>
    /// Get key name from virtual key code
    /// </summary>
    public static string GetKeyNameFromVirtualKey(int virtualKey)
    {
        return virtualKey switch
        {
            VK_A => "A", VK_B => "B", VK_C => "C", VK_D => "D", VK_E => "E",
            VK_F => "F", VK_G => "G", VK_H => "H", VK_I => "I", VK_J => "J",
            VK_K => "K", VK_L => "L", VK_M => "M", VK_N => "N", VK_O => "O",
            VK_P => "P", VK_Q => "Q", VK_R => "R", VK_S => "S", VK_T => "T",
            VK_U => "U", VK_V => "V", VK_W => "W", VK_X => "X", VK_Y => "Y",
            VK_Z => "Z", VK_ESCAPE => "ESC", VK_F1 => "F1", VK_F2 => "F2", VK_F3 => "F3",
            _ => $"Key{virtualKey}"
        };
    }

    /// <summary>
    /// Format a keybind combination as a human-readable string
    /// </summary>
    public static string FormatKeybindString(bool ctrl, bool alt, bool shift, int virtualKey)
    {
        var parts = new List<string>();
        if (ctrl) parts.Add("Ctrl");
        if (alt) parts.Add("Alt");
        if (shift) parts.Add("Shift");
        parts.Add(GetKeyNameFromVirtualKey(virtualKey));
        return string.Join("+", parts);
    }
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
                                              Action? closeProjection = null,
                                              Action? stopProjectionAndRestore = null)
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

            // Ctrl+Shift+P - Stop projection and restore menu/window
            if (stopProjectionAndRestore != null)
            {
                manager.RegisterKeybind(GlobalKeybindManager.VK_P, true, false, true, 
                                      "Ctrl+Shift+P - Stop Projection & Restore Menu", stopProjectionAndRestore);
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
            Console.WriteLine("   Ctrl+Shift+P - Stop Projection & Restore Menu");
            Console.WriteLine("   Ctrl+Shift+Esc - Close Projection");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Could not register some global keybinds: {ex.Message}");
        }
    }
}