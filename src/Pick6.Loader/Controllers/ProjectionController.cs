using Pick6.Core;
using Pick6.Projection;
using Pick6.Loader.Settings;

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
/// </summary>
public class ProjectionController : IDisposable
{
    private readonly GameCaptureEngine _captureEngine;
    private readonly BorderlessProjectionWindow _projectionWindow;
    private System.Timers.Timer? _processMonitorTimer;
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

            // Start monitoring for processes
            StartProcessMonitoring();

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

            // Stop process monitoring
            StopProcessMonitoring();

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

    private void StartProcessMonitoring()
    {
        _processMonitorTimer = new System.Timers.Timer(1000); // Check every second
        _processMonitorTimer.Elapsed += ProcessMonitorTimer_Elapsed;
        _processMonitorTimer.Start();

        // Check immediately if FiveM is already running
        CheckForFiveMAndInject();
    }

    private void StopProcessMonitoring()
    {
        _processMonitorTimer?.Stop();
        _processMonitorTimer?.Dispose();
        _processMonitorTimer = null;
    }

    private void ProcessMonitorTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!IsRunning) return;
        CheckForFiveMAndInject();
    }

    private void CheckForFiveMAndInject()
    {
        try
        {
            var summary = FiveMDetector.GetProcessSummary();
            
            if (summary.TotalProcessCount > 0)
            {
                AttemptInjection(summary);
            }
        }
        catch (Exception ex)
        {
            EmitLog("Error", $"Error checking for FiveM processes: {ex.Message}");
        }
    }

    private void AttemptInjection(FiveMProcessSummary summary)
    {
        ProcessInfo? targetProcess = null;
        string method = "";

        // Prioritize Vulkan processes
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
        else if (summary.TraditionalProcesses.Any())
        {
            targetProcess = summary.TraditionalProcesses.First();
            method = "Window capture";
        }

        if (targetProcess == null) return;

        EmitLog("Info", $"Attempting {method} on {targetProcess.ProcessName}");

        if (_captureEngine.StartCapture(targetProcess.ProcessName))
        {
            EmitLog("Info", $"Successfully started capture - {method}");
            
            // Auto-start projection
            _projectionWindow.StartProjection(0); // Use primary monitor
        }
        else
        {
            EmitLog("Warn", $"Failed to start capture on {targetProcess.ProcessName}");
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
    }
}