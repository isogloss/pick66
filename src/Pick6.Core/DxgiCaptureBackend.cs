using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Pick6.Core.Timing;
using Pick6.Core.Diagnostics;

namespace Pick6.Core;

/// <summary>
/// DXGI-based capture backend for OBS-style game capture
/// Hooks into DXGI.dll and D3D11.dll for FiveM processes
/// </summary>
[SupportedOSPlatform("windows")]
public class DxgiCaptureBackend : ICaptureBackend
{
    private DxgiInjector? _injector;
    private bool _isCapturing = false;
    private Thread? _captureThread;
    private readonly object _lockObject = new();
    private SharedMemoryBuffer? _sharedMemory;
    private readonly FramePacer _framePacer = new();
    private readonly FrameStatistics _statistics = new();

    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Capture settings
    /// </summary>
    public CaptureSettings Settings { get; set; } = new();

    /// <summary>
    /// Frame timing and performance statistics  
    /// </summary>
    public FrameStatistics Statistics => _statistics;

    /// <summary>
    /// Backend name identifier
    /// </summary>
    public string BackendName => "DXGI";

    /// <summary>
    /// Check if DXGI capture is available on this system
    /// </summary>
    public bool IsAvailable => OperatingSystem.IsWindows() && IsDxgiSupported();

