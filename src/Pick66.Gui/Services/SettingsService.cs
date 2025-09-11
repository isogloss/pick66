using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Pick66.Gui.Services;

/// <summary>
/// Service for managing application settings with persistence to %APPDATA%/Pick66
/// </summary>
public class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "Pick66");
    
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");
    
    private readonly ILogger<SettingsService> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        EnsureSettingsDirectoryExists();
    }

    /// <summary>
    /// Load settings from disk or return defaults
    /// </summary>
    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                _logger.LogInformation("Settings file not found, using defaults");
                return new AppSettings();
            }

            var json = await File.ReadAllTextAsync(SettingsFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Settings file is empty, using defaults");
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings == null)
            {
                _logger.LogWarning("Failed to deserialize settings, using defaults");
                return new AppSettings();
            }

            // Validate settings
            settings.Validate();
            
            _logger.LogInformation("Settings loaded successfully");
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            return new AppSettings();
        }
    }

    /// <summary>
    /// Save settings to disk
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            settings.Validate();
            
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(SettingsFilePath, json);
            
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
    }

    private void EnsureSettingsDirectoryExists()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create settings directory: {Directory}", SettingsDirectory);
        }
    }
}