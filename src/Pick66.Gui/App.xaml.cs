using System.Windows;
using Microsoft.Extensions.Logging;
using Pick6.Core;
using Pick66.Gui.Services;

namespace Pick66.Gui;

/// <summary>
/// WPF Application entry point for Pick66 - unified lottery and game capture GUI
/// </summary>
public partial class App : Application
{
    private ILogger<App>? _logger;
    private LoggingService? _loggingService;

    public LoggingService GetLoggingService()
    {
        return _loggingService ?? throw new InvalidOperationException("Logging service not initialized");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Initialize logging
            _loggingService = new LoggingService();
            _logger = _loggingService.CreateLogger<App>();
            
            // Set up Pick6.Core logging integration
            Log.AddSink(new WpfLogSink(_loggingService));
            
            _logger.LogInformation("Pick66 GUI application starting...");

            // Handle command line arguments
            if (HandleCommandLineArgs(e.Args))
            {
                Shutdown();
                return;
            }

            // Create and show main window
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
            
            _logger.LogInformation("Main window displayed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start application");
            MessageBox.Show($"Application startup error: {ex.Message}", "Pick66 Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private bool HandleCommandLineArgs(string[] args)
    {
        foreach (var arg in args)
        {
            switch (arg.ToLower())
            {
                case "--help":
                case "-h":
                    ShowHelp();
                    return true;
                    
                case "--check-updates-only":
                    _logger?.LogInformation("Update check requested, skipping GUI");
                    // TODO: Implement update check
                    return true;
            }
        }
        
        return false;
    }

    private void ShowHelp()
    {
        var helpText = """
            Pick66 - Unified Lottery and Game Capture Application
            
            Usage: Pick66.Gui.exe [options]
            
            Options:
              --help, -h              Show this help message
              --check-updates-only    Check for updates and exit (no GUI)
            
            GUI Features:
              • Lottery number generation with configurable parameters
              • Game capture and projection for FiveM
              • Settings management with theme support
              • Real-time status monitoring and logging
            
            For more information, visit: https://github.com/isogloss/pick66
            """;
            
        MessageBox.Show(helpText, "Pick66 Help", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("Application shutting down...");
        _loggingService?.Dispose();
        base.OnExit(e);
    }
}