    /// <summary>
    /// Start capturing frames using DXGI injection into FiveM process
    /// </summary>
    public bool StartCapture(string processName)
    {
        lock (_lockObject)
        {
            if (_isCapturing) return false;

            // Only support FiveM processes
            if (!IsFiveMProcess(processName))
            {
                ErrorOccurred?.Invoke(this, $"DXGI capture only supports FiveM processes, got: {processName}");
                return false;
            }

            try
            {
                // Find FiveM process to inject into
                var fiveMProcesses = FiveMDetector.FindFiveMProcesses();
                var targetProcess = fiveMProcesses.FirstOrDefault(p => 
                    p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

                if (targetProcess == null)
                {
                    ErrorOccurred?.Invoke(this, $"No FiveM process found with name: {processName}");
                    return false;
                }

                // Create DXGI injector and inject hooks
                _injector = new DxgiInjector();
                
                if (!_injector.InjectIntoProcess(targetProcess.ProcessId))
                {
                    ErrorOccurred?.Invoke(this, "Failed to inject DXGI hooks into FiveM process");
                    return false;
                }

                // Initialize shared memory for frame data transfer
                _sharedMemory = new SharedMemoryBuffer($"Pick6_DXGI_Frames_{targetProcess.ProcessId}");
                
                _isCapturing = true;
                _captureThread = new Thread(CaptureLoop) { IsBackground = true };
                _captureThread.Start();
                
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"DXGI injection failed: {ex.Message}");
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
            _captureThread?.Join(1000);
            
            _injector?.RemoveInjection();
            _injector = null;
            
            _sharedMemory?.Dispose();
            _sharedMemory = null;
        }
    }

    /// <summary>
    /// Main capture loop that reads frames from shared memory
    /// </summary>
    private void CaptureLoop()
    {
        // Initialize frame pacing
        _framePacer.Reset(Settings.TargetFPS, PacingMode.HybridSpin);
        _statistics.Reset();
        
        // Enable diagnostics logging if requested
        var enableDiagnostics = Environment.GetEnvironmentVariable("PICK6_DIAG") == "1";

        while (_isCapturing)
        {
            try
            {
                if (_sharedMemory != null)
                {
                    var frameData = _sharedMemory.ReadFrame();
                    if (frameData != null)
                    {
                        var bitmap = ConvertFrameDataToBitmap(frameData);
                        if (bitmap != null)
                        {
                            FrameCaptured?.Invoke(this, new FrameCapturedEventArgs(bitmap));
                        }
                    }
                }

                // Wait for next frame and get timing statistics
                var frameElapsed = _framePacer.WaitNextFrame();
                _statistics.RecordFrame(frameElapsed.ElapsedMs, frameElapsed.TargetIntervalMs);

                // Optional diagnostic logging
                if (enableDiagnostics && _statistics.TotalFrames % 60 == 0) // Log every ~1 second at 60fps
                {
                    Console.WriteLine($"[DXGI Capture] {_statistics.GetSummary()}");
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"DXGI Capture error: {ex.Message}");
                Thread.Sleep(100); // Brief pause on error
            }
        }
    }

    /// <summary>
    /// Convert raw frame data from shared memory to Bitmap
    /// </summary>
    private Bitmap? ConvertFrameDataToBitmap(byte[] frameData)
    {
        try
        {
            // Frame data format: [width:4][height:4][pixel_data]
            if (frameData.Length < 8) return null;

            int width = BitConverter.ToInt32(frameData, 0);
            int height = BitConverter.ToInt32(frameData, 4);
            
            if (width <= 0 || height <= 0 || frameData.Length < 8 + (width * height * 4))
                return null;

            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // Copy pixel data
            Marshal.Copy(frameData, 8, bitmapData.Scan0, width * height * 4);
            
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Frame conversion error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Check if DXGI is supported on this system
    /// </summary>
    private static bool IsDxgiSupported()
    {
        try
        {
            // Try to load DXGI.dll to check if it's available
            var dxgiModule = LoadLibrary("dxgi.dll");
            if (dxgiModule != IntPtr.Zero)
            {
                FreeLibrary(dxgiModule);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
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

    #region Win32 API
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);
    #endregion
}

/// <summary>
/// DXGI-based DLL injector for FiveM processes
/// Hooks DXGI.dll and D3D11.dll for frame capture
/// </summary>
[SupportedOSPlatform("windows")]
public class DxgiInjector
{
    private const string DXGI_HOOK_DLL_NAME = "Pick6DxgiHook.dll";
    private Process? _targetProcess;
    private IntPtr _injectedDllHandle = IntPtr.Zero;

    /// <summary>
    /// Inject DXGI/D3D11 hooks into the target FiveM process
    /// </summary>
    public bool InjectIntoProcess(int processId)
    {
        try
        {
            _targetProcess = Process.GetProcessById(processId);
            if (_targetProcess == null || _targetProcess.HasExited)
                return false;

            var dllPath = GetInjectionDllPath();
            if (!File.Exists(dllPath))
            {
                // In a real implementation, this DLL would be built as part of the solution
                // For now, we'll create a placeholder that demonstrates the concept
                throw new FileNotFoundException($"DXGI hook DLL not found: {dllPath}. This DLL would contain the actual DXGI/D3D11 hooks.");
            }

            return PerformDllInjection(dllPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DXGI injection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Remove the injected DLL from the target process
    /// </summary>
    public void RemoveInjection()
    {
        if (_injectedDllHandle != IntPtr.Zero && _targetProcess != null && !_targetProcess.HasExited)
        {
            try
            {
                // In a real implementation, would cleanly remove hooks and free the DLL
                // This is a simplified cleanup
                _injectedDllHandle = IntPtr.Zero;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Perform the actual DLL injection using Windows APIs
    /// </summary>
    private bool PerformDllInjection(string dllPath)
    {
        var processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, _targetProcess!.Id);
        if (processHandle == IntPtr.Zero)
            return false;

        try
        {
            // Allocate memory for DLL path in target process
            var allocAddress = VirtualAllocEx(processHandle, IntPtr.Zero, 
                dllPath.Length * 2 + 2, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            
            if (allocAddress == IntPtr.Zero)
                return false;

            // Write DLL path to allocated memory
            var pathBytes = System.Text.Encoding.Unicode.GetBytes(dllPath + '\0');
            if (!WriteProcessMemory(processHandle, allocAddress, pathBytes, pathBytes.Length, out _))
                return false;

            // Get LoadLibraryW address
            var kernel32 = GetModuleHandle("kernel32.dll");
            var loadLibraryAddr = GetProcAddress(kernel32, "LoadLibraryW");
            
            if (loadLibraryAddr == IntPtr.Zero)
                return false;

            // Create remote thread to load the DLL
            var threadHandle = CreateRemoteThread(processHandle, IntPtr.Zero, 0, 
                loadLibraryAddr, allocAddress, 0, out _);
            
            if (threadHandle != IntPtr.Zero)
            {
                WaitForSingleObject(threadHandle, 5000); // Wait up to 5 seconds
                CloseHandle(threadHandle);
                _injectedDllHandle = allocAddress; // Store for cleanup
                return true;
            }

            return false;
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    /// <summary>
    /// Get the path to the DXGI hook DLL
    /// </summary>
    private string GetInjectionDllPath()
    {
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(currentDir, DXGI_HOOK_DLL_NAME);
    }

    #region Win32 API
    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll")]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll")]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint PAGE_READWRITE = 0x04;
    #endregion
}

/// <summary>
/// Shared memory buffer for transferring frame data between processes
/// Used for DXGI frame capture data transfer
/// </summary>
public class SharedMemoryBuffer : IDisposable
{
    private readonly string _name;
    private IntPtr _mappedMemory = IntPtr.Zero;
    private readonly int _bufferSize = 1920 * 1080 * 4 + 1024; // Full HD + header space

    public SharedMemoryBuffer(string name)
    {
        _name = name;
        InitializeSharedMemory();
    }

    /// <summary>
    /// Read frame data from shared memory
    /// </summary>
    public byte[]? ReadFrame()
    {
        if (_mappedMemory == IntPtr.Zero) return null;

        try
        {
            // Read frame size from first 4 bytes
            var sizeBytes = new byte[4];
            Marshal.Copy(_mappedMemory, sizeBytes, 0, 4);
            int frameSize = BitConverter.ToInt32(sizeBytes, 0);

            if (frameSize <= 0 || frameSize > _bufferSize - 4) return null;

            // Read frame data
            var frameData = new byte[frameSize];
            Marshal.Copy(_mappedMemory + 4, frameData, 0, frameSize);
            
            return frameData;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Initialize the shared memory mapping
    /// </summary>
    private void InitializeSharedMemory()
    {
        try
        {
            // In a real implementation, would create/open actual shared memory
            // For now, this is a placeholder that demonstrates the concept
            _mappedMemory = Marshal.AllocHGlobal(_bufferSize);
        }
        catch
        {
            _mappedMemory = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_mappedMemory != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_mappedMemory);
            _mappedMemory = IntPtr.Zero;
        }
    }
}