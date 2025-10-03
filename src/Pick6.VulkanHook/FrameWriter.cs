using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Pick6.VulkanHook;

/// <summary>
/// Writes captured frames to shared memory for IPC with main application
/// </summary>
[SupportedOSPlatform("windows")]
internal class FrameWriter : IDisposable
{
    private IntPtr _memoryHandle = IntPtr.Zero;
    private IntPtr _mappedView = IntPtr.Zero;
    private const int BUFFER_SIZE = 1920 * 1080 * 4; // 4K RGBA buffer
    private readonly string _memoryName;

    public FrameWriter()
    {
        // Use process ID in shared memory name
        var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
        _memoryName = $"Pick6_Frames_{processId}";
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            // Open or create shared memory mapping
            _memoryHandle = OpenFileMapping(FILE_MAP_ALL_ACCESS, false, _memoryName);
            
            if (_memoryHandle == IntPtr.Zero)
            {
                // Create if doesn't exist
                _memoryHandle = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero,
                    PAGE_READWRITE, 0, BUFFER_SIZE, _memoryName);
            }

            if (_memoryHandle != IntPtr.Zero)
            {
                _mappedView = MapViewOfFile(_memoryHandle, FILE_MAP_ALL_ACCESS, 0, 0, BUFFER_SIZE);
            }

            if (_mappedView != IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine($"Pick6VulkanHook: Shared memory initialized: {_memoryName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Pick6VulkanHook: Failed to map shared memory view");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Pick6VulkanHook: Shared memory init failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Write a captured frame to shared memory
    /// </summary>
    public void WriteFrame(FrameData frame)
    {
        if (_mappedView == IntPtr.Zero)
            return;

        try
        {
            // Write frame header
            var header = new FrameHeader
            {
                Magic = FRAME_MAGIC,
                Width = frame.Width,
                Height = frame.Height,
                Format = frame.Format,
                DataSize = frame.Data.Length,
                Timestamp = frame.Timestamp
            };

            Marshal.StructureToPtr(header, _mappedView, false);

            // Write frame data
            if (frame.Data.Length > 0)
            {
                var dataPtr = IntPtr.Add(_mappedView, Marshal.SizeOf<FrameHeader>());
                Marshal.Copy(frame.Data, 0, dataPtr, Math.Min(frame.Data.Length, BUFFER_SIZE - Marshal.SizeOf<FrameHeader>()));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Pick6VulkanHook: Frame write failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Pick6VulkanHook: Cleanup failed: {ex.Message}");
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
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes,
        uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenFileMapping(uint dwDesiredAccess, bool bInheritHandle, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess,
        uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
    private const uint PAGE_READWRITE = 0x04;
    private const uint FILE_MAP_ALL_ACCESS = 0xF001F;
    #endregion
}
