using Pick6.Core;
using System.Drawing;
using System.Runtime.Versioning;

namespace Pick6.Projection;

/// <summary>
/// Borderless fullscreen projection window implementation
/// </summary>
public class BorderlessProjectionWindow
{
    private bool _isProjecting = false;
    private Bitmap? _currentFrame;
    private readonly object _frameLock = new();
    private Thread? _renderThread;

    public event EventHandler? ProjectionStarted;
    public event EventHandler? ProjectionStopped;

    /// <summary>
    /// Start the borderless projection
    /// </summary>
    public void StartProjection(int screenIndex = 0)
    {
        if (_isProjecting) return;

        _isProjecting = true;
        
        if (OperatingSystem.IsWindows())
        {
            StartWindowsProjection(screenIndex);
        }
        else
        {
            StartSimulatedProjection();
        }

        ProjectionStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Stop the projection
    /// </summary>
    public void StopProjection()
    {
        if (!_isProjecting) return;

        _isProjecting = false;
        _renderThread?.Join(1000);
        
        ProjectionStopped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Update the current frame being displayed
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
    private void StartWindowsProjection(int screenIndex)
    {
        // This would create an actual borderless fullscreen window on Windows
        Console.WriteLine($"Starting Windows projection on screen {screenIndex}");
        
        _renderThread = new Thread(() =>
        {
            while (_isProjecting)
            {
                // Render current frame to borderless window
                RenderFrame();
                Thread.Sleep(16); // ~60 FPS
            }
        })
        { IsBackground = true };
        
        _renderThread.Start();
    }

    private void StartSimulatedProjection()
    {
        Console.WriteLine("Starting simulated projection (console output)");
        
        _renderThread = new Thread(() =>
        {
            int frameCount = 0;
            while (_isProjecting)
            {
                if (_currentFrame != null)
                {
                    frameCount++;
                    if (frameCount % 60 == 0) // Log every 60 frames
                    {
                        if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                        {
                            Console.WriteLine($"Projecting frame {frameCount}: {_currentFrame.Width}x{_currentFrame.Height}");
                        }
                        else
                        {
                            Console.WriteLine($"Projecting frame {frameCount}");
                        }
                    }
                }
                Thread.Sleep(16);
            }
        })
        { IsBackground = true };
        
        _renderThread.Start();
    }

    private void RenderFrame()
    {
        lock (_frameLock)
        {
            if (_currentFrame == null) return;
            
            // In a real Windows implementation, this would:
            // 1. Create a borderless fullscreen window
            // 2. Use hardware-accelerated rendering (DirectX/OpenGL)
            // 3. Present the frame with minimal latency
            // 4. Handle window events and maintain fullscreen state
        }
    }
}

/// <summary>
/// Configuration for projection settings
/// </summary>
public class ProjectionSettings
{
    public int TargetScreen { get; set; } = 0;
    public bool EnableVSync { get; set; } = true;
    public bool UseHardwareAcceleration { get; set; } = true;
    public ProjectionMode Mode { get; set; } = ProjectionMode.Fullscreen;
}

public enum ProjectionMode
{
    Fullscreen,
    Windowed,
    BorderlessWindowed
}
