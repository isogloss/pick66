using System;
using System.Runtime.InteropServices;

namespace Pick6.Core;

/// <summary>
/// Manages Windows privileges for process injection
/// </summary>
public static class PrivilegeManager
{
    private const string SE_DEBUG_NAME = "SeDebugPrivilege";
    private const int SE_PRIVILEGE_ENABLED = 0x00000002;

    /// <summary>
    /// Attempt to enable SeDebugPrivilege for current process
    /// </summary>
    /// <returns>True if privilege was enabled or already available</returns>
    public static bool TryEnableSeDebugPrivilege()
    {
        try
        {
            IntPtr tokenHandle = IntPtr.Zero;
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandle))
            {
                Log.Warn("Failed to open process token for privilege adjustment");
                return false;
            }

            try
            {
                if (!LookupPrivilegeValue(null, SE_DEBUG_NAME, out LUID luid))
                {
                    Log.Warn("Failed to lookup SeDebugPrivilege value");
                    return false;
                }

                var tokenPrivileges = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Privileges = new LUID_AND_ATTRIBUTES[]
                    {
                        new LUID_AND_ATTRIBUTES
                        {
                            Luid = luid,
                            Attributes = SE_PRIVILEGE_ENABLED
                        }
                    }
                };

                if (!AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    Log.Warn("Failed to adjust token privileges for SeDebugPrivilege");
                    return false;
                }

                var lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_NOT_ALL_ASSIGNED)
                {
                    Log.Warn("SeDebugPrivilege not all assigned - may require administrator rights");
                    return false;
                }

                Log.Info("SeDebugPrivilege enabled successfully");
                return true;
            }
            finally
            {
                CloseHandle(tokenHandle);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Exception while enabling SeDebugPrivilege: {ex.Message}");
            return false;
        }
    }

    #region Win32 API
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;
    private const int ERROR_NOT_ALL_ASSIGNED = 1300;

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public int PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }
    #endregion
}