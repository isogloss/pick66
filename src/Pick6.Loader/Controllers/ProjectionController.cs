using Pick6.Core;
using Pick6.Projection;
using Pick6.Loader.Settings;
using System.Threading;
using System.Threading.Tasks;

namespace Pick6.Loader.Controllers;

/// <summary>
/// Event args for status change events
/// </summary>
public class StatusChangedEventArgs : EventArgs
{
    public ProjectionStatus Status { get; }
    public string? Message { get; }

    public StatusChangedEventArgs(ProjectionStatus status, string? message = null)
    {
        Status = status;
        Message = message;
    }
}

/// <summary>
/// Event args for log events
/// </summary>
public class LogEventArgs : EventArgs
{
    public DateTime Timestamp { get; }
    public string Level { get; }
    public string Message { get; }

    public LogEventArgs(string level, string message)
    {
        Timestamp = DateTime.Now;
        Level = level;
        Message = message;
    }
}

/// <summary>
/// Projection/runtime status enumeration
/// </summary>
public enum ProjectionStatus
{
    Idle,
    Starting,
    Running,
    Stopping,
    Error
}

/// <summary>
/// Controller that encapsulates the projection/injection lifecycle
/// Enhanced with multi-strategy injection support
/// </summary>
public class ProjectionController : IDisposable
{
    private readonly GameCaptureEngine _captureEngine;
    private readonly BorderlessProjectionWindow _projectionWindow;
    private EnhancedInjector? _enhancedInjector;
    private CancellationTokenSource? _injectionCancellation;
    private Task? _injectionTask;
    private volatile bool _isRunning = false;
    private volatile bool _isDisposed = false;
    private ProjectionStatus _currentStatus = ProjectionStatus.Idle;
    private readonly object _stateLock = new();

    public event EventHandler<StatusChangedEventArgs>? StatusChanged;
    public event EventHandler<LogEventArgs>? Log;

