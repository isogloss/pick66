using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Pick6.Core;

/// <summary>
/// Handles DLL injection into FiveM processes for Vulkan frame capture
/// </summary>
public class VulkanInjector
{
    private const string INJECTION_DLL_NAME = "Pick6VulkanHook.dll";
    private Process? _targetProcess;
    private IntPtr _injectedDllHandle = IntPtr.Zero;

    /// <summary>
    /// Find FiveM processes that are using Vulkan
    /// </summary>
    public static List<VulkanProcessInfo> FindVulkanProcesses()
    {
        var processes = new List<VulkanProcessInfo>();
        
        // Get all FiveM processes
        var fiveMProcesses = FiveMDetector.FindFiveMProcesses();
        
        foreach (var processInfo in fiveMProcesses)
        {
            // Check if process is using Vulkan
            if (IsProcessUsingVulkan(processInfo.ProcessId))
            {
                processes.Add(new VulkanProcessInfo
                {
                    ProcessId = processInfo.ProcessId,
                    ProcessName = processInfo.ProcessName,
                    WindowTitle = processInfo.WindowTitle,
                    WindowHandle = processInfo.WindowHandle,
                    VulkanDevice = GetVulkanDeviceInfo(processInfo.ProcessId)
                });
            }
        }
        
        return processes;
    }

    /// <summary>
    /// Inject the Vulkan hook DLL into the target process
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
                // DLL not found - in a real implementation, this would be built as part of the solution
                throw new FileNotFoundException($"Vulkan hook DLL not found: {dllPath}");
            }

            return PerformDllInjection(dllPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Injection failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Remove the injected DLL from the process
    /// </summary>
    public void RemoveInjection()
    {
        if (_injectedDllHandle != IntPtr.Zero && _targetProcess != null)
        {
            // In a real implementation, would call FreeLibrary via CreateRemoteThread
            // For now, just clean up references
            _injectedDllHandle = IntPtr.Zero;
        }
        
        _targetProcess?.Dispose();
        _targetProcess = null;
    }

    private static bool IsProcessUsingVulkan(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            
            // Check if vulkan-1.dll is loaded in the process
            // This is a simplified check - real implementation would enumerate process modules
            var modules = process.Modules;
            foreach (ProcessModule module in modules)
            {
                if (module.ModuleName?.ToLower().Contains("vulkan") == true)
                    return true;
            }
        }
        catch
        {
            // Process might be inaccessible or exited
        }
        
        return false;
    }

    private static string GetVulkanDeviceInfo(int processId)
    {
        // In a real implementation, would query the Vulkan device information
        // For now, return a placeholder
        return "Vulkan Device (Unknown)";
    }

    private string GetInjectionDllPath()
    {
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(currentDir, INJECTION_DLL_NAME);
    }

    private bool PerformDllInjection(string dllPath)
    {
        if (_targetProcess == null) return false;

        // Simplified injection logic - in a real implementation would use:
        // 1. OpenProcess to get process handle
        // 2. VirtualAllocEx to allocate memory in target process
        // 3. WriteProcessMemory to write DLL path
        // 4. CreateRemoteThread to call LoadLibrary
        
        var processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, _targetProcess.Id);
        if (processHandle == IntPtr.Zero)
            return false;

        try
        {
            // Allocate memory for DLL path in target process
            var allocSize = (dllPath.Length + 1) * Marshal.SizeOf<char>();
            var allocAddress = VirtualAllocEx(processHandle, IntPtr.Zero, allocSize, 
                MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            
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

    #region Win32 API
    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll")]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll")]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, int dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

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
/// Information about a process using Vulkan
/// </summary>
public class VulkanProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public IntPtr WindowHandle { get; set; }
    public string VulkanDevice { get; set; } = "";

    public override string ToString()
    {
        return $"{ProcessName} - {WindowTitle} (PID: {ProcessId}) [Vulkan: {VulkanDevice}]";
    }
}