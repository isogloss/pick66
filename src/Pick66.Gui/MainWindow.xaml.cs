using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Pick6.Core;
using Pick6.Projection;
using Pick66.Core;
using Pick66.Gui.Services;
using Pick66.Gui.Views;
using System.Text;

namespace Pick66.Gui;

/// <summary>
/// Main window for Pick66 - unified lottery and game capture application
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly SettingsService _settingsService;
    private readonly LoggingService _loggingService;
    private AppSettings _settings;
    
    // Game capture components
    private GameCaptureEngine? _captureEngine;
    private BorderlessProjectionWindow? _projectionWindow;
    
    // UI update timer
    private System.Windows.Threading.DispatcherTimer? _uiTimer;
    
    // Lottery service
    private readonly NumberPickerService _lotteryService;

    public MainWindow()
    {
        InitializeComponent();
        
        // Get logging service from app (would be better with DI container)
        _loggingService = ((App)Application.Current).GetLoggingService();
        _logger = _loggingService.CreateLogger<MainWindow>();
        _settingsService = new SettingsService(_loggingService.CreateLogger<SettingsService>());
        
        // Initialize services
        _lotteryService = new NumberPickerService();
        
        // Load settings and initialize
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing main window...");
            
            // Load settings
            _settings = await _settingsService.LoadSettingsAsync();
            ApplyTheme();
            
            // Initialize game capture components
            InitializeGameCapture();
            
            // Setup UI timer for updates
            SetupUiTimer();
            
            // Setup log display
            SetupLogDisplay();
            
            // Auto-start projection if enabled
            if (_settings.AutoStartProjection)
            {
                _logger.LogInformation("Auto-start projection enabled, attempting to start...");
                await Task.Delay(1000); // Give UI time to initialize
                StartCapture_Click(this, new RoutedEventArgs());
            }
            
            _logger.LogInformation("Main window initialized successfully");
            UpdateStatusText("Application ready");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize main window");
            UpdateStatusText("Initialization failed");
            MessageBox.Show($"Initialization error: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InitializeGameCapture()
    {
        try
        {
            _captureEngine = new GameCaptureEngine();
            _projectionWindow = new BorderlessProjectionWindow();
            
            // Configure capture engine with current settings
            _captureEngine.Settings.TargetFPS = _settings.TargetFps;
            _captureEngine.Settings.ScaleWidth = _settings.ResolutionWidth;
            _captureEngine.Settings.ScaleHeight = _settings.ResolutionHeight;
            _captureEngine.Settings.UseHardwareAcceleration = _settings.HardwareAcceleration;
            
            // Setup event handlers
            _captureEngine.FrameCaptured += OnFrameCaptured;
            _captureEngine.ErrorOccurred += OnCaptureError;
            
            _projectionWindow.ProjectionStarted += OnProjectionStarted;
            _projectionWindow.ProjectionStopped += OnProjectionStopped;
            
            _logger.LogInformation("Game capture components initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize game capture components");
        }
    }

    private void SetupUiTimer()
    {
        _uiTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100) // 10 FPS update rate for UI
        };
        _uiTimer.Tick += UpdatePerformanceMetrics;
        _uiTimer.Start();
    }

    private void SetupLogDisplay()
    {
        _loggingService.LogEntryAdded += OnLogEntryAdded;
    }

    private void ApplyTheme()
    {
        var themeUri = _settings.IsDarkTheme 
            ? new Uri("Styles/DarkTheme.xaml", UriKind.Relative)
            : new Uri("Styles/LightTheme.xaml", UriKind.Relative);
            
        try
        {
            var dictionary = new ResourceDictionary { Source = themeUri };
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(dictionary);
            
            ThemeText.Text = _settings.IsDarkTheme ? "Dark Theme" : "Light Theme";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load theme, using default");
            ThemeText.Text = "Default Theme";
        }
    }

    private void UpdateStatusText(string status)
    {
        Dispatcher.BeginInvoke(() => StatusText.Text = status);
    }

    private void OnLogEntryAdded(object? sender, LogEntryEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var logText = $"[{e.Entry.Timestamp:HH:mm:ss}] [{e.Entry.Level}] {e.Entry.Message}";
            LogListBox.Items.Add(logText);
            
            // Keep only last 200 entries
            while (LogListBox.Items.Count > 200)
            {
                LogListBox.Items.RemoveAt(0);
            }
            
            // Auto-scroll to bottom
            if (LogListBox.Items.Count > 0)
            {
                LogListBox.ScrollIntoView(LogListBox.Items[^1]);
            }
        });
    }

    private void UpdatePerformanceMetrics(object? sender, EventArgs e)
    {
        if (_captureEngine?.Statistics != null)
        {
            Dispatcher.BeginInvoke(() =>
            {
                FpsText.Text = $"{_captureEngine.Statistics.FramesPerSecond:F1}";
                DroppedFramesText.Text = _captureEngine.Statistics.DroppedFrames.ToString();
            });
        }
    }

    private void OnFrameCaptured(object? sender, FrameCapturedEventArgs e)
    {
        // Forward frame to projection window
        _projectionWindow?.UpdateFrame(e.Frame);
    }

    private void OnCaptureError(object? sender, string errorMessage)
    {
        _logger.LogError("Capture error: {Error}", errorMessage);
        Dispatcher.BeginInvoke(() =>
        {
            CaptureStatusText.Text = "Error";
            StartCaptureButton.IsEnabled = true;
            StopCaptureButton.IsEnabled = false;
        });
    }

    private void OnProjectionStarted(object? sender, EventArgs e)
    {
        _logger.LogInformation("Projection started");
        Dispatcher.BeginInvoke(() =>
        {
            StartProjectionButton.IsEnabled = false;
            StopProjectionButton.IsEnabled = true;
        });
    }

    private void OnProjectionStopped(object? sender, EventArgs e)
    {
        _logger.LogInformation("Projection stopped");
        Dispatcher.BeginInvoke(() =>
        {
            StartProjectionButton.IsEnabled = true;
            StopProjectionButton.IsEnabled = false;
        });
    }

    // Event Handlers
    private void GenerateTickets_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Parse input values
            if (!int.TryParse(NumbersPerTicketBox.Text, out int numbersPerTicket) || numbersPerTicket < 1)
            {
                MessageBox.Show("Please enter a valid number of numbers per ticket (minimum 1).", 
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(MinNumberBox.Text, out int minNumber) || minNumber < 1)
            {
                MessageBox.Show("Please enter a valid minimum number (minimum 1).", 
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(MaxNumberBox.Text, out int maxNumber) || maxNumber < minNumber)
            {
                MessageBox.Show("Please enter a valid maximum number (must be >= minimum number).", 
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TicketCountBox.Text, out int ticketCount) || ticketCount < 1)
            {
                MessageBox.Show("Please enter a valid number of tickets (minimum 1).", 
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool uniqueNumbers = UniqueNumbersCheckBox.IsChecked == true;

            // Validate unique numbers constraint
            if (uniqueNumbers && (maxNumber - minNumber + 1) < numbersPerTicket)
            {
                MessageBox.Show($"Cannot generate {numbersPerTicket} unique numbers from range {minNumber}-{maxNumber}.", 
                    "Invalid Configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Generate tickets
            var results = new StringBuilder();
            results.AppendLine($"Generated {ticketCount} ticket(s) with {numbersPerTicket} numbers each");
            results.AppendLine($"Range: {minNumber}-{maxNumber}, Unique: {(uniqueNumbers ? "Yes" : "No")}");
            results.AppendLine(new string('=', 50));
            results.AppendLine();

            for (int i = 0; i < ticketCount; i++)
            {
                var ticket = _lotteryService.GenerateTicket(numbersPerTicket, minNumber, maxNumber, uniqueNumbers);
                var formattedNumbers = string.Join(" ", ticket.Select(n => n.ToString("D2")));
                results.AppendLine($"Ticket {i + 1:D3}: {formattedNumbers}");
            }

            ResultsTextBox.Text = results.ToString();
            _logger.LogInformation("Generated {Count} lottery tickets", ticketCount);
            UpdateStatusText($"Generated {ticketCount} tickets");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating lottery tickets");
            MessageBox.Show($"Error generating tickets: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyResults_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(ResultsTextBox.Text))
            {
                Clipboard.SetText(ResultsTextBox.Text);
                UpdateStatusText("Results copied to clipboard");
                _logger.LogInformation("Results copied to clipboard");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy results to clipboard");
            MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveResults_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ResultsTextBox.Text))
            {
                MessageBox.Show("No results to save. Please generate tickets first.", "No Results", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"lottery_tickets_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveDialog.FileName, ResultsTextBox.Text);
                UpdateStatusText($"Results saved to {Path.GetFileName(saveDialog.FileName)}");
                _logger.LogInformation("Results saved to file: {FileName}", saveDialog.FileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save results to file");
            MessageBox.Show($"Failed to save file: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StartCapture_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_captureEngine == null)
            {
                MessageBox.Show("Capture engine not initialized.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Find FiveM processes
            var summary = FiveMDetector.GetProcessSummary();
            if (summary.TotalProcessCount == 0)
            {
                MessageBox.Show("No FiveM processes found. Please start FiveM first.", "No Process Found", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Try to start capture
            string processName = "";
            if (summary.VulkanProcesses.Any())
            {
                processName = summary.VulkanProcesses.First().ProcessName;
                _logger.LogInformation("Attempting Vulkan capture on {ProcessName}", processName);
            }
            else if (summary.TraditionalProcesses.Any())
            {
                processName = summary.TraditionalProcesses.First().ProcessName;
                _logger.LogInformation("Attempting GDI capture on {ProcessName}", processName);
            }

            if (_captureEngine.StartCapture(processName))
            {
                CaptureStatusText.Text = "Capturing";
                StartCaptureButton.IsEnabled = false;
                StopCaptureButton.IsEnabled = true;
                UpdateStatusText("Capture started");
                
                // Auto-start projection if enabled
                if (_settings.AutoStartProjection)
                {
                    StartProjection_Click(sender, e);
                }
            }
            else
            {
                MessageBox.Show("Failed to start capture. Check logs for details.", "Capture Failed", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting capture");
            MessageBox.Show($"Error starting capture: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StopCapture_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _captureEngine?.StopCapture();
            CaptureStatusText.Text = "Idle";
            StartCaptureButton.IsEnabled = true;
            StopCaptureButton.IsEnabled = false;
            UpdateStatusText("Capture stopped");
            _logger.LogInformation("Capture stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping capture");
            MessageBox.Show($"Error stopping capture: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StartProjection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _projectionWindow?.StartProjection(_settings.MonitorIndex);
            _logger.LogInformation("Starting projection on monitor {MonitorIndex}", _settings.MonitorIndex);
            UpdateStatusText("Projection started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting projection");
            MessageBox.Show($"Error starting projection: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StopProjection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _projectionWindow?.StopProjection();
            UpdateStatusText("Projection stopped");
            _logger.LogInformation("Projection stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping projection");
            MessageBox.Show($"Error stopping projection: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settingsWindow = new SettingsWindow(_settings, _settingsService);
            if (settingsWindow.ShowDialog() == true)
            {
                _settings = settingsWindow.Settings;
                ApplyTheme();
                
                // Apply updated settings to capture engine
                if (_captureEngine != null)
                {
                    _captureEngine.Settings.TargetFPS = _settings.TargetFps;
                    _captureEngine.Settings.ScaleWidth = _settings.ResolutionWidth;
                    _captureEngine.Settings.ScaleHeight = _settings.ResolutionHeight;
                    _captureEngine.Settings.UseHardwareAcceleration = _settings.HardwareAcceleration;
                }
                
                UpdateStatusText("Settings updated");
                _logger.LogInformation("Settings updated and applied");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing settings");
            MessageBox.Show($"Error opening settings: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        _settings.IsDarkTheme = !_settings.IsDarkTheme;
        ApplyTheme();
        _ = _settingsService.SaveSettingsAsync(_settings);
        _logger.LogInformation("Theme toggled to {Theme}", _settings.IsDarkTheme ? "Dark" : "Light");
    }

    private void ShowAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutText = """
            Pick66 - Unified Lottery and Game Capture Application
            
            Version: 1.0.0
            Platform: .NET 8 + WPF
            
            Features:
            • Lottery number generation with configurable parameters
            • FiveM game capture with Vulkan injection support
            • Real-time projection to secondary monitors
            • Dark/Light theme support
            • Persistent settings management
            
            © 2024 - Built with modern .NET technologies
            """;
            
        MessageBox.Show(aboutText, "About Pick66", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            // Save settings
            _ = _settingsService.SaveSettingsAsync(_settings);
            
            // Stop timers
            _uiTimer?.Stop();
            
            // Cleanup game capture
            _captureEngine?.StopCapture();
            _projectionWindow?.StopProjection();
            
            // Unsubscribe from events
            _loggingService.LogEntryAdded -= OnLogEntryAdded;
            
            _logger.LogInformation("Main window closed, application shutting down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
        }
        
        base.OnClosed(e);
    }
}