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
    private WindowsProjectionForm? _windowsProjection;
    private int _targetFPS = 60;

    public event EventHandler? ProjectionStarted;
    public event EventHandler? ProjectionStopped;

    /// <summary>
    /// Set the target FPS for the projection
    /// </summary>
    public void SetTargetFPS(int fps)
    {
        _targetFPS = Math.Max(15, Math.Min(240, fps));
        _windowsProjection?.SetTargetFPS(_targetFPS);
    }

    /// <summary>
    /// Enable or disable FPS logging for debugging
    /// </summary>
    public void SetFpsLogging(bool enabled)
    {
        _windowsProjection?.SetFpsLogging(enabled);
    }

    /// <summary>
    /// Enable or disable match capture FPS mode
    /// </summary>
    public void SetMatchCaptureFPS(bool enabled)
    {
        _windowsProjection?.SetMatchCaptureFPS(enabled);
    }

    /// <summary>
    /// Update the projection FPS based on capture engine settings
    /// </summary>
    public void UpdateCaptureFPS(int captureFPS)
    {
        _windowsProjection?.UpdateCaptureFPS(captureFPS);
    }

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

        if (OperatingSystem.IsWindows() && _windowsProjection != null)
        {
            _windowsProjection.StopProjection();
            _windowsProjection = null;
        }
        
        ProjectionStopped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Update the current frame being displayed
    /// </summary>
    public void UpdateFrame(Bitmap frame)
    {
        if (!_isProjecting) return;

        if (OperatingSystem.IsWindows())
        {
            UpdateFrameWindows(frame);
        }
    }

    [SupportedOSPlatform("windows")]
    private void UpdateFrameWindows(Bitmap frame)
    {
        lock (_frameLock)
        {
            _currentFrame?.Dispose();
            _currentFrame = new Bitmap(frame);
            
            // Update the Windows projection window if it exists
            _windowsProjection?.UpdateFrame(_currentFrame);
        }
    }

    [SupportedOSPlatform("windows")]
    private void StartWindowsProjection(int screenIndex)
    {
        try
        {
            _windowsProjection = new WindowsProjectionForm();
            _windowsProjection.SetTargetFPS(_targetFPS);
            
            // Forward projection events
            _windowsProjection.ProjectionStarted += (s, e) => ProjectionStarted?.Invoke(this, e);
            _windowsProjection.ProjectionStopped += (s, e) => 
            {
                _isProjecting = false;
                ProjectionStopped?.Invoke(this, e);
            };
            
            _windowsProjection.StartProjection(screenIndex);
            Console.WriteLine($"✅ Started borderless projection window on screen {screenIndex}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to start Windows projection: {ex.Message}");
            _isProjecting = false;
        }
    }

    private void StartSimulatedProjection()
    {
        Console.WriteLine("Starting simulated projection (non-Windows platform)");
        
        Task.Run(async () =>
        {
            int frameCount = 0;
            var frameTimeMs = 1000.0 / _targetFPS;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var lastFrameTime = 0.0;
            
            while (_isProjecting)
            {
                var currentTime = stopwatch.Elapsed.TotalMilliseconds;
                
                if (currentTime - lastFrameTime >= frameTimeMs)
                {
                    lock (_frameLock)
                    {
                        if (_currentFrame != null)
                        {
                            frameCount++;
                            if (frameCount % _targetFPS == 0) // Log every second worth of frames
                            {
                                // On non-Windows platforms, we can't access Bitmap properties safely
                                Console.WriteLine($"Simulated projection frame {frameCount} (FPS: {_targetFPS})");
                            }
                        }
                    }
                    lastFrameTime = currentTime;
                }
                
                await Task.Delay(1); // Small delay to prevent 100% CPU usage
            }
        });
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
