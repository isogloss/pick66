using System.Windows;
using Microsoft.Win32;
using Pick66.Gui.Services;

namespace Pick66.Gui.Views;

/// <summary>
/// Settings window for configuring application preferences
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    
    public AppSettings Settings { get; private set; }

    public SettingsWindow(AppSettings currentSettings, SettingsService settingsService)
    {
        InitializeComponent();
        
        _settingsService = settingsService;
        Settings = new AppSettings
        {
            // Copy all current settings
            IsDarkTheme = currentSettings.IsDarkTheme,
            AutoStartProjection = currentSettings.AutoStartProjection,
            VerboseLogging = currentSettings.VerboseLogging,
            ProjectionRefreshIntervalMs = currentSettings.ProjectionRefreshIntervalMs,
            TargetFps = currentSettings.TargetFps,
            ResolutionWidth = currentSettings.ResolutionWidth,
            ResolutionHeight = currentSettings.ResolutionHeight,
            MonitorIndex = currentSettings.MonitorIndex,
            HardwareAcceleration = currentSettings.HardwareAcceleration,
            OutputDirectory = currentSettings.OutputDirectory,
            HotkeyToggleProjection = currentSettings.HotkeyToggleProjection,
            HotkeyStopAndRestore = currentSettings.HotkeyStopAndRestore,
            LotteryNumbersPerTicket = currentSettings.LotteryNumbersPerTicket,
            LotteryMinNumber = currentSettings.LotteryMinNumber,
            LotteryMaxNumber = currentSettings.LotteryMaxNumber,
            LotteryUniqueNumbers = currentSettings.LotteryUniqueNumbers
        };
        
        LoadSettingsToUI();
    }

    private void LoadSettingsToUI()
    {
        // General
        DarkThemeCheckBox.IsChecked = Settings.IsDarkTheme;
        
        // Game Capture
        AutoStartCheckBox.IsChecked = Settings.AutoStartProjection;
        TargetFpsBox.Text = Settings.TargetFps.ToString();
        ResolutionWidthBox.Text = Settings.ResolutionWidth.ToString();
        ResolutionHeightBox.Text = Settings.ResolutionHeight.ToString();
        MonitorIndexBox.Text = Settings.MonitorIndex.ToString();
        HardwareAccelCheckBox.IsChecked = Settings.HardwareAcceleration;
        
        // Logging
        VerboseLoggingCheckBox.IsChecked = Settings.VerboseLogging;
        OutputDirectoryBox.Text = Settings.OutputDirectory;
        
        // Hotkeys
        ToggleHotkeyBox.Text = Settings.HotkeyToggleProjection;
        StopHotkeyBox.Text = Settings.HotkeyStopAndRestore;
        
        // Lottery
        LotteryNumbersBox.Text = Settings.LotteryNumbersPerTicket.ToString();
        LotteryMinBox.Text = Settings.LotteryMinNumber.ToString();
        LotteryMaxBox.Text = Settings.LotteryMaxNumber.ToString();
        LotteryUniqueCheckBox.IsChecked = Settings.LotteryUniqueNumbers;
    }

    private bool ValidateAndSaveSettings()
    {
        try
        {
            // General
            Settings.IsDarkTheme = DarkThemeCheckBox.IsChecked == true;
            
            // Game Capture
            Settings.AutoStartProjection = AutoStartCheckBox.IsChecked == true;
            
            if (!int.TryParse(TargetFpsBox.Text, out int targetFps) || targetFps < 1 || targetFps > 600)
            {
                MessageBox.Show("Target FPS must be between 1 and 600.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Settings.TargetFps = targetFps;
            
            if (!int.TryParse(ResolutionWidthBox.Text, out int width) || width < 0)
            {
                MessageBox.Show("Resolution width must be 0 or greater (0 = auto).", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Settings.ResolutionWidth = width;
            
            if (!int.TryParse(ResolutionHeightBox.Text, out int height) || height < 0)
            {
                MessageBox.Show("Resolution height must be 0 or greater (0 = auto).", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Settings.ResolutionHeight = height;
            
            if (!int.TryParse(MonitorIndexBox.Text, out int monitorIndex) || monitorIndex < 0)
            {
                MessageBox.Show("Monitor index must be 0 or greater.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Settings.MonitorIndex = monitorIndex;
            
            Settings.HardwareAcceleration = HardwareAccelCheckBox.IsChecked == true;
            
            // Logging
            Settings.VerboseLogging = VerboseLoggingCheckBox.IsChecked == true;
            Settings.OutputDirectory = OutputDirectoryBox.Text?.Trim() ?? "output";
            
            // Hotkeys
            Settings.HotkeyToggleProjection = ToggleHotkeyBox.Text?.Trim() ?? "Ctrl+P";
            Settings.HotkeyStopAndRestore = StopHotkeyBox.Text?.Trim() ?? "Ctrl+Shift+P";
            
            // Lottery
            if (!int.TryParse(LotteryNumbersBox.Text, out int numbersPerTicket) || numbersPerTicket < 1)
            {
                MessageBox.Show("Numbers per ticket must be at least 1.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Settings.LotteryNumbersPerTicket = numbersPerTicket;
            
            if (!int.TryParse(LotteryMinBox.Text, out int minNumber) || minNumber < 1)
            {
                MessageBox.Show("Minimum lottery number must be at least 1.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Settings.LotteryMinNumber = minNumber;
            
            if (!int.TryParse(LotteryMaxBox.Text, out int maxNumber) || maxNumber < minNumber)
            {
                MessageBox.Show("Maximum lottery number must be greater than or equal to minimum number.", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Settings.LotteryMaxNumber = maxNumber;
            
            Settings.LotteryUniqueNumbers = LotteryUniqueCheckBox.IsChecked == true;
            
            // Validate lottery constraints
            if (Settings.LotteryUniqueNumbers && (maxNumber - minNumber + 1) < numbersPerTicket)
            {
                var result = MessageBox.Show(
                    $"Cannot generate {numbersPerTicket} unique numbers from range {minNumber}-{maxNumber}.\n\n" +
                    "Would you like to allow duplicate numbers?", 
                    "Invalid Lottery Configuration", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                if (result == MessageBoxResult.Yes)
                {
                    Settings.LotteryUniqueNumbers = false;
                    LotteryUniqueCheckBox.IsChecked = false;
                }
                else
                {
                    return false;
                }
            }
            
            // Final validation
            Settings.Validate();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error validating settings: {ex.Message}", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void BrowseOutputDirectory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderDialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Output Directory"
            };
            
            // Set initial directory if current path exists
            if (System.IO.Directory.Exists(OutputDirectoryBox.Text))
            {
                folderDialog.InitialDirectory = OutputDirectoryBox.Text;
            }
            
            if (folderDialog.ShowDialog() == true)
            {
                OutputDirectoryBox.Text = folderDialog.FolderName;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error browsing for directory: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (ValidateAndSaveSettings())
        {
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}