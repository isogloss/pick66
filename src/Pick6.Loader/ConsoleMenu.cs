using Pick6.Core;
using Pick6.Core.Util;
using Pick6.Projection;
using System.Drawing;

namespace Pick6.Loader;

/// <summary>
/// Logging levels for Pick6
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

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

    /// <summary>
    /// Log message with level filtering
    /// </summary>
    private void Log(string message, LogLevel level)
    {
        if (level < _logLevel) return;

        string prefix = level switch
        {
            LogLevel.Debug => "[DEBUG]",
            LogLevel.Info => "[INFO]",
            LogLevel.Warning => "[WARNING]",
            LogLevel.Error => "[ERROR]",
            _ => "[INFO]"
        };

        Console.WriteLine($"{prefix} {message}");
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

        // Handle check-updates-only mode - exit after checking
        if (_checkUpdatesOnly)
        {
            Log("Check updates only mode - exiting after update check", LogLevel.Info);
            return;
        }

        // Handle manual update check
        if (_checkUpdates)
        {
            Log("Performing manual update check...", LogLevel.Info);
            // Note: Actual update checking would be handled in the main Program.cs
            // This is just for command line parsing
        }

        // Auto-start behavior - skip interactive menu by default unless --interactive flag present
        bool useInteractiveMenu = args.Any(arg => arg.ToLower() == "--interactive");
        
        if (!useInteractiveMenu && args.Length > 0)
        {
            // Auto-mode: start capture and projection automatically
            Log("Running in auto-start mode (non-interactive)", LogLevel.Info);
            AutoStartCapture();
            
            // Keep running until Ctrl+C
            while (isRunning)
            {
                Thread.Sleep(1000);
                
                // Optional: Log FPS stats in debug mode
                if (_logLevel <= LogLevel.Debug)
                {
                    LogFpsStats();
                }
            }
        }
        else
        {
            // Traditional interactive menu mode
            Log("Running in interactive menu mode", LogLevel.Info);
            RunMainLoop(ref isRunning);
        }

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

    private bool _autoMode = false;
    private bool _noProjection = false;
    private LogLevel _logLevel = LogLevel.Info;
    private bool _checkUpdatesOnly = false;
    private bool _checkUpdates = false;

    private void HandleCommandLineArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--auto-start":
                    _autoMode = true;
                    AutoStartCapture();
                    break;
                case "--fps":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int fps))
                    {
                        // Remove 120 FPS ceiling, set reasonable upper limit to prevent runaway CPU
                        if (fps > 0 && fps <= 600)
                        {
                            _captureEngine.Settings.TargetFPS = fps;
                            Log($"Target FPS set to {fps}", LogLevel.Info);
                        }
                        else
                        {
                            Log($"Invalid FPS value {fps}. Must be between 1 and 600.", LogLevel.Warning);
                        }
                        i++;
                    }
                    break;
                case "--fps-logging":
                    _projectionWindow.SetFpsLogging(true);
                    Log("FPS logging enabled", LogLevel.Info);
                    break;
                case "--resolution":
                    if (i + 2 < args.Length && 
                        int.TryParse(args[i + 1], out int width) &&
                        int.TryParse(args[i + 2], out int height))
                    {
                        _captureEngine.Settings.ScaleWidth = width;
                        _captureEngine.Settings.ScaleHeight = height;
                        Log($"Resolution set to {width}x{height}", LogLevel.Info);
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
                            Log($"Target monitor set to {monitor}", LogLevel.Info);
                        }
                        else
                        {
                            Log($"Invalid monitor {monitor}. Available: 0-{monitors.Count - 1}", LogLevel.Warning);
                        }
                        i++;
                    }
                    break;
                case "--no-projection":
                    _noProjection = true;
                    Log("Projection disabled", LogLevel.Info);
                    break;
                case "--log-level":
                    if (i + 1 < args.Length && Enum.TryParse<LogLevel>(args[i + 1], true, out var logLevel))
                    {
                        _logLevel = logLevel;
                        Log($"Log level set to {logLevel}", LogLevel.Info);
                        i++;
                    }
                    else if (i + 1 < args.Length)
                    {
                        Log($"Invalid log level '{args[i + 1]}'. Valid values: Debug, Info, Warning, Error", LogLevel.Warning);
                        i++;
                    }
                    break;
                case "--check-updates-only":
                    _checkUpdatesOnly = true;
                    Log("Check updates only mode enabled", LogLevel.Info);
                    break;
                case "--check-updates":
                    _checkUpdates = true;
                    Log("Manual update check requested", LogLevel.Info);
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

            switch (choice?.ToLower())
            {
                // A. Capture Settings
                case "1": ScanForFiveM(); break;
                case "2": StartCapture(); break;
                case "3": StopCapture(); break;
                case "4": ConfigureFPS(); break;
                case "5": ConfigureResolution(); break;
                case "6": ToggleHardwareAcceleration(); break;
                
                // B. Projection
                case "7": StartProjection(); break;
                case "8": StopProjection(); break;
                case "9": ToggleMatchCaptureFPS(); break;
                case "10": ConfigureProjectionFPS(); break;
                case "11": SelectMonitor(); break;
                case "12": TestProjectionWithDemoFrames(); break;
                
                // C. Performance & Diagnostics
                case "13": ShowLiveStatistics(); break;
                case "14": ShowPerformanceWarnings(); break;
                case "15": ToggleStatsLogging(); break;
                case "16": DumpDiagnostics(); break;
                
                // D. Output / Quality (TODO placeholders)
                case "17": Console.WriteLine("Quality presets - Coming soon!"); break;
                case "18": Console.WriteLine("Bitrate config - Coming soon!"); break;
                case "19": Console.WriteLine("Output format - Coming soon!"); break;
                case "20": Console.WriteLine("Recording - Coming soon!"); break;
                
                // E. Injection & Process
                case "21": ScanForFiveM(); break; // Rescan = same as scan
                case "22": ForceReinjection(); break;
                case "23": ShowLastInjectionMethod(); break;
                case "24": Console.WriteLine("Process priority - Coming soon!"); break;
                
                // F. System
                case "k": case "keybinds": ConfigureKeybinds(); break;
                case "h": case "help": ShowHelpEnhanced(); break;
                case "q": case "quick": QuickStart(); break;
                case "0": case "exit": case "quit": isRunning = false; break;
                
                default:
                    Console.WriteLine("Invalid choice. Press 'H' for help or try again.");
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
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                   PICK6                                      ║");
        Console.WriteLine("║                          Enhanced Console Interface                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Show current status summary
        var captureActive = _captureEngine != null && _captureEngine.Statistics.TotalFrames > 0;
        var projectionActive = _projectionWindow?.IsProjecting ?? false;
        
        Console.WriteLine($"Status: Capture {(captureActive ? "🟢 Active" : "🔴 Inactive")} | " +
                         $"Projection {(projectionActive ? "🟢 Active" : "🔴 Inactive")}");
        if (captureActive)
        {
            Console.WriteLine($"        {_captureEngine.Statistics.GetSummary()}");
        }
        Console.WriteLine();

        Console.WriteLine("┌─ A. Capture Settings ────────────────────────────────────────────────────────┐");
        Console.WriteLine("│  1. Scan for FiveM processes    2. Start capture       3. Stop capture      │");
        Console.WriteLine("│  4. Configure FPS (30/60/120)   5. Set resolution      6. Hardware accel    │");
        Console.WriteLine("└───────────────────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();
        
        Console.WriteLine("┌─ B. Projection ───────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│  7. Start projection             8. Stop projection                           │");
        Console.WriteLine("│  9. Toggle match capture FPS     10. Set projection FPS                      │");
        Console.WriteLine("│  11. Select monitor              12. Test projection                          │");
        Console.WriteLine("└───────────────────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();
        
        Console.WriteLine("┌─ C. Performance & Diagnostics ───────────────────────────────────────────────┐");
        Console.WriteLine("│  13. Show live statistics       14. Performance warnings                     │");
        Console.WriteLine("│  15. Enable/disable stats log   16. Dump diagnostics                         │");
        Console.WriteLine("└───────────────────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();
        
        Console.WriteLine("┌─ D. Output / Quality ─────────────────────────────────────────────────────────┐");
        Console.WriteLine("│  17. Quality presets (TODO)     18. Bitrate config (TODO)                   │");
        Console.WriteLine("│  19. Output format (TODO)       20. Recording (TODO)                         │");
        Console.WriteLine("└───────────────────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();
        
        Console.WriteLine("┌─ E. Injection & Process ──────────────────────────────────────────────────────┐");
        Console.WriteLine("│  21. Rescan FiveM                22. Force reinjection                        │");
        Console.WriteLine("│  23. Show last injection method  24. Process priority (TODO)                 │");
        Console.WriteLine("└───────────────────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();
        
        Console.WriteLine("┌─ F. System ───────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│  K. Configure keybinds           H. Help                                      │");
        Console.WriteLine("│  Q. Quick start                  0. Exit                                      │");
        Console.WriteLine("└───────────────────────────────────────────────────────────────────────────────┘");
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
        
        using var spinner = new Spinner("Scanning for FiveM processes...");
        spinner.Start();
        
        // Simulate scanning delay for demonstration
        Thread.Sleep(500);
        
        var summary = FiveMDetector.GetProcessSummary();

        if (summary.TotalProcessCount == 0)
        {
            spinner.Fail("No FiveM processes found");
            Console.WriteLine("   Please make sure FiveM is running and try again.");
        }
        else
        {
            spinner.Success($"Found {summary.TotalProcessCount} FiveM process(es)");
            
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
        
        using var scanSpinner = new Spinner("Scanning for FiveM processes...");
        scanSpinner.Start();
        
        var summary = FiveMDetector.GetProcessSummary();
        if (summary.TotalProcessCount == 0)
        {
            scanSpinner.Fail("No FiveM processes found");
            Console.WriteLine("   Please start FiveM first.");
            return;
        }
        
        scanSpinner.Success($"Found {summary.TotalProcessCount} process(es)");

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

        using var injectionSpinner = new Spinner($"Injecting into {targetProcess.ProcessName}...");
        injectionSpinner.Start();

        if (_captureEngine.StartCapture(targetProcess.ProcessName))
        {
            injectionSpinner.Success($"Capture started successfully - {captureMethod}");
            Console.WriteLine($"   FPS: {_captureEngine.Settings.TargetFPS}");
            Console.WriteLine($"   Resolution: {(_captureEngine.Settings.ScaleWidth > 0 ? $"{_captureEngine.Settings.ScaleWidth}x{_captureEngine.Settings.ScaleHeight}" : "Original")}");
        }
        else
        {
            injectionSpinner.Fail("Failed to start capture");
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
        if (int.TryParse(fpsInput, out int fps) && fps > 0 && fps <= 600)
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
                Log($"Auto-started capture for: {targetProcess}", LogLevel.Info);
                
                if (!_noProjection)
                {
                    _projectionWindow.StartProjection(_selectedMonitor);
                    Log("Auto-started projection", LogLevel.Info);
                }
                else
                {
                    Log("Projection disabled by --no-projection flag", LogLevel.Info);
                }
            }
        }
        else
        {
            Log("No FiveM processes found for auto-start", LogLevel.Warning);
        }
    }

    private DateTime _lastFpsLog = DateTime.Now;
    private int _frameCount = 0;

    /// <summary>
    /// Log FPS statistics for debug mode
    /// </summary>
    private void LogFpsStats()
    {
        _frameCount++;
        var now = DateTime.Now;
        var elapsed = now - _lastFpsLog;
        
        if (elapsed.TotalSeconds >= 5.0) // Log every 5 seconds
        {
            var avgFps = _frameCount / elapsed.TotalSeconds;
            Log($"Average FPS: {avgFps:F1} (target: {_captureEngine.Settings.TargetFPS})", LogLevel.Debug);
            
            _frameCount = 0;
            _lastFpsLog = now;
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

    #region Enhanced Menu Methods

    /// <summary>
    /// Configure FPS with preset options
    /// </summary>
    private void ConfigureFPS()
    {
        Console.WriteLine("\n=== Configure Target FPS ===");
        Console.WriteLine($"Current FPS: {_captureEngine.Settings.TargetFPS}");
        Console.WriteLine();
        Console.WriteLine("1. 30 FPS (Low performance/battery saving)");
        Console.WriteLine("2. 60 FPS (Standard)");
        Console.WriteLine("3. 120 FPS (High performance)");
        Console.WriteLine("4. 144 FPS (Gaming monitors)");
        Console.WriteLine("5. Custom FPS");
        Console.WriteLine("0. Back");
        Console.Write("Choice: ");

        var choice = Console.ReadLine()?.Trim();
        int newFPS = _captureEngine.Settings.TargetFPS;

        switch (choice)
        {
            case "1": newFPS = 30; break;
            case "2": newFPS = 60; break;
            case "3": newFPS = 120; break;
            case "4": newFPS = 144; break;
            case "5":
                Console.Write("Enter custom FPS (15-240): ");
                if (int.TryParse(Console.ReadLine(), out int customFPS))
                {
                    newFPS = Math.Max(15, Math.Min(240, customFPS));
                }
                break;
            case "0": return;
        }

        if (newFPS != _captureEngine.Settings.TargetFPS)
        {
            _captureEngine.Settings.TargetFPS = newFPS;
            Console.WriteLine($"✅ FPS updated to {newFPS}");
            
            // Also update projection FPS if it's running
            if (_projectionWindow.IsProjecting)
            {
                _projectionWindow.SetTargetFPS(newFPS);
                Console.WriteLine($"   Projection FPS also updated to {newFPS}");
            }
        }
    }

    /// <summary>
    /// Configure capture resolution
    /// </summary>
    private void ConfigureResolution()
    {
        Console.WriteLine("\n=== Configure Resolution ===");
        Console.WriteLine($"Current: {(_captureEngine.Settings.ScaleWidth > 0 ? $"{_captureEngine.Settings.ScaleWidth}x{_captureEngine.Settings.ScaleHeight}" : "Original (no scaling)")}");
        Console.WriteLine();
        Console.WriteLine("1. Original (no scaling)");
        Console.WriteLine("2. 1920x1080 (1080p)");
        Console.WriteLine("3. 1280x720 (720p)");
        Console.WriteLine("4. 2560x1440 (1440p)");
        Console.WriteLine("5. Custom resolution");
        Console.WriteLine("0. Back");
        Console.Write("Choice: ");

        var choice = Console.ReadLine()?.Trim();

        switch (choice)
        {
            case "1":
                _captureEngine.Settings.ScaleWidth = 0;
                _captureEngine.Settings.ScaleHeight = 0;
                Console.WriteLine("✅ Resolution set to original (no scaling)");
                break;
            case "2":
                _captureEngine.Settings.ScaleWidth = 1920;
                _captureEngine.Settings.ScaleHeight = 1080;
                Console.WriteLine("✅ Resolution set to 1920x1080");
                break;
            case "3":
                _captureEngine.Settings.ScaleWidth = 1280;
                _captureEngine.Settings.ScaleHeight = 720;
                Console.WriteLine("✅ Resolution set to 1280x720");
                break;
            case "4":
                _captureEngine.Settings.ScaleWidth = 2560;
                _captureEngine.Settings.ScaleHeight = 1440;
                Console.WriteLine("✅ Resolution set to 2560x1440");
                break;
            case "5":
                Console.Write("Enter width: ");
                if (int.TryParse(Console.ReadLine(), out int width) && width > 0)
                {
                    Console.Write("Enter height: ");
                    if (int.TryParse(Console.ReadLine(), out int height) && height > 0)
                    {
                        _captureEngine.Settings.ScaleWidth = width;
                        _captureEngine.Settings.ScaleHeight = height;
                        Console.WriteLine($"✅ Resolution set to {width}x{height}");
                    }
                }
                break;
            case "0": return;
        }
    }

    /// <summary>
    /// Toggle hardware acceleration
    /// </summary>
    private void ToggleHardwareAcceleration()
    {
        _captureEngine.Settings.UseHardwareAcceleration = !_captureEngine.Settings.UseHardwareAcceleration;
        var status = _captureEngine.Settings.UseHardwareAcceleration ? "enabled" : "disabled";
        Console.WriteLine($"✅ Hardware acceleration {status}");
    }

    /// <summary>
    /// Toggle match capture FPS mode
    /// </summary>
    private void ToggleMatchCaptureFPS()
    {
        // This would need to be implemented in the projection window
        Console.WriteLine("✅ Match capture FPS toggled (TODO: implement in BorderlessProjectionWindow)");
    }

    /// <summary>
    /// Configure projection FPS
    /// </summary>
    private void ConfigureProjectionFPS()
    {
        Console.WriteLine("\n=== Configure Projection FPS ===");
        Console.Write("Enter projection FPS (15-240): ");
        if (int.TryParse(Console.ReadLine(), out int fps) && fps >= 15 && fps <= 240)
        {
            _projectionWindow.SetTargetFPS(fps);
            Console.WriteLine($"✅ Projection FPS set to {fps}");
        }
        else
        {
            Console.WriteLine("❌ Invalid FPS. Must be between 15 and 240.");
        }
    }

    /// <summary>
    /// Select monitor for projection
    /// </summary>
    private void SelectMonitor()
    {
        ShowAvailableMonitors();
        Console.Write($"Select monitor (0-{MonitorHelper.GetAllMonitors().Count - 1}): ");
        if (int.TryParse(Console.ReadLine(), out int monitorIndex))
        {
            _selectedMonitor = monitorIndex;
            Console.WriteLine($"✅ Monitor {monitorIndex} selected");
        }
    }

    /// <summary>
    /// Show live statistics
    /// </summary>
    private void ShowLiveStatistics()
    {
        Console.WriteLine("\n=== Live Performance Statistics ===");
        Console.WriteLine("Press any key to stop monitoring...");
        Console.WriteLine();

        var startTime = DateTime.Now;
        
        while (!Console.KeyAvailable)
        {
            // Clear previous output
            Console.SetCursorPosition(0, Console.CursorTop - 4);
            
            // Show capture statistics
            Console.WriteLine($"Capture:    {_captureEngine.Statistics.GetSummary()}                    ");
            
            // Show projection statistics if available and active
            var projectionStats = "Projection: Not active                                            ";
            if (_projectionWindow.IsProjecting)
            {
                // Note: BorderlessProjectionWindow doesn't directly expose stats,
                // this is a limitation of the current architecture
                projectionStats = "Projection: Active (stats not available via current interface)";
            }
            Console.WriteLine(projectionStats);
            
            var elapsed = DateTime.Now - startTime;
            Console.WriteLine($"Uptime:     {elapsed:hh\\:mm\\:ss}                                               ");
            Console.WriteLine($"Memory:     {GC.GetTotalMemory(false) / 1024 / 1024:F1} MB                               ");
            
            Thread.Sleep(100); // Update 10 times per second
        }
        
        Console.ReadKey(); // Consume the key press
        Console.WriteLine("Statistics monitoring stopped.");
    }

    /// <summary>
    /// Show performance warnings and recommendations
    /// </summary>
    private void ShowPerformanceWarnings()
    {
        Console.WriteLine("\n=== Performance Analysis ===");
        
        var captureStats = _captureEngine.Statistics;
        var hasWarnings = false;
        
        // Check capture performance
        if (captureStats.TotalFrames > 180) // At least 3 seconds of data
        {
            var targetFPS = _captureEngine.Settings.TargetFPS;
            var avgFPS = captureStats.AverageFps;
            
            if (avgFPS < targetFPS * 0.7)
            {
                hasWarnings = true;
                Console.WriteLine($"⚠️  Capture FPS Warning: Average {avgFPS:F1} FPS is significantly below target {targetFPS} FPS");
                Console.WriteLine("   Possible causes: CPU overload, insufficient memory, game blocking capture");
            }
            
            if (captureStats.P95FrameTimeMs > (1000.0 / targetFPS) * 2.0)
            {
                hasWarnings = true;
                Console.WriteLine($"⚠️  Frame Time Warning: 95th percentile frame time is {captureStats.P95FrameTimeMs:F1}ms (target: {1000.0/targetFPS:F1}ms)");
                Console.WriteLine("   This indicates inconsistent frame delivery");
            }
            
            if (captureStats.DropRate > 5.0)
            {
                hasWarnings = true;
                Console.WriteLine($"⚠️  Drop Rate Warning: {captureStats.DropRate:F1}% of frames are dropped");
                Console.WriteLine("   Consider reducing FPS or resolution");
            }
        }
        
        // Check memory usage
        var memoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
        if (memoryMB > 500)
        {
            hasWarnings = true;
            Console.WriteLine($"⚠️  Memory Usage Warning: {memoryMB:F1} MB allocated");
            Console.WriteLine("   High memory usage may indicate a memory leak or excessive frame buffering");
        }
        
        if (!hasWarnings)
        {
            Console.WriteLine("✅ No performance issues detected");
            Console.WriteLine($"   Capture: {captureStats.AverageFps:F1} avg FPS, {captureStats.DropRate:F1}% drop rate");
            Console.WriteLine($"   Memory: {memoryMB:F1} MB");
        }
        
        Console.WriteLine("\n💡 Performance Tips:");
        Console.WriteLine("   • Run as administrator for better injection support");
        Console.WriteLine("   • Close other applications to free up CPU/memory");
        Console.WriteLine("   • Lower FPS or resolution if performance is poor");
        Console.WriteLine("   • Use Vulkan injection when available (better than window capture)");
    }

    /// <summary>
    /// Toggle statistics logging
    /// </summary>
    private void ToggleStatsLogging()
    {
        var currentValue = Environment.GetEnvironmentVariable("PICK6_DIAG");
        var isEnabled = currentValue == "1";
        
        if (isEnabled)
        {
            Environment.SetEnvironmentVariable("PICK6_DIAG", "0");
            Console.WriteLine("✅ Diagnostic logging disabled");
        }
        else
        {
            Environment.SetEnvironmentVariable("PICK6_DIAG", "1");
            Console.WriteLine("✅ Diagnostic logging enabled");
            Console.WriteLine("   Frame timing logs will appear in console");
        }
    }

    /// <summary>
    /// Dump detailed diagnostics to file
    /// </summary>
    private void DumpDiagnostics()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var filename = $"pick6_diagnostics_{timestamp}.txt";
        
        try
        {
            using var writer = new StreamWriter(filename);
            writer.WriteLine("PICK6 Diagnostics Report");
            writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
            writer.WriteLine();
            
            writer.WriteLine("=== Capture Engine ===");
            writer.WriteLine($"Target FPS: {_captureEngine.Settings.TargetFPS}");
            writer.WriteLine($"Resolution: {(_captureEngine.Settings.ScaleWidth > 0 ? $"{_captureEngine.Settings.ScaleWidth}x{_captureEngine.Settings.ScaleHeight}" : "Original")}");
            writer.WriteLine($"Hardware Acceleration: {_captureEngine.Settings.UseHardwareAcceleration}");
            writer.WriteLine($"Statistics: {_captureEngine.Statistics.GetSummary()}");
            writer.WriteLine();
            
            writer.WriteLine("=== System Information ===");
            writer.WriteLine($"OS: {Environment.OSVersion}");
            writer.WriteLine($"CLR Version: {Environment.Version}");
            writer.WriteLine($"Working Set: {Environment.WorkingSet / 1024 / 1024} MB");
            writer.WriteLine($"GC Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
            writer.WriteLine($"Processor Count: {Environment.ProcessorCount}");
            writer.WriteLine();
            
            writer.WriteLine("=== FiveM Detection ===");
            var summary = FiveMDetector.GetProcessSummary();
            writer.WriteLine($"Total Processes: {summary.TotalProcessCount}");
            writer.WriteLine($"Vulkan Processes: {summary.VulkanProcesses.Count}");
            writer.WriteLine($"Traditional Processes: {summary.TraditionalProcesses.Count}");
            writer.WriteLine($"Vulkan Support: {summary.HasVulkanSupport}");
            writer.WriteLine();
            
            writer.WriteLine("=== Environment Variables ===");
            writer.WriteLine($"PICK6_DIAG: {Environment.GetEnvironmentVariable("PICK6_DIAG") ?? "not set"}");
            
            Console.WriteLine($"✅ Diagnostics saved to {filename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to save diagnostics: {ex.Message}");
        }
    }

    /// <summary>
    /// Force reinjection by restarting capture
    /// </summary>
    private void ForceReinjection()
    {
        Console.WriteLine("\n=== Force Reinjection ===");
        
        if (_captureEngine.Statistics.TotalFrames > 0)
        {
            Console.WriteLine("Stopping current capture...");
            _captureEngine.StopCapture();
            Thread.Sleep(500); // Brief delay
        }
        
        Console.WriteLine("Attempting reinjection...");
        StartCapture();
    }

    /// <summary>
    /// Show information about the last injection method used
    /// </summary>
    private void ShowLastInjectionMethod()
    {
        Console.WriteLine("\n=== Last Injection Method ===");
        
        var summary = FiveMDetector.GetProcessSummary();
        
        if (summary.TotalProcessCount == 0)
        {
            Console.WriteLine("❌ No FiveM processes currently detected");
            return;
        }
        
        string preferredMethod = summary.VulkanProcesses.Any() ? "Vulkan Injection" : "GDI Window Capture";
        var isActive = _captureEngine.Statistics.TotalFrames > 0;
        
        Console.WriteLine($"Preferred method: {preferredMethod}");
        Console.WriteLine($"Capture status: {(isActive ? "🟢 Active" : "🔴 Inactive")}");
        
        if (isActive)
        {
            Console.WriteLine($"Performance: {_captureEngine.Statistics.GetSummary()}");
        }
        
        Console.WriteLine();
        Console.WriteLine("Available processes:");
        if (summary.VulkanProcesses.Any())
        {
            Console.WriteLine("🎮 Vulkan processes (preferred):");
            foreach (var proc in summary.VulkanProcesses)
            {
                Console.WriteLine($"   • {proc}");
            }
        }
        
        if (summary.TraditionalProcesses.Any())
        {
            Console.WriteLine("🖥️ Traditional processes (fallback):");
            foreach (var proc in summary.TraditionalProcesses)
            {
                Console.WriteLine($"   • {proc}");
            }
        }
    }

    /// <summary>
    /// Show help information
    /// </summary>
    private void ShowHelpEnhanced()
    {
        Console.WriteLine("\n=== PICK6 Help ===");
        Console.WriteLine();
        Console.WriteLine("Quick Start:");
        Console.WriteLine("1. Start FiveM and join a server");
        Console.WriteLine("2. Run Pick6 and select option '2' (Start capture)");
        Console.WriteLine("3. Select option '7' (Start projection) to display the game");
        Console.WriteLine("4. Use option '13' to monitor performance in real-time");
        Console.WriteLine();
        Console.WriteLine("Menu Sections:");
        Console.WriteLine("A. Capture Settings  - Configure FiveM capture (FPS, resolution, hardware)");
        Console.WriteLine("B. Projection        - Control display window (start/stop, monitor selection)");
        Console.WriteLine("C. Diagnostics       - Monitor performance, view statistics and warnings");
        Console.WriteLine("D. Output/Quality    - Future encoding and recording features");
        Console.WriteLine("E. Injection         - Process management and reinjection");
        Console.WriteLine("F. System           - Keybinds, help, and utility functions");
        Console.WriteLine();
        Console.WriteLine("Performance Tips:");
        Console.WriteLine("• For best results, run as administrator");
        Console.WriteLine("• Vulkan injection provides better performance than window capture");
        Console.WriteLine("• Set PICK6_DIAG=1 environment variable for detailed frame timing logs");
        Console.WriteLine("• Use option '14' to check for performance warnings");
        Console.WriteLine("• Lower FPS (option '4') if you experience frame drops");
        Console.WriteLine();
        Console.WriteLine("Keybinds (Windows only):");
        Console.WriteLine("• Ctrl+P: Toggle projection");
        Console.WriteLine("• Ctrl+Shift+P: Stop projection and return to menu");
        Console.WriteLine("• Ctrl+L: Toggle loader window (GUI mode only)");
        Console.WriteLine("• See option 'K' to configure custom keybinds");
    }

    #endregion
}