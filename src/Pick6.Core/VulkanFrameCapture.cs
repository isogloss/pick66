using System.Drawing;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace Pick6.Core;

/// <summary>
/// Vulkan-based frame capture engine using DLL injection
/// </summary>
public class VulkanFrameCapture
{
    private VulkanInjector? _injector;
    private bool _isCapturing = false;
    private Thread? _captureThread;
    private readonly object _lockObject = new();
    private SharedMemoryBuffer? _sharedMemory;

    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Capture settings
    /// </summary>
    public CaptureSettings Settings { get; set; } = new();

    /// <summary>
    /// Start capturing frames using Vulkan injection
    /// </summary>
    [SupportedOSPlatform("windows")]
    public bool StartCapture(int processId)
    {
        lock (_lockObject)
        {
            if (_isCapturing) return false;

            try
            {
                _injector = new VulkanInjector();
                
                if (!_injector.InjectIntoProcess(processId))
                {
                    ErrorOccurred?.Invoke(this, "Failed to inject into process");
                    return false;
                }

                // Initialize shared memory for frame data transfer
                _sharedMemory = new SharedMemoryBuffer($"Pick6_Frames_{processId}");
                
                _isCapturing = true;
                _captureThread = new Thread(CaptureLoop) { IsBackground = true };
                _captureThread.Start();
                
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Injection failed: {ex.Message}");
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

    [SupportedOSPlatform("windows")]
    private void CaptureLoop()
    {
        while (_isCapturing)
        {
            try
            {
                // Check for new frame data from the injected DLL
                var frameData = _sharedMemory?.ReadFrame();
                if (frameData != null)
                {
                    var bitmap = ConvertFrameDataToBitmap(frameData);
                    if (bitmap != null)
                    {
                        FrameCaptured?.Invoke(this, new FrameCapturedEventArgs(bitmap));
                    }
                }

                // Target frame rate based on settings
                int delay = 1000 / Settings.TargetFPS;
                Thread.Sleep(delay);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Capture error: {ex.Message}");
                Thread.Sleep(100); // Brief pause on error
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private Bitmap? ConvertFrameDataToBitmap(VulkanFrameData frameData)
    {
        try
        {
            // Convert Vulkan frame data to Bitmap
            // In a real implementation, this would handle various Vulkan formats
            var bitmap = new Bitmap(frameData.Width, frameData.Height);
            
            // Apply scaling if configured
            int targetWidth = Settings.ScaleWidth > 0 ? Settings.ScaleWidth : frameData.Width;
            int targetHeight = Settings.ScaleHeight > 0 ? Settings.ScaleHeight : frameData.Height;
            
            if (targetWidth != frameData.Width || targetHeight != frameData.Height)
            {
                var scaledBitmap = new Bitmap(targetWidth, targetHeight);
                using (var graphics = Graphics.FromImage(scaledBitmap))
                {
                    graphics.DrawImage(bitmap, 0, 0, targetWidth, targetHeight);
                }
                bitmap.Dispose();
                return scaledBitmap;
            }
            
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Shared memory buffer for high-performance frame data transfer
/// </summary>
public class SharedMemoryBuffer : IDisposable
{
    private readonly string _name;
    private IntPtr _memoryHandle = IntPtr.Zero;
    private IntPtr _mappedView = IntPtr.Zero;
    private const int BUFFER_SIZE = 1920 * 1080 * 4; // 4K RGBA buffer

    public SharedMemoryBuffer(string name)
    {
        _name = name;
        Initialize();
    }

    private void Initialize()
    {
        // Create shared memory mapping
        _memoryHandle = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, 
            PAGE_READWRITE, 0, BUFFER_SIZE, _name);
        
        if (_memoryHandle != IntPtr.Zero)
        {
            _mappedView = MapViewOfFile(_memoryHandle, FILE_MAP_ALL_ACCESS, 0, 0, BUFFER_SIZE);
        }
    }

    public VulkanFrameData? ReadFrame()
    {
        if (_mappedView == IntPtr.Zero) return null;

        try
        {
            // Read frame header from shared memory
            var header = Marshal.PtrToStructure<FrameHeader>(_mappedView);
            
            if (header.Magic != FRAME_MAGIC || header.DataSize <= 0) 
                return null;

            // Read frame data
            var dataPtr = IntPtr.Add(_mappedView, Marshal.SizeOf<FrameHeader>());
            var frameData = new byte[header.DataSize];
            Marshal.Copy(dataPtr, frameData, 0, header.DataSize);

            return new VulkanFrameData
            {
                Width = header.Width,
                Height = header.Height,
                Format = header.Format,
                Data = frameData,
                Timestamp = header.Timestamp
            };
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_mappedView != IntPtr.Zero)
        {
            UnmapViewOfFile(_mappedView);
            _mappedView = IntPtr.Zero;
        }
        
        if (_memoryHandle != IntPtr.Zero)
        {
            CloseHandle(_memoryHandle);
            _memoryHandle = IntPtr.Zero;
        }
    }

    #region Structures
    [StructLayout(LayoutKind.Sequential)]
    private struct FrameHeader
    {
        public uint Magic;
        public int Width;
        public int Height;
        public int Format;
        public int DataSize;
        public long Timestamp;
    }

    private const uint FRAME_MAGIC = 0x50494B36; // "PIK6"
    #endregion

    #region Win32 API
    [DllImport("kernel32.dll")]
    private static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, 
        uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

    [DllImport("kernel32.dll")]
    private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, 
        uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

    [DllImport("kernel32.dll")]
    private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
    private const uint PAGE_READWRITE = 0x04;
    private const uint FILE_MAP_ALL_ACCESS = 0xF001F;
    #endregion
}

/// <summary>
/// Vulkan frame data structure
/// </summary>
public class VulkanFrameData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Format { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public long Timestamp { get; set; }
}