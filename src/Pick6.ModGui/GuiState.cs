using Pick6.Core;
using Pick6.Projection;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Pick6.ModGui;

/// <summary>
/// Singleton state manager for ImGui mod menu
/// </summary>
public sealed class GuiState
{
    private static readonly Lazy<GuiState> _instance = new(() => new GuiState());
    public static GuiState Instance => _instance.Value;

    private readonly ConcurrentQueue<LogEntry> _logEntries = new();
    private const int MaxLogEntries = 1000;

    // UI State
    public float UiScale { get; set; } = 1.0f;
    public int SelectedTabIndex { get; set; } = 0;
    public bool AutoScrollLogs { get; set; } = true;

    // Core References
    public GameCaptureEngine? CaptureEngine { get; set; }
    public BorderlessProjectionWindow? ProjectionWindow { get; set; }

    // Settings
    public ImGuiSettings CurrentSettings { get; set; } = new();

    // Performance Metrics
    public float CurrentFPS { get; set; } = 0.0f;
    public int DroppedFrames { get; set; } = 0;

    // Status
    public bool IsCapturing { get; set; } = false;
    public bool IsProjecting { get; set; } = false;
    public string CurrentStatus { get; set; } = "Idle";

    private GuiState()
    {
        // Load settings
        LoadSettings();
    }

    /// <summary>
    /// Add a log entry to the ring buffer
    /// </summary>
    public void AddLogEntry(string level, DateTime timestamp, string message)
    {
        _logEntries.Enqueue(new LogEntry(level, timestamp, message));
        
        // Keep only the most recent entries
        while (_logEntries.Count > MaxLogEntries)
        {
            _logEntries.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Get all current log entries
    /// </summary>
    public IEnumerable<LogEntry> GetLogEntries()
    {
        return _logEntries.ToArray();
    }

    /// <summary>
    /// Save settings to disk
    /// </summary>
    public void SaveSettings()
    {
        CurrentSettings.Validate();
        UiScale = CurrentSettings.UiScale; // Keep in sync
        
        try
        {
            var settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pick6");
            Directory.CreateDirectory(settingsDir);
            
            var settingsPath = Path.Combine(settingsDir, "imgui_settings.json");
            var json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    private void LoadSettings()
    {
        try
        {
            var settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Pick6");
            var settingsPath = Path.Combine(settingsDir, "imgui_settings.json");
            
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                CurrentSettings = JsonSerializer.Deserialize<ImGuiSettings>(json) ?? new ImGuiSettings();
            }
        }
        catch
        {
            CurrentSettings = new ImGuiSettings();
        }
        
        CurrentSettings.Validate();
        UiScale = CurrentSettings.UiScale;
    }
}

/// <summary>
/// Log entry for the ImGui console
/// </summary>
public record LogEntry(string Level, DateTime Timestamp, string Message);