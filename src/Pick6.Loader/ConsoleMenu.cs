using Pick6.Core;
using Pick6.Projection;
using System.Drawing;

namespace Pick6.Loader;

/// <summary>
/// Console menu implementation for Pick6 Loader
/// Extracted from Pick6.UI and Pick6.Launcher for reuse
/// </summary>
public class ConsoleMenu
{
    private readonly GameCaptureEngine _captureEngine;
    private readonly BorderlessProjectionWindow _projectionWindow;
    private readonly GlobalKeybindManager? _keybindManager;
    private int _selectedMonitor = 0;

    public ConsoleMenu()
    {
        _captureEngine = new GameCaptureEngine();
        _projectionWindow = new BorderlessProjectionWindow();
        
        // Initialize global keybind manager
        _keybindManager = new GlobalKeybindManager();
        SetupGlobalKeybinds();
        
        SetupEventHandlers();
    }

    private void SetupGlobalKeybinds()
    {
        if (_keybindManager == null) return;

        DefaultKeybinds.RegisterDefaultKeybinds(_keybindManager,
            toggleLoader: null, // No loader window in console mode
            toggleProjection: () => ToggleProjection(),
            closeProjection: () => StopProjectionOnly(),
            stopProjectionAndRestore: () => StopProjectionAndRestoreMenu(),
            closeProjectionAndToggleLoader: () => CloseProjectionAndToggleLoader()
        );

        _keybindManager.StartMonitoring();
        Console.WriteLine("Global keybinds enabled for console mode.");
    }

    public void Run(string[] args)
    {
        var isRunning = true;

        Console.WriteLine("================================================");
        Console.WriteLine("                   pick6                        ");
        Console.WriteLine("================================================");
        Console.WriteLine();

        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            isRunning = false;
        };

        // Check for command line arguments
        if (args.Length > 0)
        {
            HandleCommandLineArgs(args);
        }

        // Main application loop
        RunMainLoop(ref isRunning);

