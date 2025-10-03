using System;
using System.IO;
using System.Reflection;

namespace Pick6.Core;

/// <summary>
/// Extracts embedded DLL resources to a temporary directory for use during runtime
/// </summary>
public static class ResourceExtractor
{
    private static readonly string ExtractedDllsDirectory;
    private static readonly object LockObject = new object();
    
    static ResourceExtractor()
    {
        // Create a unique directory for extracted DLLs in the temp folder
        var tempPath = Path.GetTempPath();
        ExtractedDllsDirectory = Path.Combine(tempPath, "Pick6", "Native");
        
        // Ensure the directory exists
        Directory.CreateDirectory(ExtractedDllsDirectory);
    }
    
    /// <summary>
    /// Get the directory where extracted DLLs are stored
    /// </summary>
    public static string GetExtractedDllsDirectory() => ExtractedDllsDirectory;
    
    /// <summary>
    /// Extract an embedded DLL resource to the temp directory
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource (e.g., "Pick6VulkanHook.dll")</param>
    /// <returns>The full path to the extracted DLL</returns>
    public static string ExtractDll(string resourceName)
    {
        lock (LockObject)
        {
            var targetPath = Path.Combine(ExtractedDllsDirectory, resourceName);
            
            // If already extracted, return it
            if (File.Exists(targetPath))
            {
                return targetPath;
            }
            
            // Try to extract from embedded resources
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            
            // Look for the resource (it might have a different name prefix)
            string? foundResourceName = null;
            foreach (var name in resourceNames)
            {
                if (name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
                {
                    foundResourceName = name;
                    break;
                }
            }
            
            if (foundResourceName == null)
            {
                // Resource not found, return the target path anyway
                // The caller will handle the missing file
                return targetPath;
            }
            
            // Extract the resource
            try
            {
                using var stream = assembly.GetManifestResourceStream(foundResourceName);
                if (stream == null)
                {
                    return targetPath;
                }
                
                using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
                stream.CopyTo(fileStream);
                
                return targetPath;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to extract embedded DLL '{resourceName}': {ex.Message}");
                return targetPath;
            }
        }
    }
    
    /// <summary>
    /// Try to extract a DLL if it's embedded, otherwise return null
    /// </summary>
    /// <param name="dllFileName">The name of the DLL file</param>
    /// <returns>Path to extracted DLL if successful, null if not embedded</returns>
    public static string? TryExtractDll(string dllFileName)
    {
        try
        {
            var extractedPath = ExtractDll(dllFileName);
            if (File.Exists(extractedPath))
            {
                return extractedPath;
            }
        }
        catch
        {
            // Ignore extraction errors
        }
        
        return null;
    }
    
    /// <summary>
    /// Clean up old extracted DLLs (useful for updates)
    /// </summary>
    public static void CleanupOldExtractions()
    {
        try
        {
            if (Directory.Exists(ExtractedDllsDirectory))
            {
                var files = Directory.GetFiles(ExtractedDllsDirectory);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore errors (file might be in use)
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
