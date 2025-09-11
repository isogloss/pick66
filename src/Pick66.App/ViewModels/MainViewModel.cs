using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using Pick66.App.Commands;

namespace Pick66.App.ViewModels;

/// <summary>
/// Main view model for the Pick66 projection application
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private bool _isProjectionActive;
    private string _projectionStatus = "Idle";
    private string _statusMessage = "Ready to start projection";
    private bool _isBusy;
    private CancellationTokenSource? _cancellationTokenSource;

    public MainViewModel()
    {
        LogMessages = new ObservableCollection<string>();
        StartProjectionCommand = new AsyncRelayCommand(StartProjectionAsync, () => !IsProjectionActive && !IsBusy);
        StopProjectionCommand = new RelayCommand(StopProjection, () => IsProjectionActive && !IsBusy);
        ClearLogsCommand = new RelayCommand(ClearLogs, () => LogMessages.Count > 0);
        
        // Add welcome message
        LogMessages.Add($"[{DateTime.Now:HH:mm:ss}] Pick66 Projection Interface initialized");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    #region Properties

    public bool IsProjectionActive
    {
        get => _isProjectionActive;
        private set
        {
            if (SetProperty(ref _isProjectionActive, value))
            {
                OnPropertyChanged(nameof(StatusText));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string ProjectionStatus
    {
        get => _projectionStatus;
        private set => SetProperty(ref _projectionStatus, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string StatusText => IsProjectionActive ? "Running" : "Idle";

    public ObservableCollection<string> LogMessages { get; }

    #endregion

    #region Commands

    public AsyncRelayCommand StartProjectionCommand { get; }
    public RelayCommand StopProjectionCommand { get; }
    public RelayCommand ClearLogsCommand { get; }

    #endregion

    #region Methods

    private async Task StartProjectionAsync()
    {
        if (IsProjectionActive) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Starting projection...";
            AddLogMessage("Starting projection interface...");

            _cancellationTokenSource = new CancellationTokenSource();

            // Simulate startup process
            await Task.Delay(1000, _cancellationTokenSource.Token);
            
            IsProjectionActive = true;
            ProjectionStatus = "Running";
            StatusMessage = "Projection active - monitoring display";
            AddLogMessage("Projection interface started successfully");

            // Start monitoring loop (simulated)
            _ = Task.Run(async () =>
            {
                try
                {
                    while (IsProjectionActive && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(5000, _cancellationTokenSource.Token);
                        if (IsProjectionActive)
                        {
                            AddLogMessage($"Projection active - Status: OK");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
            }, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            AddLogMessage("Projection startup cancelled");
        }
        catch (Exception ex)
        {
            AddLogMessage($"Error starting projection: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void StopProjection()
    {
        if (!IsProjectionActive) return;

        try
        {
            IsProjectionActive = false;
            ProjectionStatus = "Idle";
            StatusMessage = "Ready to start projection";
            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            AddLogMessage("Projection interface stopped");
        }
        catch (Exception ex)
        {
            AddLogMessage($"Error stopping projection: {ex.Message}");
        }
    }

    private void ClearLogs()
    {
        LogMessages.Clear();
        AddLogMessage($"[{DateTime.Now:HH:mm:ss}] Log cleared");
    }

    private void AddLogMessage(string message)
    {
        var timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        
        // Ensure UI thread access
        if (Application.Current?.Dispatcher.CheckAccess() == true)
        {
            LogMessages.Add(timestampedMessage);
        }
        else
        {
            Application.Current?.Dispatcher.BeginInvoke(() => LogMessages.Add(timestampedMessage));
        }

        // Keep only last 100 messages
        while (LogMessages.Count > 100)
        {
            LogMessages.RemoveAt(0);
        }
    }

    #endregion

    #region INotifyPropertyChanged Implementation

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}