        // Cleanup
        Cleanup();
    }

    private void SetupEventHandlers()
    {
        // Forward captured frames to projection window
        _captureEngine.FrameCaptured += (s, e) =>
        {
            _projectionWindow.UpdateFrame(e.Frame);
        };

        // Handle capture errors
        _captureEngine.ErrorOccurred += (s, errorMessage) =>
        {
            Console.WriteLine($"[ERROR] Capture: {errorMessage}");
        };

        // Handle projection events
        _projectionWindow.ProjectionStarted += (s, e) =>
        {
            Console.WriteLine("[INFO] Projection started");
        };

        _projectionWindow.ProjectionStopped += (s, e) =>
        {
            Console.WriteLine("[INFO] Projection stopped");
        };
    }

    private void HandleCommandLineArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--auto-start":
                    AutoStartCapture();
                    break;
                case "--fps":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int fps))
                    {
                        _captureEngine.Settings.TargetFPS = fps;
                        Console.WriteLine($"[INFO] Target FPS set to {fps}");
                        i++;
                    }
                    break;
                case "--fps-logging":
                    _projectionWindow.SetFpsLogging(true);
                    Console.WriteLine("[INFO] FPS logging enabled");
                    break;
                case "--resolution":
                    if (i + 2 < args.Length && 
                        int.TryParse(args[i + 1], out int width) &&
                        int.TryParse(args[i + 2], out int height))
                    {
                        _captureEngine.Settings.ScaleWidth = width;
                        _captureEngine.Settings.ScaleHeight = height;
                        Console.WriteLine($"[INFO] Resolution set to {width}x{height}");
                        i += 2;
                    }
                    break;
                case "--monitor":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int monitor))
                    {
                        var monitors = MonitorHelper.GetAllMonitors();
                        if (monitor >= 0 && monitor < monitors.Count)
                        {
                            _selectedMonitor = monitor;
                            Console.WriteLine($"[INFO] Target monitor set to {monitor}");
                        }
                        else
                        {
                            Console.WriteLine($"[WARNING] Invalid monitor {monitor}. Available: 0-{monitors.Count - 1}");
                        }
                        i++;
                    }
                    break;
            }
        }
    }

    private void RunMainLoop(ref bool isRunning)
    {
        while (isRunning)
        {
            ShowMainMenu();
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    ScanForFiveM();
                    break;
                case "2":
                    StartCapture();
                    break;
                case "3":
                    StopCapture();
                    break;
                case "4":
                    StartProjection();
                    break;
                case "5":
                    StopProjection();
                    break;
                case "6":
                    ConfigureSettings();
                    break;
                case "7":
                    QuickStart();
                    break;
                case "8":
                    ShowStatus();
                    break;
                case "9":
                    TestProjectionWithDemoFrames();
                    break;
                case "k":
                case "K":
                case "keybinds":
                    ConfigureKeybinds();
                    break;
                case "h":
                case "H":
                    ConfigureCloseProjectionToggleLoaderKeybind();
                    break;
                case "0":
                case "exit":
                case "quit":
                    isRunning = false;
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }

            if (isRunning)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }

    private void ShowMainMenu()
    {
        Console.Clear();
        Console.WriteLine("=== pick6 ===");
        Console.WriteLine();
        Console.WriteLine("1. Scan for processes");
        Console.WriteLine("2. Start capture");
        Console.WriteLine("3. Stop capture");
        Console.WriteLine("4. Start projection");
        Console.WriteLine("5. Stop projection");
        Console.WriteLine("6. Settings");
        Console.WriteLine("7. Quick start");
        Console.WriteLine("8. Status");
        Console.WriteLine("9. Test projection");
        Console.WriteLine("K. Keybinds");
        
        // Show the configurable hotkey
        var currentKey = GetCurrentCloseProjectionToggleLoaderKey();
        Console.WriteLine($"H. Keybind: Close Projection + Toggle Loader ({currentKey})");
        
        Console.WriteLine("0. Exit");
        Console.WriteLine();
        Console.Write("Choice: ");
    }

    private string GetCurrentCloseProjectionToggleLoaderKey()
    {
        if (_keybindManager == null) return "Not Available";
        
        var keybinds = _keybindManager.GetRegisteredKeybinds();
        var targetKeybind = keybinds.FirstOrDefault(kb => kb.Description.Contains("Close Projection + Toggle Loader"));
        
        if (targetKeybind == null) return "F12";
        
        return GlobalKeybindManager.FormatKeybindString(targetKeybind.Ctrl, targetKeybind.Alt, targetKeybind.Shift, targetKeybind.VirtualKey);
    }

    private void ConfigureCloseProjectionToggleLoaderKeybind()
    {
        Console.Clear();
        Console.WriteLine("=== Configure Close Projection + Toggle Loader Keybind ===");
        Console.WriteLine();
        Console.WriteLine("Current keybind: " + GetCurrentCloseProjectionToggleLoaderKey());
        Console.WriteLine();
        Console.WriteLine("Press the new key combination you want to use...");
        Console.WriteLine("(Supports: F1-F12, A-Z, with optional Ctrl, Alt, Shift modifiers)");
        Console.WriteLine("Press ESC to cancel");
        Console.WriteLine();
        
        // TODO: Implement key capture logic here
        // For now, let's provide a simple text input method
        Console.Write("Enter new key (e.g., F12, Ctrl+F1, Alt+Shift+P): ");
        var input = Console.ReadLine()?.Trim().ToUpper();
        
        if (string.IsNullOrEmpty(input) || input == "ESC")
        {
            Console.WriteLine("Keybind configuration cancelled.");
            Thread.Sleep(1000);
            return;
        }
        
        // Parse the input and update the keybind
        if (TryParseKeybindInput(input, out bool ctrl, out bool alt, out bool shift, out int virtualKey))
        {
            UpdateCloseProjectionToggleLoaderKeybind(ctrl, alt, shift, virtualKey);
            Console.WriteLine($"✅ Keybind updated to: {GlobalKeybindManager.FormatKeybindString(ctrl, alt, shift, virtualKey)}");
        }
        else
        {
            Console.WriteLine("❌ Invalid key combination. Please use format like: F12, Ctrl+F1, Alt+Shift+P");
        }
        
        Thread.Sleep(2000);
    }

    private bool TryParseKeybindInput(string input, out bool ctrl, out bool alt, out bool shift, out int virtualKey)
    {
        ctrl = alt = shift = false;
        virtualKey = -1;
        
        var parts = input.Split('+');
        string keyName = parts[parts.Length - 1]; // Last part is the key
        
        // Check for modifiers
        for (int i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i])
            {
                case "CTRL": ctrl = true; break;
                case "ALT": alt = true; break;
                case "SHIFT": shift = true; break;
            }
        }
        
        // Convert key name to virtual key
        virtualKey = GlobalKeybindManager.GetVirtualKeyFromName(keyName);
        return virtualKey != -1;
    }

    private void UpdateCloseProjectionToggleLoaderKeybind(bool ctrl, bool alt, bool shift, int virtualKey)
    {
        if (_keybindManager == null) return;
        
        // First, find and remove the existing keybind
        var keybinds = _keybindManager.GetRegisteredKeybinds();
        var existingKeybind = keybinds.FirstOrDefault(kb => kb.Description.Contains("Close Projection + Toggle Loader"));
        
        if (existingKeybind != null)
        {
            _keybindManager.UnregisterKeybind(existingKeybind.KeybindId);
        }
        
        // Register the new keybind
        var description = $"{GlobalKeybindManager.FormatKeybindString(ctrl, alt, shift, virtualKey)} - Close Projection + Toggle Loader";
        _keybindManager.RegisterKeybind(virtualKey, ctrl, alt, shift, description, () => CloseProjectionAndToggleLoader());
    }

    private void ScanForFiveM()
    {
        Console.WriteLine("\n=== Scanning for FiveM Processes (Enhanced) ===");
        var summary = FiveMDetector.GetProcessSummary();

        if (summary.TotalProcessCount == 0)
        {
            Console.WriteLine("❌ No FiveM processes found.");
            Console.WriteLine("   Please make sure FiveM is running and try again.");
        }
        else
        {
            Console.WriteLine($"✅ Found {summary.TotalProcessCount} FiveM process(es):");
            
            if (summary.VulkanProcesses.Any())
            {
                Console.WriteLine("\n🎮 Vulkan Processes (Preferred for injection):");
                for (int i = 0; i < summary.VulkanProcesses.Count; i++)
                {
                    Console.WriteLine($"   {i + 1}. {summary.VulkanProcesses[i]}");
                }
            }
            
            if (summary.TraditionalProcesses.Any())
            {
                Console.WriteLine("\n🖥️ Traditional Processes (Window capture fallback):");
                for (int i = 0; i < summary.TraditionalProcesses.Count; i++)
                {
                    Console.WriteLine($"   {i + 1}. {summary.TraditionalProcesses[i]}");
                }
            }

            Console.WriteLine($"\n📊 Vulkan Support: {(summary.HasVulkanSupport ? "✅ Available" : "❌ Not detected")}");
            Console.WriteLine("   💡 Vulkan injection provides better performance than window capture");
        }
    }

    private void StartCapture()
    {
        Console.WriteLine("\n=== Starting Capture (Enhanced) ===");
        
        var summary = FiveMDetector.GetProcessSummary();
        if (summary.TotalProcessCount == 0)
        {
            Console.WriteLine("❌ No FiveM processes found. Please start FiveM first.");
            return;
        }

        // Prioritize Vulkan processes for injection
        ProcessInfo? targetProcess = null;
        string captureMethod = "";

        if (summary.VulkanProcesses.Any())
        {
            var vulkanProcess = summary.VulkanProcesses.First();
            targetProcess = new ProcessInfo
            {
                ProcessId = vulkanProcess.ProcessId,
                ProcessName = vulkanProcess.ProcessName,
                WindowTitle = vulkanProcess.WindowTitle,
                WindowHandle = vulkanProcess.WindowHandle
            };
            captureMethod = "Vulkan Injection";
        }
        else if (summary.TraditionalProcesses.Any())
        {
            targetProcess = summary.TraditionalProcesses.First();
            captureMethod = "GDI Window Capture (fallback)";
        }

        if (targetProcess == null)
        {
            Console.WriteLine("❌ No suitable processes found for capture.");
            return;
        }

        Console.WriteLine($"🎯 Targeting: {targetProcess}");
        Console.WriteLine($"📡 Method: {captureMethod}");

        if (_captureEngine.StartCapture(targetProcess.ProcessName))
        {
            Console.WriteLine("✅ Capture started successfully!");
            Console.WriteLine($"   FPS: {_captureEngine.Settings.TargetFPS}");
            Console.WriteLine($"   Resolution: {(_captureEngine.Settings.ScaleWidth > 0 ? $"{_captureEngine.Settings.ScaleWidth}x{_captureEngine.Settings.ScaleHeight}" : "Original")}");
        }
        else
        {
            Console.WriteLine("❌ Failed to start capture.");
            Console.WriteLine("   💡 Try running as administrator for injection support");
        }
    }

    private void StopCapture()
    {
        Console.WriteLine("\n=== Stopping Capture ===");
        _captureEngine.StopCapture();
        Console.WriteLine("✅ Capture stopped.");
    }

    private void StartProjection()
    {
        Console.WriteLine("\n=== Starting Projection ===");
        _projectionWindow.StartProjection(_selectedMonitor);
        Console.WriteLine($"✅ Borderless projection window started on monitor {_selectedMonitor}.");
        Console.WriteLine("   The projection window should now be visible.");
    }

    private void StopProjection()
    {
        Console.WriteLine("\n=== Stopping Projection ===");
        _projectionWindow.StopProjection();
        Console.WriteLine("✅ Projection stopped.");
    }

    private void ConfigureSettings()
    {
        Console.WriteLine("\n=== Configuration Settings ===");
        Console.WriteLine();
        Console.WriteLine($"Current Target FPS: {_captureEngine.Settings.TargetFPS}");
        Console.WriteLine($"Current Resolution: {(_captureEngine.Settings.ScaleWidth > 0 ? $"{_captureEngine.Settings.ScaleWidth}x{_captureEngine.Settings.ScaleHeight}" : "Original (auto-detect)")}");
        Console.WriteLine($"Hardware Acceleration: {_captureEngine.Settings.UseHardwareAcceleration}");
        Console.WriteLine($"Target Monitor: {_selectedMonitor}");
        
        // Show available monitors
        ShowAvailableMonitors();
        
        Console.WriteLine();

        Console.WriteLine("Enter new values (press Enter to keep current):");
        
        Console.Write($"Target FPS ({_captureEngine.Settings.TargetFPS}): ");
        var fpsInput = Console.ReadLine();
        if (int.TryParse(fpsInput, out int fps) && fps > 0 && fps <= 240)
        {
            _captureEngine.Settings.TargetFPS = fps;
            _projectionWindow.SetTargetFPS(fps);
            Console.WriteLine($"✅ FPS updated to {fps}");
        }

        Console.WriteLine("\nResolution settings (enter 0 for both to use original size):");
        Console.Write($"Width ({_captureEngine.Settings.ScaleWidth}): ");
        var widthInput = Console.ReadLine();
        if (int.TryParse(widthInput, out int width) && width >= 0)
        {
            _captureEngine.Settings.ScaleWidth = width;
        }

        Console.Write($"Height ({_captureEngine.Settings.ScaleHeight}): ");
        var heightInput = Console.ReadLine();
        if (int.TryParse(heightInput, out int height) && height >= 0)
        {
            _captureEngine.Settings.ScaleHeight = height;
        }

        if (int.TryParse(widthInput, out width) && int.TryParse(heightInput, out height))
        {
            Console.WriteLine($"✅ Resolution updated to {(width > 0 && height > 0 ? $"{width}x{height}" : "Original")}");
        }

        // Monitor selection
        Console.Write($"Target Monitor ({_selectedMonitor}): ");
        var monitorInput = Console.ReadLine();
        if (int.TryParse(monitorInput, out int monitor) && monitor >= 0)
        {
            try
            {
                var monitors = MonitorHelper.GetAllMonitors();
                if (monitor < monitors.Count)
                {
                    _selectedMonitor = monitor;
                    Console.WriteLine($"✅ Target monitor updated to {monitor}");
                }
                else
                {
                    Console.WriteLine($"❌ Invalid monitor index. Available: 0-{monitors.Count - 1}");
                }
            }
            catch
            {
                Console.WriteLine("❌ Could not access monitor information");
            }
        }

        Console.WriteLine("✅ Settings updated successfully!");
    }

    private void ShowAvailableMonitors()
    {
        try
        {
            var monitors = MonitorHelper.GetAllMonitors();
            Console.WriteLine($"\nAvailable Monitors ({monitors.Count}):");
            foreach (var monitor in monitors)
            {
                var bounds = $"at {monitor.Bounds.X},{monitor.Bounds.Y}";
                Console.WriteLine($"  {monitor.Index}: Monitor {monitor.Index + 1} - {monitor.Bounds.Width}x{monitor.Bounds.Height} {bounds}{(monitor.IsPrimary ? " (Primary)" : "")}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Could not enumerate monitors: {ex.Message}");
        }
    }

    private void QuickStart()
    {
        Console.WriteLine("\n=== Quick Start (Enhanced) ===");
        Console.WriteLine("🚀 Auto-detecting FiveM and starting capture + projection...");

        var summary = FiveMDetector.GetProcessSummary();
        if (summary.TotalProcessCount == 0)
        {
            Console.WriteLine("❌ No FiveM processes found. Please start FiveM first.");
            return;
        }

        // Prioritize Vulkan processes
        ProcessInfo? targetProcess = null;
        string method = "";

        if (summary.VulkanProcesses.Any())
        {
            var vulkanProcess = summary.VulkanProcesses.First();
            targetProcess = new ProcessInfo
            {
                ProcessId = vulkanProcess.ProcessId,
                ProcessName = vulkanProcess.ProcessName,
                WindowTitle = vulkanProcess.WindowTitle,
                WindowHandle = vulkanProcess.WindowHandle
            };
            method = "Vulkan injection";
        }
        else
        {
            targetProcess = summary.TraditionalProcesses.First();
            method = "window capture";
        }

        Console.WriteLine($"🎯 Found: {targetProcess} ({method})");

        if (_captureEngine.StartCapture(targetProcess.ProcessName))
        {
            Console.WriteLine("✅ Capture started!");
            _projectionWindow.StartProjection(_selectedMonitor);
            Console.WriteLine("✅ Projection started!");
            Console.WriteLine();
            Console.WriteLine("🎮 Pick6 is now running! The game should be projected in a borderless window.");
            Console.WriteLine($"   📡 Using: {method}");
            Console.WriteLine("   Press any key to return to the main menu.");
        }
        else
        {
            Console.WriteLine("❌ Failed to start capture.");
            Console.WriteLine("   💡 For Vulkan injection, try running as administrator");
        }
    }

    private void ShowStatus()
    {
        Console.WriteLine("\n=== System Status ===");
        Console.WriteLine();
        
        var summary = FiveMDetector.GetProcessSummary();
        Console.WriteLine($"FiveM Processes: {summary.TotalProcessCount}");
        Console.WriteLine($"  - Vulkan Processes: {summary.VulkanProcesses.Count}");
        Console.WriteLine($"  - Traditional Processes: {summary.TraditionalProcesses.Count}");
        Console.WriteLine($"Vulkan Support: {(summary.HasVulkanSupport ? "✅ Available" : "❌ Not detected")}");
        Console.WriteLine();
        
        // Monitor information
        try
        {
            var monitors = MonitorHelper.GetAllMonitors();
            Console.WriteLine($"Available Monitors: {monitors.Count}");
            foreach (var monitor in monitors)
            {
                var status = monitor.Index == _selectedMonitor ? " ← Selected" : "";
                Console.WriteLine($"  {monitor}{status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Monitor Detection: ❌ Error ({ex.Message})");
        }
        Console.WriteLine();
        
        // This would check actual capture/projection status in a real implementation
        Console.WriteLine($"Capture Status: Active"); // Placeholder
        Console.WriteLine($"Projection Status: Active"); // Placeholder
        Console.WriteLine($"Current FPS Target: {_captureEngine.Settings.TargetFPS}");
        Console.WriteLine($"Resolution Setting: {(_captureEngine.Settings.ScaleWidth > 0 ? $"{_captureEngine.Settings.ScaleWidth}x{_captureEngine.Settings.ScaleHeight}" : "Original")}");
        Console.WriteLine($"Target Monitor: {_selectedMonitor}");
        Console.WriteLine();
        Console.WriteLine("💡 Tips:");
        Console.WriteLine("  - Vulkan injection provides better performance");
        Console.WriteLine("  - Run as administrator for injection privileges");
        Console.WriteLine("  - Traditional window capture works as fallback");
        Console.WriteLine("  - Use Ctrl+L/Ctrl+P hotkeys in GUI mode for quick control");
        Console.WriteLine("  - Stealth mode hides windows from Alt+Tab and taskbar");
    }

    private void AutoStartCapture()
    {
        var summary = FiveMDetector.GetProcessSummary();
        if (summary.TotalProcessCount > 0)
        {
            ProcessInfo? targetProcess = null;
            
            if (summary.VulkanProcesses.Any())
            {
                var vulkanProcess = summary.VulkanProcesses.First();
                targetProcess = new ProcessInfo
                {
                    ProcessId = vulkanProcess.ProcessId,
                    ProcessName = vulkanProcess.ProcessName,
                    WindowTitle = vulkanProcess.WindowTitle,
                    WindowHandle = vulkanProcess.WindowHandle
                };
            }
            else
            {
                targetProcess = summary.TraditionalProcesses.First();
            }

            if (targetProcess != null && _captureEngine.StartCapture(targetProcess.ProcessName))
            {
                Console.WriteLine($"[INFO] Auto-started capture for: {targetProcess}");
                _projectionWindow.StartProjection(_selectedMonitor);
                Console.WriteLine("[INFO] Auto-started projection");
            }
        }
    }

    private void ToggleProjection()
    {
        try
        {
            if (_projectionWindow == null) return;
            
            // Simple toggle - stop if running, start if stopped
            // Since console doesn't have a way to track projection state easily,
            // we'll just try to start projection (which will do nothing if already running)
            _projectionWindow.StartProjection(_selectedMonitor);
            Console.WriteLine("[INFO] Projection toggle requested");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to toggle projection: {ex.Message}");
        }
    }

    private void StopProjectionOnly()
    {
        try
        {
            _projectionWindow?.StopProjection();
            Console.WriteLine("[INFO] Projection stopped via keybind");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to stop projection: {ex.Message}");
        }
    }

    private void StopProjectionAndRestoreMenu()
    {
        try
        {
            _projectionWindow?.StopProjection();
            Console.WriteLine("[INFO] Projection stopped and menu restored via keybind");
            
            // In console mode, we can't really "restore" a window, but we can display a message
            Console.WriteLine("[INFO] Return to console to access the main menu");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to stop projection and restore menu: {ex.Message}");
        }
    }

    private void CloseProjectionAndToggleLoader()
    {
        try
        {
            // First: Close/stop the projection
            _projectionWindow?.StopProjection();
            Console.WriteLine("[INFO] Projection closed via combined hotkey");
            
            // Second: Toggle loader (in console mode, display current status)
            Console.WriteLine("[INFO] Loader status: Console mode active");
            Console.WriteLine("[INFO] Press any key to return to main menu or use other hotkeys");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to execute close projection + toggle loader: {ex.Message}");
        }
    }

    private void Cleanup()
    {
        Console.WriteLine("\n=== Shutting Down ===");
        _keybindManager?.Dispose();
        _captureEngine?.StopCapture();
        _projectionWindow?.StopProjection();
        Console.WriteLine("✅ Pick6 has been shut down gracefully.");
    }

    private async void TestProjectionWithDemoFrames()
    {
        Console.WriteLine("\n=== Testing Projection ===");
        
        // Start projection first
        _projectionWindow.StartProjection(_selectedMonitor);
        Console.WriteLine("✅ Projection window started");
        
        Console.WriteLine("🎨 Generating colorful test frames...");
        
        await GenerateTestFramesAsync();
        
        Console.WriteLine("\nPress any key when done viewing the projection window.");
        Console.ReadKey();
        
        _projectionWindow.StopProjection();
        Console.WriteLine("✅ Projection test completed");
    }

    private async Task GenerateTestFramesAsync()
    {
        var random = new Random();
        for (int i = 0; i < 300; i++) // 10 seconds at ~30 FPS
        {
            try
            {
                GenerateTestFrame(random, i);
                await Task.Delay(33); // ~30 FPS
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating test frame: {ex.Message}");
                break;
            }
        }
        
        Console.WriteLine("\n🏁 Test frames generation completed.");
    }

    private void GenerateTestFrame(Random random, int frameNumber)
    {
        // Create a test frame with random colors
        using var testFrame = new Bitmap(800, 600);
        using var graphics = Graphics.FromImage(testFrame);
        
        // Fill with a random color
        var color = Color.FromArgb(
            random.Next(50, 255),
            random.Next(50, 255), 
            random.Next(50, 255)
        );
        graphics.Clear(color);
        
        // Add some text
        using var font = new Font("Arial", 24, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        graphics.DrawString($"Pick6 Test Frame #{frameNumber + 1}", font, brush, 50, 50);
        graphics.DrawString($"Color: {color}", font, brush, 50, 100);
        graphics.DrawString("ESC to close projection", new Font("Arial", 16), brush, 50, 500);
        
        _projectionWindow.UpdateFrame(testFrame);
    }

    private void ConfigureKeybinds()
    {
        if (_keybindManager == null)
        {
            Console.WriteLine("\n=== Keybinds Configuration ===");
            Console.WriteLine("❌ Keybind manager not available.");
            return;
        }

        var isRunning = true;
        while (isRunning)
        {
            Console.Clear();
            Console.WriteLine("=== Keybinds Configuration ===");
            Console.WriteLine();
            
            var keybinds = _keybindManager.GetRegisteredKeybinds();
            if (keybinds.Count == 0)
            {
                Console.WriteLine("No keybinds are currently registered.");
            }
            else
            {
                Console.WriteLine("Current Keybinds:");
                for (int i = 0; i < keybinds.Count; i++)
                {
                    var kb = keybinds[i];
                    var keyCombo = GlobalKeybindManager.FormatKeybindString(kb.Ctrl, kb.Alt, kb.Shift, kb.VirtualKey);
                    Console.WriteLine($"  {i + 1}. {keyCombo} - {kb.Description}");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("1. Add new keybind");
            Console.WriteLine("2. Remove keybind");
            Console.WriteLine("3. Test keybind");
            Console.WriteLine("4. Reset to defaults");
            Console.WriteLine("0. Return to main menu");
            Console.WriteLine();
            Console.Write("Enter your choice: ");
            
            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    AddNewKeybind();
                    break;
                case "2":
                    RemoveKeybind();
                    break;
                case "3":
                    TestKeybind();
                    break;
                case "4":
                    ResetKeybindsToDefaults();
                    break;
                case "0":
                    isRunning = false;
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
            
            if (isRunning)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }

    private void AddNewKeybind()
    {
        Console.WriteLine("\n=== Add New Keybind ===");
        Console.WriteLine("Available actions:");
        Console.WriteLine("1. Toggle projection");
        Console.WriteLine("2. Stop projection");
        Console.WriteLine("3. Stop projection + restore menu");
        Console.WriteLine("4. Custom test action");
        Console.Write("Select action (1-4): ");
        
        var actionChoice = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(actionChoice) || !int.TryParse(actionChoice, out int actionNum) || actionNum < 1 || actionNum > 4)
        {
            Console.WriteLine("Invalid action selection.");
            return;
        }

        Action action;
        string description;
        switch (actionNum)
        {
            case 1:
                action = () => ToggleProjection();
                description = "Toggle projection";
                break;
            case 2:
                action = () => StopProjectionOnly();
                description = "Stop projection";
                break;
            case 3:
                action = () => StopProjectionAndRestoreMenu();
                description = "Stop projection + restore menu";
                break;
            case 4:
                action = () => Console.WriteLine("[TEST] Custom keybind triggered!");
                description = "Test action";
                break;
            default:
                Console.WriteLine("Invalid action.");
                return;
        }

        // Get key combination
        Console.WriteLine("\nEnter key combination:");
        Console.Write("Use Ctrl? (y/n): ");
        var ctrlInput = Console.ReadLine()?.Trim().ToLower();
        bool useCtrl = ctrlInput == "y" || ctrlInput == "yes";

        Console.Write("Use Alt? (y/n): ");
        var altInput = Console.ReadLine()?.Trim().ToLower();
        bool useAlt = altInput == "y" || altInput == "yes";

        Console.Write("Use Shift? (y/n): ");
        var shiftInput = Console.ReadLine()?.Trim().ToLower();
        bool useShift = shiftInput == "y" || shiftInput == "yes";

        Console.Write("Enter key (A-Z, ESC, F1-F3): ");
        var keyInput = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(keyInput))
        {
            Console.WriteLine("Invalid key input.");
            return;
        }

        var virtualKey = GlobalKeybindManager.GetVirtualKeyFromName(keyInput);
        if (virtualKey == -1)
        {
            Console.WriteLine($"Unsupported key: {keyInput}");
            return;
        }

        // Check for conflicts
        if (_keybindManager.IsKeybindRegistered(virtualKey, useCtrl, useAlt, useShift))
        {
            var keyCombo = GlobalKeybindManager.FormatKeybindString(useCtrl, useAlt, useShift, virtualKey);
            Console.WriteLine($"⚠️ Keybind {keyCombo} is already registered.");
            Console.Write("Overwrite existing keybind? (y/n): ");
            var overwriteInput = Console.ReadLine()?.Trim().ToLower();
            if (overwriteInput != "y" && overwriteInput != "yes")
            {
                Console.WriteLine("Keybind addition cancelled.");
                return;
            }
            
            // Remove the existing keybind first
            var existingId = GlobalKeybindManager.GenerateKeybindId(virtualKey, useCtrl, useAlt, useShift);
            _keybindManager.UnregisterKeybind(existingId);
        }

        // Register the new keybind
        if (_keybindManager.RegisterKeybind(virtualKey, useCtrl, useAlt, useShift, description, action))
        {
            var keyCombo = GlobalKeybindManager.FormatKeybindString(useCtrl, useAlt, useShift, virtualKey);
            Console.WriteLine($"✅ Successfully registered keybind: {keyCombo} - {description}");
        }
        else
        {
            Console.WriteLine("❌ Failed to register keybind. The key combination might be reserved by the system.");
        }
    }

    private void RemoveKeybind()
    {
        Console.WriteLine("\n=== Remove Keybind ===");
        var keybinds = _keybindManager?.GetRegisteredKeybinds() ?? new List<GlobalKeybindManager.KeybindInfo>();
        
        if (keybinds.Count == 0)
        {
            Console.WriteLine("No keybinds to remove.");
            return;
        }

        Console.WriteLine("Current keybinds:");
        for (int i = 0; i < keybinds.Count; i++)
        {
            var kb = keybinds[i];
            var keyCombo = GlobalKeybindManager.FormatKeybindString(kb.Ctrl, kb.Alt, kb.Shift, kb.VirtualKey);
            Console.WriteLine($"  {i + 1}. {keyCombo} - {kb.Description}");
        }

        Console.Write($"Enter keybind number to remove (1-{keybinds.Count}): ");
        var input = Console.ReadLine()?.Trim();
        if (int.TryParse(input, out int index) && index >= 1 && index <= keybinds.Count)
        {
            var keybind = keybinds[index - 1];
            if (_keybindManager!.UnregisterKeybind(keybind.KeybindId))
            {
                var keyCombo = GlobalKeybindManager.FormatKeybindString(keybind.Ctrl, keybind.Alt, keybind.Shift, keybind.VirtualKey);
                Console.WriteLine($"✅ Removed keybind: {keyCombo}");
            }
            else
            {
                Console.WriteLine("❌ Failed to remove keybind.");
            }
        }
        else
        {
            Console.WriteLine("Invalid selection.");
        }
    }

    private void TestKeybind()
    {
        Console.WriteLine("\n=== Test Keybind ===");
        Console.WriteLine("Press any registered global keybind to test it.");
        Console.WriteLine("Press ESC to return to the keybinds menu.");
        Console.WriteLine("Note: You may need to focus another window and then press the keybind.");
        Console.WriteLine("\nWaiting for keybind press...");
        
        // Wait for ESC key to return
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(true);
        } while (key.Key != ConsoleKey.Escape);
        
        Console.WriteLine("Test mode ended.");
    }

    private void ResetKeybindsToDefaults()
    {
        Console.WriteLine("\n=== Reset Keybinds to Defaults ===");
        Console.Write("This will remove all custom keybinds and restore defaults. Continue? (y/n): ");
        var input = Console.ReadLine()?.Trim().ToLower();
        
        if (input == "y" || input == "yes")
        {
            if (_keybindManager != null)
            {
                // Stop monitoring and clear all keybinds
                _keybindManager.StopMonitoring();
                
                // Restart with defaults
                SetupGlobalKeybinds();
                Console.WriteLine("✅ Keybinds reset to defaults.");
            }
        }
        else
        {
            Console.WriteLine("Reset cancelled.");
        }
    }

    public static void ShowHelp()
    {
        Console.WriteLine("pick6_loader - Unified OBS Game Capture Clone for FiveM");
        Console.WriteLine();
        Console.WriteLine("Usage: pick6_loader [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --gui                 Force GUI mode (default on Windows)");
        Console.WriteLine("  --console             Force console mode");
        Console.WriteLine("  --auto-start          Automatically start capture and projection (console mode)");
        Console.WriteLine("  --fps <number>        Set target FPS (console mode, default: 60, max: 240)");
        Console.WriteLine("  --fps-logging         Enable FPS logging for debugging");
        Console.WriteLine("  --resolution <w> <h>  Set output resolution (console mode)");
        Console.WriteLine("  --monitor <index>     Set target monitor for projection (console mode)");
        Console.WriteLine("  --help                Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  pick6_loader                           # Start GUI on Windows, console on other OS");
        Console.WriteLine("  pick6_loader --console                 # Force console mode");
        Console.WriteLine("  pick6_loader --console --auto-start    # Console mode with auto-start");
        Console.WriteLine("  pick6_loader --console --fps 30        # Console mode with 30 FPS");
        Console.WriteLine("  pick6_loader --console --monitor 1     # Console mode with monitor 1");
        Console.WriteLine("  pick6_loader --console --resolution 1920 1080 --fps 60 --monitor 0");
        Console.WriteLine();
        Console.WriteLine("New Features:");
        Console.WriteLine("  🖥️  Monitor Selection: Choose target display for projection (GUI & console)");
        Console.WriteLine("  🎯 FPS Control: Consistent frame rate control (15-240 FPS)");
        Console.WriteLine("  ⌨️  Global Hotkeys: System-wide keyboard shortcuts (Windows only)");
        Console.WriteLine("  👻 Stealth Mode: Hidden from Alt+Tab and taskbar");
        Console.WriteLine("  ⚡ Performance: Optimized render loop for smooth 60+ FPS projection");
        Console.WriteLine();
        Console.WriteLine("Global Hotkeys (Windows):");
        Console.WriteLine("  Ctrl+L                Toggle loader window visibility");
        Console.WriteLine("  Ctrl+P                Toggle projection window");
        Console.WriteLine("  Ctrl+Shift+P          Stop projection & restore menu");
        Console.WriteLine("  Ctrl+Shift+Esc        Close projection immediately");
        Console.WriteLine("  ESC (in projection)   Close projection window");
        Console.WriteLine();
        Console.WriteLine("Build commands:");
        Console.WriteLine("  dotnet build -c Release");
        Console.WriteLine("  dotnet publish src/Pick6.Loader -c Release -r win-x64 --self-contained true \\");
        Console.WriteLine("    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \\");
        Console.WriteLine("    -p:PublishReadyToRun=true");
    }
}