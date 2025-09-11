using System.Text.Json;
using Pick6.Core;

namespace Pick6.Loader.Settings;

/// <summary>
/// Service for loading and saving user settings
/// </summary>
public static class SettingsService
{
    private static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static readonly string Pick6DataPath = Path.Combine(AppDataPath, "Pick6");
    private static readonly string SettingsFilePath = Path.Combine(Pick6DataPath, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Load user settings from disk, or return default settings if file doesn't exist
    /// </summary>
    /// <returns>UserSettings instance</returns>
    public static UserSettings TryLoadOrDefault()
    {
        try
        {
            return Load();
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load settings, using defaults: {ex.Message}");
            return new UserSettings();
        }
    }

    /// <summary>
    /// Load user settings from disk
    /// </summary>
    /// <returns>UserSettings instance</returns>
    /// <exception cref="FileNotFoundException">If settings file doesn't exist</exception>
    /// <exception cref="InvalidOperationException">If settings file is invalid</exception>
    public static UserSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            throw new FileNotFoundException($"Settings file not found: {SettingsFilePath}");
        }

        var json = File.ReadAllText(SettingsFilePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Settings file is empty");
        }

        var settings = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);
        if (settings == null)
        {
            throw new InvalidOperationException("Failed to deserialize settings");
        }

        // Validate and fix any invalid values
        var originalRefreshInterval = settings.ProjectionRefreshIntervalMs;
        settings.Validate();

        // Log warning if validation changed values
        if (settings.ProjectionRefreshIntervalMs != originalRefreshInterval)
        {
            Log.Warn($"Invalid ProjectionRefreshIntervalMs ({originalRefreshInterval}), clamped to {settings.ProjectionRefreshIntervalMs}");
        }

        return settings;
    }

    /// <summary>
    /// Save user settings to disk
    /// </summary>
    /// <param name="settings">Settings to save</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool Save(UserSettings settings)
    {
        if (settings == null)
        {
            Log.Error("Cannot save null settings");
            return false;
        }

        try
        {
            // Validate settings before saving
            settings.Validate();

            // Ensure directory exists
            Directory.CreateDirectory(Pick6DataPath);

            // Serialize and save
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);

            Log.Info("Settings saved successfully");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save settings: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get the path where settings are stored
    /// </summary>
    /// <returns>Full path to settings file</returns>
    public static string GetSettingsFilePath()
    {
        return SettingsFilePath;
    }

    /// <summary>
    /// Check if settings file exists
    /// </summary>
    /// <returns>True if settings file exists</returns>
    public static bool SettingsExist()
    {
        return File.Exists(SettingsFilePath);
    }
}