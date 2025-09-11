using System.Drawing;

namespace Pick6.Core;

/// <summary>
/// Interface for capture backends (GDI, DXGI, etc.)
/// Prepares for future DXGI backend implementation
/// </summary>
public interface ICaptureBackend
{
    /// <summary>
    /// Event fired when a frame is captured
    /// </summary>
    event EventHandler<FrameCapturedEventArgs>? FrameCaptured;
    
    /// <summary>
    /// Event fired when an error occurs
    /// </summary>
    event EventHandler<string>? ErrorOccurred;
    
    /// <summary>
    /// Capture settings
    /// </summary>
    CaptureSettings Settings { get; set; }
    
    /// <summary>
    /// Start capturing frames from the specified process
    /// </summary>
    /// <param name="processName">Name of the process to capture</param>
    /// <returns>True if capture started successfully</returns>
    bool StartCapture(string processName);
    
    /// <summary>
    /// Stop capturing frames
    /// </summary>
    void StopCapture();
    
    /// <summary>
    /// Get the backend type name
    /// </summary>
    string BackendName { get; }
    
    /// <summary>
    /// Check if this backend is available on the current system
    /// </summary>
    bool IsAvailable { get; }
}

/// <summary>
/// GDI-based capture backend implementation
/// </summary>
public class GdiCaptureBackend : ICaptureBackend
{
    private readonly GameCaptureEngine _engine;
    
    public GdiCaptureBackend()
    {
        _engine = new GameCaptureEngine();
    }
    
    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured
    {
        add => _engine.FrameCaptured += value;
        remove => _engine.FrameCaptured -= value;
    }
    
    public event EventHandler<string>? ErrorOccurred
    {
        add => _engine.ErrorOccurred += value;
        remove => _engine.ErrorOccurred -= value;
    }
    
    public CaptureSettings Settings 
    { 
        get => _engine.Settings; 
        set => _engine.Settings = value; 
    }
    
    public bool StartCapture(string processName) => _engine.StartCapture(processName);
    
    public void StopCapture() => _engine.StopCapture();
    
    public string BackendName => "GDI";
    
    public bool IsAvailable => OperatingSystem.IsWindows();
}

#if FUTURE_DXGI_SUPPORT
/// <summary>
/// DXGI-based capture backend (future implementation)
/// Placeholder for high-performance DirectX capture
/// </summary>
public class DxgiCaptureBackend : ICaptureBackend
{
    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;
    public event EventHandler<string>? ErrorOccurred;
    
    public CaptureSettings Settings { get; set; } = new();
    
    public bool StartCapture(string processName)
    {
        // TODO: Implement DXGI capture
        ErrorOccurred?.Invoke(this, "DXGI capture not yet implemented");
        return false;
    }
    
    public void StopCapture()
    {
        // TODO: Implement DXGI stop
    }
    
    public string BackendName => "DXGI";
    
    public bool IsAvailable => OperatingSystem.IsWindowsVersionAtLeast(8, 0);
}
#endif

/// <summary>
/// Factory for creating capture backends
/// </summary>
public static class CaptureBackendFactory
{
    /// <summary>
    /// Create the best available capture backend for the current system
    /// </summary>
    public static ICaptureBackend CreateBestBackend()
    {
#if FUTURE_DXGI_SUPPORT
        // Prefer DXGI if available (future)
        var dxgi = new DxgiCaptureBackend();
        if (dxgi.IsAvailable)
        {
            return dxgi;
        }
#endif
        
        // Fall back to GDI
        return new GdiCaptureBackend();
    }
    
    /// <summary>
    /// Get all available capture backends
    /// </summary>
    public static IEnumerable<ICaptureBackend> GetAvailableBackends()
    {
        var backends = new List<ICaptureBackend>();
        
        var gdi = new GdiCaptureBackend();
        if (gdi.IsAvailable)
            backends.Add(gdi);
            
#if FUTURE_DXGI_SUPPORT
        var dxgi = new DxgiCaptureBackend();
        if (dxgi.IsAvailable)
            backends.Add(dxgi);
#endif
        
        return backends;
    }
}