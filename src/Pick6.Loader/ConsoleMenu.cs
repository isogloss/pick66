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
    private int _selectedMonitor = 0;

    public ConsoleMenu()
    {
        _captureEngine = new GameCaptureEngine();
        _projectionWindow = new BorderlessProjectionWindow();
        SetupEventHandlers();
    }

    public void Run(string[] args)
    {
        var isRunning = true;

        Console.WriteLine("================================================");
        Console.WriteLine("        Pick6 - OBS Game Capture Clone        ");
        Console.WriteLine("         Console Mode                          ");
        Console.WriteLine("================================================");
        Console.WriteLine();
        Console.WriteLine("💡 For a better experience on Windows, run without --console flag for GUI mode");
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
            if (OperatingSystem.IsWindows())
            {
                _projectionWindow.UpdateFrame(e.Frame);
            }
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
        Console.WriteLine("=== Pick6 Main Menu ===");
        Console.WriteLine();
        Console.WriteLine("1. Scan for FiveM processes");
        Console.WriteLine("2. Start capture");
        Console.WriteLine("3. Stop capture");
        Console.WriteLine("4. Start projection");
        Console.WriteLine("5. Stop projection");
        Console.WriteLine("6. Configure settings");
        Console.WriteLine("7. Quick start (auto-detect and start)");
        Console.WriteLine("8. Show status");
        Console.WriteLine("9. Test projection with demo frames");
        Console.WriteLine("0. Exit");
        Console.WriteLine();
        Console.Write("Enter your choice: ");
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
        if (OperatingSystem.IsWindows())
        {
            ShowAvailableMonitors();
        }
        
        Console.WriteLine();

        Console.WriteLine("Enter new values (press Enter to keep current):");
        
        Console.Write($"Target FPS ({_captureEngine.Settings.TargetFPS}): ");
        var fpsInput = Console.ReadLine();
        if (int.TryParse(fpsInput, out int fps) && fps > 0 && fps <= 120)
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
        if (OperatingSystem.IsWindows())
        {
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
        
        // This would check actual capture/projection status in a real implementation
        Console.WriteLine($"Capture Status: Active"); // Placeholder
        Console.WriteLine($"Projection Status: Active"); // Placeholder
        Console.WriteLine($"Current FPS Target: {_captureEngine.Settings.TargetFPS}");
        Console.WriteLine($"Resolution Setting: {(_captureEngine.Settings.ScaleWidth > 0 ? $"{_captureEngine.Settings.ScaleWidth}x{_captureEngine.Settings.ScaleHeight}" : "Original")}");
        Console.WriteLine();
        Console.WriteLine("💡 Tips:");
        Console.WriteLine("  - Vulkan injection provides better performance");
        Console.WriteLine("  - Run as administrator for injection privileges");
        Console.WriteLine("  - Traditional window capture works as fallback");
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

    private void Cleanup()
    {
        Console.WriteLine("\n=== Shutting Down ===");
        _captureEngine?.StopCapture();
        _projectionWindow?.StopProjection();
        Console.WriteLine("✅ Pick6 has been shut down gracefully.");
    }

    private async void TestProjectionWithDemoFrames()
    {
        Console.WriteLine("\n=== Testing Projection with Demo Frames ===");
        Console.WriteLine("This will start projection and generate colored test frames...");
        
        // Start projection first
        _projectionWindow.StartProjection(_selectedMonitor);
        Console.WriteLine("✅ Projection window started");
        
        if (OperatingSystem.IsWindows())
        {
            Console.WriteLine("🎨 Generating colorful test frames for 10 seconds...");
            Console.WriteLine("   (You should see a projection window with changing colors)");
            
            await GenerateTestFramesAsync();
            
            Console.WriteLine("\nTest is running... The projection window should show colorful frames.");
            Console.WriteLine("Press any key when you're done viewing the projection window.");
            Console.ReadKey();
        }
        else
        {
            Console.WriteLine("⚠️ Demo frames test is only available on Windows platform");
            Console.WriteLine("   On this platform, you'll see simulated projection output instead.");
            
            // Still run a basic test for non-Windows - but can't use Bitmap APIs
            await Task.Delay(2000);
        }
        
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
                if (OperatingSystem.IsWindows())
                {
                    GenerateTestFrame(random, i);
                }
                await Task.Delay(33); // ~30 FPS
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating test frame: {ex.Message}");
                break;
            }
        }
        
        Console.WriteLine("\n🏁 Test completed - stopping projection...");
        _projectionWindow.StopProjection();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
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
        Console.WriteLine("  --fps <number>        Set target FPS (console mode, default: 60)");
        Console.WriteLine("  --resolution <w> <h>  Set output resolution (console mode)");
        Console.WriteLine("  --help                Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  pick6_loader                           # Start GUI on Windows, console on other OS");
        Console.WriteLine("  pick6_loader --console                 # Force console mode");
        Console.WriteLine("  pick6_loader --console --auto-start    # Console mode with auto-start");
        Console.WriteLine("  pick6_loader --console --fps 30        # Console mode with 30 FPS");
        Console.WriteLine("  pick6_loader --console --resolution 1920 1080 --fps 60");
        Console.WriteLine();
        Console.WriteLine("Build commands:");
        Console.WriteLine("  dotnet build -c Release");
        Console.WriteLine("  dotnet publish src/Pick6.Loader -c Release -r win-x64 --self-contained true \\");
        Console.WriteLine("    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \\");
        Console.WriteLine("    -p:PublishReadyToRun=true");
    }
}