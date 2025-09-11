using Pick6.Core;

namespace Pick6.Loader.Update;

/// <summary>
/// Handles reading and writing the current payload version information
/// </summary>
public class VersionStore
{
    private static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static readonly string Pick6DataPath = Path.Combine(AppDataPath, "Pick6");
    private static readonly string VersionFilePath = Path.Combine(Pick6DataPath, "payload_version.txt");

    /// <summary>
    /// Gets the currently cached payload version, or null if none exists
    /// </summary>
    public static string? GetCurrentVersion()
    {
        try
        {
            if (File.Exists(VersionFilePath))
            {
                var version = File.ReadAllText(VersionFilePath).Trim();
                return string.IsNullOrEmpty(version) ? null : version;
            }
            return null;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to read payload version: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sets the current payload version
    /// </summary>
    public static bool SetCurrentVersion(string version)
    {
        try
        {
            Directory.CreateDirectory(Pick6DataPath);
            File.WriteAllText(VersionFilePath, version);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to write payload version: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the payload cache directory path
    /// </summary>
    public static string GetPayloadCachePath()
    {
        return Path.Combine(Pick6DataPath, "payload");
    }

    /// <summary>
    /// Ensures the payload cache directory exists
    /// </summary>
    public static bool EnsurePayloadCacheDirectory()
    {
        try
        {
            var cachePath = GetPayloadCachePath();
            Directory.CreateDirectory(cachePath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to create payload cache directory: {ex.Message}");
            return false;
        }
    }
}