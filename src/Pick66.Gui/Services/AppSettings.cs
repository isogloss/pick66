using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Pick66.Gui.Services;

/// <summary>
/// Application settings model with validation and property change notification
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    private bool _isDarkTheme = true;
    private bool _autoStartProjection = false;
    private bool _verboseLogging = false;
    private int _projectionRefreshIntervalMs = 500;
    private int _targetFps = 60;
    private int _resolutionWidth = 0; // 0 = auto
    private int _resolutionHeight = 0; // 0 = auto
    private int _monitorIndex = 0;
    private bool _hardwareAcceleration = true;
    private string _outputDirectory = "output";
    private string _hotkeyToggleProjection = "Ctrl+P";
    private string _hotkeyStopAndRestore = "Ctrl+Shift+P";

    // Lottery settings
    private int _lotteryNumbersPerTicket = 6;
    private int _lotteryMinNumber = 1;
    private int _lotteryMaxNumber = 49;
    private bool _lotteryUniqueNumbers = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Dark theme enabled
    /// </summary>
    [JsonPropertyName("isDarkTheme")]
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => SetProperty(ref _isDarkTheme, value);
    }

    /// <summary>
    /// Automatically start projection when the application loads
    /// </summary>
    [JsonPropertyName("autoStartProjection")]
    public bool AutoStartProjection
    {
        get => _autoStartProjection;
        set => SetProperty(ref _autoStartProjection, value);
    }

    /// <summary>
    /// Enable verbose logging output
    /// </summary>
    [JsonPropertyName("verboseLogging")]
    public bool VerboseLogging
    {
        get => _verboseLogging;
        set => SetProperty(ref _verboseLogging, value);
    }

    /// <summary>
    /// Projection refresh interval in milliseconds
    /// </summary>
    [JsonPropertyName("projectionRefreshIntervalMs")]
    public int ProjectionRefreshIntervalMs
    {
        get => _projectionRefreshIntervalMs;
        set => SetProperty(ref _projectionRefreshIntervalMs, value);
    }

    /// <summary>
    /// Target FPS for capture and projection
    /// </summary>
    [JsonPropertyName("targetFps")]
    public int TargetFps
    {
        get => _targetFps;
        set => SetProperty(ref _targetFps, value);
    }

    /// <summary>
    /// Resolution width (0 for auto)
    /// </summary>
    [JsonPropertyName("resolutionWidth")]
    public int ResolutionWidth
    {
        get => _resolutionWidth;
        set => SetProperty(ref _resolutionWidth, value);
    }

    /// <summary>
    /// Resolution height (0 for auto)
    /// </summary>
    [JsonPropertyName("resolutionHeight")]
    public int ResolutionHeight
    {
        get => _resolutionHeight;
        set => SetProperty(ref _resolutionHeight, value);
    }

    /// <summary>
    /// Monitor index for projection
    /// </summary>
    [JsonPropertyName("monitorIndex")]
    public int MonitorIndex
    {
        get => _monitorIndex;
        set => SetProperty(ref _monitorIndex, value);
    }

    /// <summary>
    /// Enable hardware acceleration
    /// </summary>
    [JsonPropertyName("hardwareAcceleration")]
    public bool HardwareAcceleration
    {
        get => _hardwareAcceleration;
        set => SetProperty(ref _hardwareAcceleration, value);
    }

    /// <summary>
    /// Output directory for captures and logs
    /// </summary>
    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetProperty(ref _outputDirectory, value);
    }

    /// <summary>
    /// Hotkey to toggle projection
    /// </summary>
    [JsonPropertyName("hotkeyToggleProjection")]
    public string HotkeyToggleProjection
    {
        get => _hotkeyToggleProjection;
        set => SetProperty(ref _hotkeyToggleProjection, value);
    }

    /// <summary>
    /// Hotkey to stop projection and restore menu
    /// </summary>
    [JsonPropertyName("hotkeyStopAndRestore")]
    public string HotkeyStopAndRestore
    {
        get => _hotkeyStopAndRestore;
        set => SetProperty(ref _hotkeyStopAndRestore, value);
    }

    /// <summary>
    /// Numbers per lottery ticket
    /// </summary>
    [JsonPropertyName("lotteryNumbersPerTicket")]
    public int LotteryNumbersPerTicket
    {
        get => _lotteryNumbersPerTicket;
        set => SetProperty(ref _lotteryNumbersPerTicket, value);
    }

    /// <summary>
    /// Minimum lottery number
    /// </summary>
    [JsonPropertyName("lotteryMinNumber")]
    public int LotteryMinNumber
    {
        get => _lotteryMinNumber;
        set => SetProperty(ref _lotteryMinNumber, value);
    }

    /// <summary>
    /// Maximum lottery number
    /// </summary>
    [JsonPropertyName("lotteryMaxNumber")]
    public int LotteryMaxNumber
    {
        get => _lotteryMaxNumber;
        set => SetProperty(ref _lotteryMaxNumber, value);
    }

    /// <summary>
    /// Generate unique numbers only
    /// </summary>
    [JsonPropertyName("lotteryUniqueNumbers")]
    public bool LotteryUniqueNumbers
    {
        get => _lotteryUniqueNumbers;
        set => SetProperty(ref _lotteryUniqueNumbers, value);
    }

    /// <summary>
    /// Validate and fix any invalid values
    /// </summary>
    public void Validate()
    {
        // Clamp numeric values to valid ranges
        if (ProjectionRefreshIntervalMs < 50) ProjectionRefreshIntervalMs = 50;
        if (ProjectionRefreshIntervalMs > 10000) ProjectionRefreshIntervalMs = 10000;
        
        if (TargetFps < 1) TargetFps = 1;
        if (TargetFps > 600) TargetFps = 600;
        
        if (ResolutionWidth < 0) ResolutionWidth = 0;
        if (ResolutionHeight < 0) ResolutionHeight = 0;
        
        if (MonitorIndex < 0) MonitorIndex = 0;
        
        // Validate strings
        if (string.IsNullOrWhiteSpace(OutputDirectory))
            OutputDirectory = "output";
            
        if (string.IsNullOrWhiteSpace(HotkeyToggleProjection))
            HotkeyToggleProjection = "Ctrl+P";
            
        if (string.IsNullOrWhiteSpace(HotkeyStopAndRestore))
            HotkeyStopAndRestore = "Ctrl+Shift+P";
            
        // Validate lottery settings
        if (LotteryNumbersPerTicket < 1) LotteryNumbersPerTicket = 1;
        if (LotteryMinNumber < 1) LotteryMinNumber = 1;
        if (LotteryMaxNumber < LotteryMinNumber) LotteryMaxNumber = LotteryMinNumber;
        
        // If unique numbers requested, ensure range is sufficient
        if (LotteryUniqueNumbers && (LotteryMaxNumber - LotteryMinNumber + 1) < LotteryNumbersPerTicket)
        {
            LotteryUniqueNumbers = false; // Allow duplicates if range insufficient
        }
    }

    private void SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}