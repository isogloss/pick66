using Pick66.Core;
using Pick66.Projection;
using System.Drawing;

namespace Pick66.Launcher;

/// <summary>
/// Main launcher for Pick66 OBS Game Capture clone
/// </summary>
public class Program
{
    private static GameCaptureEngine? _captureEngine;
    private static BorderlessProjectionWindow? _projectionWindow;
    private static bool _isRunning = true;

    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine("================================================");
        Console.WriteLine("        Pick66 - OBS Game Capture Clone        ");
        Console.WriteLine("         Real-time FiveM Projection            ");
        Console.WriteLine("================================================");
        Console.WriteLine();

        // Initialize components
        _captureEngine = new GameCaptureEngine();
        _projectionWindow = new BorderlessProjectionWindow();

        // Setup event handlers
        SetupEventHandlers();

        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            _isRunning = false;
        };

        // Check for command line arguments
        if (args.Length > 0)
        {
            HandleCommandLineArgs(args);
        }

        // Main application loop
        RunMainLoop();

        // Cleanup
        Cleanup();
    }

    private static void SetupEventHandlers()
    {
        if (_captureEngine == null || _projectionWindow == null) return;

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

    private static void HandleCommandLineArgs(string[] args)
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
                        _captureEngine!.Settings.TargetFPS = fps;
                        Console.WriteLine($"[INFO] Target FPS set to {fps}");
                        i++;
                    }
                    break;
                case "--resolution":
                    if (i + 2 < args.Length && 
                        int.TryParse(args[i + 1], out int width) &&
                        int.TryParse(args[i + 2], out int height))
                    {
                        _captureEngine!.Settings.ScaleWidth = width;
                        _captureEngine!.Settings.ScaleHeight = height;
                        Console.WriteLine($"[INFO] Resolution set to {width}x{height}");
                        i += 2;
                    }
                    break;
                case "--help":
                    ShowHelp();
                    Environment.Exit(0);
                    break;
            }
        }
    }

    private static void RunMainLoop()
    {
        while (_isRunning)
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
                case "0":
                case "exit":
                case "quit":
                    _isRunning = false;
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }

            if (_isRunning)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }

    private static void ShowMainMenu()
    {
        Console.Clear();
        Console.WriteLine("=== Pick66 Main Menu ===");
        Console.WriteLine();
        Console.WriteLine("1. Scan for FiveM processes");
        Console.WriteLine("2. Start capture");
        Console.WriteLine("3. Stop capture");
        Console.WriteLine("4. Start projection");
        Console.WriteLine("5. Stop projection");
        Console.WriteLine("6. Configure settings");
        Console.WriteLine("7. Quick start (auto-detect and start)");
        Console.WriteLine("8. Show status");
        Console.WriteLine("0. Exit");
        Console.WriteLine();
        Console.Write("Enter your choice: ");
    }

    private static void ScanForFiveM()
    {
        Console.WriteLine("\n=== Scanning for FiveM Processes ===");
        var processes = FiveMDetector.FindFiveMProcesses();

        if (processes.Count == 0)
        {
            Console.WriteLine("❌ No FiveM processes found.");
            Console.WriteLine("   Please make sure FiveM is running and try again.");
        }
        else
        {
            Console.WriteLine($"✅ Found {processes.Count} FiveM process(es):");
            for (int i = 0; i < processes.Count; i++)
            {
                Console.WriteLine($"   {i + 1}. {processes[i]}");
            }
        }
    }

    private static void StartCapture()
    {
        Console.WriteLine("\n=== Starting Capture ===");
        
        var processes = FiveMDetector.FindFiveMProcesses();
        if (processes.Count == 0)
        {
            Console.WriteLine("❌ No FiveM processes found. Please start FiveM first.");
            return;
        }

        var targetProcess = processes.First();
        Console.WriteLine($"🎯 Targeting: {targetProcess}");

        if (_captureEngine!.StartCapture(targetProcess.ProcessName))
        {
            Console.WriteLine("✅ Capture started successfully!");
            Console.WriteLine($"   FPS: {_captureEngine.Settings.TargetFPS}");
            Console.WriteLine($"   Resolution: {(_captureEngine.Settings.ScaleWidth > 0 ? $"{_captureEngine.Settings.ScaleWidth}x{_captureEngine.Settings.ScaleHeight}" : "Original")}");
        }
        else
        {
            Console.WriteLine("❌ Failed to start capture.");
        }
    }

    private static void StopCapture()
    {
        Console.WriteLine("\n=== Stopping Capture ===");
        _captureEngine!.StopCapture();
        Console.WriteLine("✅ Capture stopped.");
    }

    private static void StartProjection()
    {
        Console.WriteLine("\n=== Starting Projection ===");
        _projectionWindow!.StartProjection();
        Console.WriteLine("✅ Borderless projection window started.");
        Console.WriteLine("   The projection window should now be visible.");
    }

    private static void StopProjection()
    {
        Console.WriteLine("\n=== Stopping Projection ===");
        _projectionWindow!.StopProjection();
        Console.WriteLine("✅ Projection stopped.");
    }

    private static void ConfigureSettings()
    {
        Console.WriteLine("\n=== Configuration Settings ===");
        Console.WriteLine();
        Console.WriteLine($"Current Target FPS: {_captureEngine!.Settings.TargetFPS}");
        Console.WriteLine($"Current Resolution: {(_captureEngine.Settings.ScaleWidth > 0 ? $"{_captureEngine.Settings.ScaleWidth}x{_captureEngine.Settings.ScaleHeight}" : "Original (auto-detect)")}");
        Console.WriteLine($"Hardware Acceleration: {_captureEngine.Settings.UseHardwareAcceleration}");
        Console.WriteLine();

        Console.WriteLine("Enter new values (press Enter to keep current):");
        
        Console.Write($"Target FPS ({_captureEngine.Settings.TargetFPS}): ");
        var fpsInput = Console.ReadLine();
        if (int.TryParse(fpsInput, out int fps) && fps > 0 && fps <= 120)
        {
            _captureEngine.Settings.TargetFPS = fps;
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

        Console.WriteLine("✅ Settings updated successfully!");
    }

    private static void QuickStart()
    {
        Console.WriteLine("\n=== Quick Start ===");
        Console.WriteLine("🚀 Auto-detecting FiveM and starting capture + projection...");

        var processes = FiveMDetector.FindFiveMProcesses();
        if (processes.Count == 0)
        {
            Console.WriteLine("❌ No FiveM processes found. Please start FiveM first.");
            return;
        }

        var targetProcess = processes.First();
        Console.WriteLine($"🎯 Found: {targetProcess}");

        if (_captureEngine!.StartCapture(targetProcess.ProcessName))
        {
            Console.WriteLine("✅ Capture started!");
            _projectionWindow!.StartProjection();
            Console.WriteLine("✅ Projection started!");
            Console.WriteLine();
            Console.WriteLine("🎮 Pick66 is now running! The game should be projected in a borderless window.");
            Console.WriteLine("   Press any key to return to the main menu.");
        }
        else
        {
            Console.WriteLine("❌ Failed to start capture.");
        }
    }

    private static void ShowStatus()
    {
        Console.WriteLine("\n=== System Status ===");
        Console.WriteLine();
        
        var processes = FiveMDetector.FindFiveMProcesses();
        Console.WriteLine($"FiveM Processes: {processes.Count}");
        
        // This would check actual capture/projection status in a real implementation
        Console.WriteLine($"Capture Status: Active"); // Placeholder
        Console.WriteLine($"Projection Status: Active"); // Placeholder
        Console.WriteLine($"Current FPS Target: {_captureEngine!.Settings.TargetFPS}");
        Console.WriteLine($"Resolution Setting: {(_captureEngine.Settings.ScaleWidth > 0 ? $"{_captureEngine.Settings.ScaleWidth}x{_captureEngine.Settings.ScaleHeight}" : "Original")}");
    }

    private static void AutoStartCapture()
    {
        var processes = FiveMDetector.FindFiveMProcesses();
        if (processes.Count > 0)
        {
            var targetProcess = processes.First();
            if (_captureEngine!.StartCapture(targetProcess.ProcessName))
            {
                Console.WriteLine($"[INFO] Auto-started capture for: {targetProcess}");
                _projectionWindow!.StartProjection();
                Console.WriteLine("[INFO] Auto-started projection");
            }
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Pick66 - OBS Game Capture Clone for FiveM");
        Console.WriteLine();
        Console.WriteLine("Usage: Pick66.Launcher.exe [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --auto-start          Automatically start capture and projection");
        Console.WriteLine("  --fps <number>        Set target FPS (default: 60)");
        Console.WriteLine("  --resolution <w> <h>  Set output resolution");
        Console.WriteLine("  --help                Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  Pick66.Launcher.exe --auto-start --fps 30");
        Console.WriteLine("  Pick66.Launcher.exe --resolution 1920 1080 --fps 60");
    }

    private static void Cleanup()
    {
        Console.WriteLine("\n=== Shutting Down ===");
        _captureEngine?.StopCapture();
        _projectionWindow?.StopProjection();
        Console.WriteLine("✅ Pick66 has been shut down gracefully.");
    }
}
