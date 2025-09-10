using Pick6.Core;
using System.Drawing;
using System.Runtime.Versioning;

namespace Pick6.UI;

/// <summary>
/// Main UI application for Pick6 Game Capture
/// </summary>
public class Program
{
    [STAThread]
    public static void Main()
    {
        // For cross-platform compatibility, we'll create a simple console-based UI
        // that can be adapted to Windows Forms when running on Windows
        if (OperatingSystem.IsWindows())
        {
            RunWindowsUI();
        }
        else
        {
            RunConsoleUI();
        }
    }

    private static void RunWindowsUI()
    {
        // This would run the Windows Forms UI on Windows
        Console.WriteLine("Pick66 Game Capture - Windows UI");
        Console.WriteLine("Note: Windows Forms UI would be available when running on Windows");
        RunConsoleUI();
    }

    private static void RunConsoleUI()
    {
        Console.WriteLine("=== Pick6 Game Capture ===");
        Console.WriteLine("Real-time FiveM capture and projection");
        Console.WriteLine();

        var captureEngine = new GameCaptureEngine();
        var projectionWindow = new ProjectionWindow();

        // Setup event handlers
        captureEngine.FrameCaptured += (s, e) =>
        {
            if (OperatingSystem.IsWindows())
            {
                projectionWindow.UpdateFrame(e.Frame);
            }
        };

        captureEngine.ErrorOccurred += (s, e) =>
        {
            Console.WriteLine($"Error: {e}");
        };

        while (true)
        {
            Console.WriteLine("\nOptions:");
            Console.WriteLine("1. Detect FiveM processes");
            Console.WriteLine("2. Start capture");
            Console.WriteLine("3. Stop capture");
            Console.WriteLine("4. Configure settings");
            Console.WriteLine("5. Start projection");
            Console.WriteLine("6. Stop projection");
            Console.WriteLine("0. Exit");
            Console.Write("Choice: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    DetectProcesses();
                    break;
                case "2":
                    StartCapture(captureEngine);
                    break;
                case "3":
                    captureEngine.StopCapture();
                    Console.WriteLine("Capture stopped.");
                    break;
                case "4":
                    ConfigureSettings(captureEngine);
                    break;
                case "5":
                    projectionWindow.StartProjection();
                    break;
                case "6":
                    projectionWindow.StopProjection();
                    break;
                case "0":
                    captureEngine.StopCapture();
                    projectionWindow.StopProjection();
                    return;
            }
        }
    }

    private static void DetectProcesses()
    {
        Console.WriteLine("\nScanning for FiveM processes...");
        var processes = FiveMDetector.FindFiveMProcesses();

        if (processes.Count == 0)
        {
            Console.WriteLine("No FiveM processes found.");
        }
        else
        {
            Console.WriteLine($"Found {processes.Count} FiveM process(es):");
            for (int i = 0; i < processes.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {processes[i]}");
            }
        }
    }

    private static void StartCapture(GameCaptureEngine engine)
    {
        Console.WriteLine("\nStarting capture...");
        var processes = FiveMDetector.FindFiveMProcesses();

        if (processes.Count == 0)
        {
            Console.WriteLine("No FiveM processes found. Please start FiveM first.");
            return;
        }

        var primaryProcess = processes.First();
        if (engine.StartCapture(primaryProcess.ProcessName))
        {
            Console.WriteLine($"Capture started for: {primaryProcess}");
        }
        else
        {
            Console.WriteLine("Failed to start capture.");
        }
    }

    private static void ConfigureSettings(GameCaptureEngine engine)
    {
        Console.WriteLine("\nCurrent Settings:");
        Console.WriteLine($"Target FPS: {engine.Settings.TargetFPS}");
        Console.WriteLine($"Scale Width: {engine.Settings.ScaleWidth} (0 = original)");
        Console.WriteLine($"Scale Height: {engine.Settings.ScaleHeight} (0 = original)");
        Console.WriteLine($"Hardware Acceleration: {engine.Settings.UseHardwareAcceleration}");

        Console.WriteLine("\nEnter new values (or press Enter to keep current):");

        Console.Write($"Target FPS ({engine.Settings.TargetFPS}): ");
        var fpsInput = Console.ReadLine();
        if (int.TryParse(fpsInput, out int fps) && fps > 0)
            engine.Settings.TargetFPS = fps;

        Console.Write($"Scale Width ({engine.Settings.ScaleWidth}): ");
        var widthInput = Console.ReadLine();
        if (int.TryParse(widthInput, out int width) && width >= 0)
            engine.Settings.ScaleWidth = width;

        Console.Write($"Scale Height ({engine.Settings.ScaleHeight}): ");
        var heightInput = Console.ReadLine();
        if (int.TryParse(heightInput, out int height) && height >= 0)
            engine.Settings.ScaleHeight = height;

        Console.WriteLine("Settings updated.");
    }
}

/// <summary>
/// Simple projection window for displaying captured frames
/// </summary>
public class ProjectionWindow
{
    private bool _isProjecting = false;
    private Bitmap? _currentFrame;
    private readonly object _frameLock = new();

    public void StartProjection()
    {
        _isProjecting = true;
        Console.WriteLine("Projection started (simulated - would show borderless window on Windows)");
        Console.WriteLine("Frame updates will be logged here...");
    }

    public void StopProjection()
    {
        _isProjecting = false;
        Console.WriteLine("Projection stopped.");
    }

    [SupportedOSPlatform("windows")]
    public void UpdateFrame(Bitmap frame)
    {
        if (!_isProjecting) return;

        lock (_frameLock)
        {
            _currentFrame?.Dispose();
            _currentFrame = new Bitmap(frame);
        }

        // In a real implementation, this would update a borderless fullscreen window
        Console.WriteLine($"Frame updated: {frame.Width}x{frame.Height} at {DateTime.Now:HH:mm:ss.fff}");
    }
}
