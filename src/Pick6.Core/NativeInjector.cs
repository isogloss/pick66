using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Pick6.Core;

/// <summary>
/// P/Invoke wrapper for Pick6Native.dll
/// Provides managed interface to native C++ injection functionality
/// </summary>
public static class NativeInjector
{
    private const string DLL_NAME = "Pick6Native.dll";

    #region P/Invoke Declarations

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    private static extern bool Pick6_Initialize();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Winapi)]
    private static extern void Pick6_Cleanup();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    private static extern bool Pick6_InjectDirect(
        uint processId,
        [MarshalAs(UnmanagedType.LPWStr)] string dllPath,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder errorMessage,
        uint errorMessageSize
    );

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    private static extern bool Pick6_DeployProxy(
        uint processId,
        [MarshalAs(UnmanagedType.LPWStr)] string proxyDllName,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder errorMessage,
        uint errorMessageSize
    );

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Winapi)]
    private static extern bool Pick6_FindFiveMProcesses(
        [Out] uint[] processIds,
        uint maxProcesses,
        out uint processCount
    );

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Winapi)]
    private static extern bool Pick6_EnableSeDebugPrivilege();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Winapi)]
    private static extern bool Pick6_IsProcessRunning(uint processId);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    private static extern bool Pick6_IsProcessUsingModule(
        uint processId,
        [MarshalAs(UnmanagedType.LPWStr)] string moduleName
    );

    #endregion

    #region Managed Wrappers

    /// <summary>
    /// Initialize the native injector
    /// </summary>
    public static bool Initialize()
    {
        try
        {
            return Pick6_Initialize();
        }
        catch (DllNotFoundException)
        {
            Log.Error("Pick6Native.dll not found. Please ensure the native DLL is in the application directory.");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to initialize native injector: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Cleanup and release native resources
    /// </summary>
    public static void Cleanup()
    {
        try
        {
            Pick6_Cleanup();
        }
        catch (Exception ex)
        {
            Log.Warn($"Error during native injector cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// Inject DLL into target process using direct LoadLibrary method
    /// </summary>
    public static bool InjectDirect(uint processId, string dllPath, out string errorMessage)
    {
        var errorBuffer = new StringBuilder(512);
        bool success = Pick6_InjectDirect(processId, dllPath, errorBuffer, 512);
        errorMessage = errorBuffer.ToString();
        return success;
    }

    /// <summary>
    /// Deploy proxy DLL to target process directory
    /// </summary>
    public static bool DeployProxy(uint processId, string proxyDllName, out string errorMessage)
    {
        var errorBuffer = new StringBuilder(512);
        bool success = Pick6_DeployProxy(processId, proxyDllName, errorBuffer, 512);
        errorMessage = errorBuffer.ToString();
        return success;
    }

    /// <summary>
    /// Find all FiveM processes
    /// </summary>
    public static uint[] FindFiveMProcesses()
    {
        const uint maxProcesses = 32;
        var processIds = new uint[maxProcesses];
        uint processCount = 0;

        if (Pick6_FindFiveMProcesses(processIds, maxProcesses, out processCount))
        {
            var result = new uint[processCount];
            Array.Copy(processIds, result, processCount);
            return result;
        }

        return Array.Empty<uint>();
    }

    /// <summary>
    /// Enable SeDebugPrivilege for current process
    /// </summary>
    public static bool EnableSeDebugPrivilege()
    {
        return Pick6_EnableSeDebugPrivilege();
    }

    /// <summary>
    /// Check if a process is running
    /// </summary>
    public static bool IsProcessRunning(uint processId)
    {
        return Pick6_IsProcessRunning(processId);
    }

    /// <summary>
    /// Check if a process is using a specific module
    /// </summary>
    public static bool IsProcessUsingModule(uint processId, string moduleName)
    {
        return Pick6_IsProcessUsingModule(processId, moduleName);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Check if the native DLL is available
    /// </summary>
    public static bool IsAvailable()
    {
        try
        {
            // Try to initialize - this will load the DLL
            return Initialize();
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the expected path to the native DLL
    /// </summary>
    public static string GetExpectedDllPath()
    {
        return PathResolver.FindDll(DLL_NAME);
    }

    #endregion
}
