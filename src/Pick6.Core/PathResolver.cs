using System;
using System.IO;

namespace Pick6.Core;

/// <summary>
/// Provides robust path resolution for DLL files, handling single-file publishing scenarios
/// where AppDomain.CurrentDomain.BaseDirectory returns a temporary extraction directory
/// </summary>
public static class PathResolver
{
    /// <summary>
    /// Get the directory where the executable is actually located,
    /// not the temp extraction directory used by single-file publishing
    /// </summary>
    public static string GetExecutableDirectory()
    {
        // Environment.ProcessPath returns the actual executable path, not the temp extraction path
        // This works correctly with single-file publishing
        var processPath = Environment.ProcessPath;
        
        if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
        {
            var dir = Path.GetDirectoryName(processPath);
            if (!string.IsNullOrEmpty(dir))
            {
                return dir;
            }
        }
        
        // Fallback to base directory if ProcessPath is not available
        // This can happen in some edge cases or older .NET versions
        return AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <summary>
    /// Get the full path to a DLL file in the executable directory
    /// </summary>
    /// <param name="dllFileName">Name of the DLL file (e.g., "Pick6VulkanHook.dll")</param>
    /// <returns>Full path to the DLL file</returns>
    public static string GetDllPath(string dllFileName)
    {
        var executableDir = GetExecutableDirectory();
        return Path.Combine(executableDir, dllFileName);
    }

    /// <summary>
    /// Find a DLL file by checking multiple possible locations
    /// </summary>
    /// <param name="dllFileName">Name of the DLL file to find</param>
    /// <param name="additionalSearchPaths">Additional directories to search</param>
    /// <returns>Full path to the DLL if found, otherwise the default path in the executable directory</returns>
    public static string FindDll(string dllFileName, params string[] additionalSearchPaths)
    {
        var executableDir = GetExecutableDirectory();
        
        // First, try the executable directory (most common case)
        var primaryPath = Path.Combine(executableDir, dllFileName);
        if (File.Exists(primaryPath))
        {
            return primaryPath;
        }
        
        // Try extracting from embedded resources
        var extractedPath = ResourceExtractor.TryExtractDll(dllFileName);
        if (extractedPath != null && File.Exists(extractedPath))
        {
            return extractedPath;
        }
        
        // Try additional search paths
        foreach (var searchPath in additionalSearchPaths)
        {
            if (string.IsNullOrEmpty(searchPath))
                continue;
                
            var candidatePath = Path.Combine(searchPath, dllFileName);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }
        
        // If AppDomain.BaseDirectory is different from executable directory, try that too
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (baseDir != executableDir)
        {
            var baseDirPath = Path.Combine(baseDir, dllFileName);
            if (File.Exists(baseDirPath))
            {
                return baseDirPath;
            }
        }
        
        // Return the primary path even if it doesn't exist, for error message clarity
        return primaryPath;
    }

    /// <summary>
    /// Find a proxy DLL by checking multiple naming conventions and locations
    /// </summary>
    /// <param name="proxyDllName">Standard proxy DLL name (e.g., "dxgi.dll")</param>
    /// <returns>Full path to the proxy DLL if found, otherwise the default path</returns>
    public static string FindProxyDll(string proxyDllName)
    {
        var executableDir = GetExecutableDirectory();
        
        // Try exact filename first (pre-built proxy DLL)
        var exactPath = Path.Combine(executableDir, proxyDllName);
        if (File.Exists(exactPath))
        {
            return exactPath;
        }
        
        // Try with Pick6 prefix naming convention
        // e.g., "dxgi.dll" -> "Pick6DxgiProxy.dll"
        var baseName = Path.GetFileNameWithoutExtension(proxyDllName);
        var prefixedName = $"Pick6{baseName}Proxy.dll";
        var prefixedPath = Path.Combine(executableDir, prefixedName);
        if (File.Exists(prefixedPath))
        {
            return prefixedPath;
        }
        
        // Try in proxy subdirectory
        var proxyDir = Path.Combine(executableDir, "proxies");
        if (Directory.Exists(proxyDir))
        {
            var proxySubPath = Path.Combine(proxyDir, proxyDllName);
            if (File.Exists(proxySubPath))
            {
                return proxySubPath;
            }
        }
        
        // If AppDomain.BaseDirectory is different, check there too
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (baseDir != executableDir)
        {
            var baseDirPath = Path.Combine(baseDir, proxyDllName);
            if (File.Exists(baseDirPath))
            {
                return baseDirPath;
            }
            
            var baseDirProxyPath = Path.Combine(baseDir, "proxies", proxyDllName);
            if (File.Exists(baseDirProxyPath))
            {
                return baseDirProxyPath;
            }
        }
        
        // Return exact path as fallback (even if doesn't exist, for error message clarity)
        return exactPath;
    }

    /// <summary>
    /// Get diagnostic information about path resolution
    /// </summary>
    public static string GetDiagnosticInfo()
    {
        var execDir = GetExecutableDirectory();
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var processPath = Environment.ProcessPath ?? "(null)";
        
        return $"Executable Directory: {execDir}\n" +
               $"Base Directory: {baseDir}\n" +
               $"Process Path: {processPath}\n" +
               $"Same Directory: {execDir == baseDir}";
    }
}
