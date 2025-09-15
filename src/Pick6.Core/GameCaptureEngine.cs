using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Pick6.Core.Timing;
using Pick6.Core.Diagnostics;

namespace Pick6.Core;

/// <summary>
/// Core capture engine for OBS-style game capture
/// Uses DXGI/D3D11 hooks for FiveM processes, with GDI fallback
/// </summary>
public class GameCaptureEngine
{
    private ICaptureBackend? _captureBackend;
    private bool _isCapturing = false;
    private readonly object _lockObject = new();

    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Capture settings
    /// </summary>
    public CaptureSettings Settings { get; set; } = new();

    /// <summary>
    /// Frame timing and performance statistics
    /// </summary>
    public FrameStatistics Statistics => _captureBackend?.Statistics ?? new FrameStatistics();

    /// <summary>
    /// Start capturing frames from the target FiveM process
    /// Uses best available backend (DXGI preferred, GDI fallback)
    /// </summary>
    public bool StartCapture(string processName)
    {
        lock (_lockObject)
        {
            if (_isCapturing) return false;

            // Only support FiveM processes for OBS-style game capture
            if (!IsFiveMProcess(processName))
            {
                ErrorOccurred?.Invoke(this, $"OBS-style capture only supports FiveM processes, got: {processName}");
                return false;
            }

            try
            {
                // Create the best available capture backend
                _captureBackend = CaptureBackendFactory.CreateBestBackend();
                _captureBackend.Settings = Settings;

                // Forward events from backend
                _captureBackend.FrameCaptured += (s, e) => FrameCaptured?.Invoke(this, e);
                _captureBackend.ErrorOccurred += (s, msg) => ErrorOccurred?.Invoke(this, msg);

                // Start capture
                if (_captureBackend.StartCapture(processName))
                {
                    _isCapturing = true;
                    return true;
                }

                ErrorOccurred?.Invoke(this, $"Failed to start capture with {_captureBackend.BackendName} backend");
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Capture startup failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Stop capturing frames
    /// </summary>
    public void StopCapture()
    {
        lock (_lockObject)
        {
            _isCapturing = false;
            _captureBackend?.StopCapture();
            _captureBackend = null;
        }
    }

    /// <summary>
    /// Check if the process name corresponds to FiveM
    /// </summary>
    private static bool IsFiveMProcess(string processName)
    {
        var lowerName = processName.ToLowerInvariant();
        return lowerName.Contains("fivem") || 
               lowerName.Contains("citizenfx") || 
               lowerName.Contains("gtaprocess");
    }
}

/// <summary>
/// Event args for frame captured events
/// </summary>
public class FrameCapturedEventArgs : EventArgs
{
    public Bitmap Frame { get; }

    public FrameCapturedEventArgs(Bitmap frame)
    {
        Frame = frame;
    }
}

/// <summary>
/// Capture configuration settings
/// </summary>
public class CaptureSettings
{
    public int TargetFPS { get; set; } = 60;
    public int ScaleWidth { get; set; } = 0; // 0 = use original
    public int ScaleHeight { get; set; } = 0; // 0 = use original
    public bool UseHardwareAcceleration { get; set; } = true;
}