    /// <summary>
    /// Gets whether projection is currently running
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (_stateLock)
            {
                return _isRunning;
            }
        }
    }

    /// <summary>
    /// Gets the current status
    /// </summary>
    public ProjectionStatus Status
    {
        get
        {
            lock (_stateLock)
            {
                return _currentStatus;
            }
        }
    }

    public ProjectionController()
    {
        _captureEngine = new GameCaptureEngine();
        _projectionWindow = new BorderlessProjectionWindow();
        SetupEventHandlers();
        
        // Initialize file logging
        LoggingSetup.InitializeFileLogging();
    }

    /// <summary>
    /// Start projection/injection with the given settings
    /// </summary>
    /// <param name="settings">User settings</param>
    /// <returns>True if start was initiated successfully</returns>
    public bool Start(UserSettings settings)
    {
        if (_isDisposed) return false;

        lock (_stateLock)
        {
            if (_isRunning)
            {
                EmitLog("Info", "Projection is already running");
                return true; // No-op if already running
            }

            _isRunning = true;
            SetStatus(ProjectionStatus.Starting, "Starting projection...");
        }

        try
        {
            EmitLog("Info", "Starting projection/injection system");

            // Apply settings to capture engine
            if (settings.ProjectionRefreshIntervalMs > 0)
            {
                // Apply refresh interval if the capture engine supports it
                // For now, we'll just log it since the existing engine may not expose this setting
                EmitLog("Info", $"Using refresh interval: {settings.ProjectionRefreshIntervalMs}ms");
            }

            // Start enhanced injection process
            StartEnhancedInjection();

            SetStatus(ProjectionStatus.Running, "Monitoring for FiveM processes...");
            return true;
        }
        catch (Exception ex)
        {
            EmitLog("Error", $"Failed to start projection: {ex.Message}");
            SetStatus(ProjectionStatus.Error, ex.Message);
            
            lock (_stateLock)
            {
                _isRunning = false;
            }
            
            return false;
        }
    }

    /// <summary>
    /// Stop projection/injection
    /// </summary>
    public void Stop()
    {
        if (_isDisposed) return;

        lock (_stateLock)
        {
            if (!_isRunning) return;

            _isRunning = false;
            SetStatus(ProjectionStatus.Stopping, "Stopping projection...");
        }

        try
        {
            EmitLog("Info", "Stopping projection/injection system");

            // Stop enhanced injection
            StopEnhancedInjection();

            // Stop projection
            _projectionWindow?.StopProjection();

            // Stop capture
            _captureEngine?.StopCapture();

            SetStatus(ProjectionStatus.Idle, "Projection stopped");
            EmitLog("Info", "Projection stopped successfully");
        }
        catch (Exception ex)
        {
            EmitLog("Error", $"Error while stopping projection: {ex.Message}");
            SetStatus(ProjectionStatus.Error, ex.Message);
        }
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
            EmitLog("Error", $"Capture error: {errorMessage}");
            SetStatus(ProjectionStatus.Error, errorMessage);
        };

        // Handle projection events
        _projectionWindow.ProjectionStarted += (s, e) =>
        {
            EmitLog("Info", "Projection window started");
        };

        _projectionWindow.ProjectionStopped += (s, e) =>
        {
            EmitLog("Info", "Projection window stopped");
        };
    }

    private void StartEnhancedInjection()
    {
        try
        {
            EmitLog("Info", "Starting enhanced injection with multi-strategy fallback");
            
            // Cancel any existing injection task
            StopEnhancedInjection();
            
            // Create new injector and cancellation token
            _enhancedInjector = new EnhancedInjector();
            _injectionCancellation = new CancellationTokenSource();
            
            // Start injection task
            _injectionTask = Task.Run(async () =>
            {
                try
                {
                    var result = await _enhancedInjector.FindAndInjectAsync(_injectionCancellation.Token);
                    
                    if (!_injectionCancellation.Token.IsCancellationRequested)
                    {
                        await HandleInjectionResult(result);
                    }
                }
                catch (OperationCanceledException)
                {
                    EmitLog("Info", "Injection process cancelled");
                }
                catch (Exception ex)
                {
                    EmitLog("Error", $"Enhanced injection failed: {ex.Message}");
                }
            }, _injectionCancellation.Token);
        }
        catch (Exception ex)
        {
            EmitLog("Error", $"Failed to start enhanced injection: {ex.Message}");
        }
    }

    private void StopEnhancedInjection()
    {
        try
        {
            _injectionCancellation?.Cancel();
            _injectionTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            EmitLog("Warn", $"Error stopping injection task: {ex.Message}");
        }
        finally
        {
            _injectionCancellation?.Dispose();
            _injectionCancellation = null;
            _injectionTask = null;
            
            _enhancedInjector?.Dispose();
            _enhancedInjector = null;
        }
    }

    private async Task HandleInjectionResult(InjectionResult result)
    {
        if (result.Success)
        {
            EmitLog("Info", $"✅ Injection successful using {result.Strategy}: {result.Message}");
            
            // Start capture - for now we'll use the existing logic
            // In a full implementation, we might extract process info from the result
            var processes = FiveMDetector.FindFiveMProcesses();
            if (processes.Any())
            {
                var targetProcess = processes.First();
                if (_captureEngine.StartCapture(targetProcess.ProcessName))
                {
                    EmitLog("Info", "Successfully started capture");
                    // Auto-start projection
                    _projectionWindow.StartProjection(0); // Use primary monitor
                }
                else
                {
                    EmitLog("Error", "Injection succeeded but capture failed to start");
                }
            }
        }
        else
        {
            EmitLog("Error", $"❌ All injection strategies failed: {result.Message}");
            SetStatus(ProjectionStatus.Error, $"Injection failed: {result.Message}");
        }
    }

    private void SetStatus(ProjectionStatus status, string? message = null)
    {
        lock (_stateLock)
        {
            _currentStatus = status;
        }

        StatusChanged?.Invoke(this, new StatusChangedEventArgs(status, message));
    }

    private void EmitLog(string level, string message)
    {
        Log?.Invoke(this, new LogEventArgs(level, message));
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Stop();
        
        _projectionWindow?.Dispose();
        _captureEngine?.Dispose();
        
        // Cleanup file logging
        LoggingSetup.ShutdownFileLogging();
    }
}