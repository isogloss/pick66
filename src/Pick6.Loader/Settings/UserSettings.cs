using System.Text.Json.Serialization;

namespace Pick6.Loader.Settings;

/// <summary>
/// User settings for Pick6 application
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Automatically start projection when the application loads
    /// </summary>
    [JsonPropertyName("autoStartProjection")]
    public bool AutoStartProjection { get; set; } = false;

    /// <summary>
    /// Enable verbose logging output
    /// </summary>
    [JsonPropertyName("verboseLogging")]
    public bool VerboseLogging { get; set; } = false;

    /// <summary>
    /// Projection refresh interval in milliseconds
    /// </summary>
    [JsonPropertyName("projectionRefreshIntervalMs")]
    public int ProjectionRefreshIntervalMs { get; set; } = 500;

    /// <summary>
    /// Hotkey to toggle projection (stored as string form)
    /// </summary>
    [JsonPropertyName("hotkeyToggleProjection")]
    public string HotkeyToggleProjection { get; set; } = "Ctrl+P";

    /// <summary>
    /// Hotkey to stop projection and restore menu
    /// </summary>
    [JsonPropertyName("hotkeyStopAndRestore")]
    public string HotkeyStopAndRestore { get; set; } = "Ctrl+Shift+P";

    /// <summary>
    /// Output directory for captures and logs
    /// </summary>
    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory { get; set; } = "output";

    /// <summary>
    /// Validate settings values and clamp to acceptable ranges
    /// </summary>
    public void Validate()
    {
        // Clamp ProjectionRefreshIntervalMs to valid range
        if (ProjectionRefreshIntervalMs < 50)
        {
            ProjectionRefreshIntervalMs = 50;
        }
        else if (ProjectionRefreshIntervalMs > 10000)
        {
            ProjectionRefreshIntervalMs = 10000;
        }

        // Ensure output directory is not null or empty
        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            OutputDirectory = "output";
        }

        // Ensure hotkeys are not null or empty
        if (string.IsNullOrWhiteSpace(HotkeyToggleProjection))
        {
            HotkeyToggleProjection = "Ctrl+P";
        }

        if (string.IsNullOrWhiteSpace(HotkeyStopAndRestore))
        {
            HotkeyStopAndRestore = "Ctrl+Shift+P";
        }
    }
}