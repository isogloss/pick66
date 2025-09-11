using System.Text.Json.Serialization;

namespace Pick6.ModGui;

/// <summary>
/// ImGui-specific user settings
/// </summary>
public class ImGuiSettings
{
    /// <summary>
    /// Target FPS for capture
    /// </summary>
    [JsonPropertyName("targetFPS")]
    public int TargetFPS { get; set; } = 60;

    /// <summary>
    /// Resolution width (0 = auto)
    /// </summary>
    [JsonPropertyName("resolutionWidth")]
    public int ResolutionWidth { get; set; } = 0;

    /// <summary>
    /// Resolution height (0 = auto)
    /// </summary>
    [JsonPropertyName("resolutionHeight")]
    public int ResolutionHeight { get; set; } = 0;

    /// <summary>
    /// Hardware acceleration enabled
    /// </summary>
    [JsonPropertyName("hardwareAcceleration")]
    public bool HardwareAcceleration { get; set; } = true;

    /// <summary>
    /// Auto-start projection
    /// </summary>
    [JsonPropertyName("autoStartProjection")]
    public bool AutoStartProjection { get; set; } = false;

    /// <summary>
    /// UI scale factor
    /// </summary>
    [JsonPropertyName("uiScale")]
    public float UiScale { get; set; } = 1.0f;

    /// <summary>
    /// Monitor index for projection
    /// </summary>
    [JsonPropertyName("monitorIndex")]
    public int MonitorIndex { get; set; } = 0;

    /// <summary>
    /// Validate and clamp settings to acceptable ranges
    /// </summary>
    public void Validate()
    {
        TargetFPS = Math.Max(1, Math.Min(600, TargetFPS));
        ResolutionWidth = Math.Max(0, ResolutionWidth);
        ResolutionHeight = Math.Max(0, ResolutionHeight);
        UiScale = Math.Max(0.5f, Math.Min(3.0f, UiScale));
        MonitorIndex = Math.Max(0, MonitorIndex);
    }
}