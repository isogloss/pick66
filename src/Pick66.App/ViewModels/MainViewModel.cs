using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Pick66.Core;
using Pick66.App.Commands;

namespace Pick66.App.ViewModels;

/// <summary>
/// Main view model for the Pick66 lottery application
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly INumberPickerService _numberPickerService;
    private int _ticketCount = 1;
    private int _numbersPerTicket = 6;
    private int _minInclusive = 1;
    private int _maxInclusive = 49;
    private bool _unique = true;
    private bool _isBusy;
    private string _busyMessage = "Working...";
    private CancellationTokenSource? _cancellationTokenSource;

    public MainViewModel() : this(new NumberPickerService())
    {
    }

    public MainViewModel(INumberPickerService numberPickerService)
    {
        _numberPickerService = numberPickerService ?? throw new ArgumentNullException(nameof(numberPickerService));
        Tickets = new ObservableCollection<string>();
        GenerateCommand = new AsyncRelayCommand(GenerateTicketsAsync, () => !IsBusy);
        ClearCommand = new RelayCommand(ClearTickets, () => !IsBusy && Tickets.Count > 0);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    #region Properties

    public int TicketCount
    {
        get => _ticketCount;
        set
        {
            if (SetProperty(ref _ticketCount, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    public int NumbersPerTicket
    {
        get => _numbersPerTicket;
        set
        {
            if (SetProperty(ref _numbersPerTicket, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    public int MinInclusive
    {
        get => _minInclusive;
        set
        {
            if (SetProperty(ref _minInclusive, value))
            {
                if (_minInclusive > _maxInclusive)
                {
                    MaxInclusive = _minInclusive;
                }
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    public int MaxInclusive
    {
        get => _maxInclusive;
        set
        {
            if (SetProperty(ref _maxInclusive, value))
            {
                if (_maxInclusive < _minInclusive)
                {
                    MinInclusive = _maxInclusive;
                }
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    public bool Unique
    {
        get => _unique;
        set
        {
            if (SetProperty(ref _unique, value))
            {
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    public string BusyMessage
    {
        get => _busyMessage;
        private set => SetProperty(ref _busyMessage, value);
    }

    public ObservableCollection<string> Tickets { get; }

    public bool CanGenerate
    {
        get
        {
            if (IsBusy) return false;
            if (TicketCount <= 0 || NumbersPerTicket <= 0) return false;
            if (MinInclusive > MaxInclusive) return false;
            if (Unique && NumbersPerTicket > (MaxInclusive - MinInclusive + 1)) return false;
            return true;
        }
    }

    #endregion

    #region Commands

    public AsyncRelayCommand GenerateCommand { get; }
    public RelayCommand ClearCommand { get; }

    #endregion

    #region Methods

    private async Task GenerateTicketsAsync()
    {
        if (!CanGenerate) return;

        try
        {
            IsBusy = true;
            _cancellationTokenSource = new CancellationTokenSource();

            // Clear previous results
            Tickets.Clear();

            // Update busy message
            if (TicketCount == 1)
            {
                BusyMessage = "Generating ticket...";
            }
            else
            {
                BusyMessage = $"Generating {TicketCount} tickets...";
            }

            var progress = new Progress<int>(count =>
            {
                if (TicketCount > 1)
                {
                    BusyMessage = $"Generated {count} of {TicketCount} tickets...";
                }
            });

            var tickets = await _numberPickerService.GenerateTicketsAsync(
                TicketCount, 
                NumbersPerTicket, 
                MinInclusive, 
                MaxInclusive, 
                Unique, 
                progress, 
                _cancellationTokenSource.Token);

            // Add results to collection
            for (int i = 0; i < tickets.Count; i++)
            {
                var numbers = string.Join(" ", tickets[i].Select(n => n.ToString("D2")));
                Tickets.Add($"Ticket {i + 1:D3}: {numbers}");
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled - do nothing
        }
        catch (Exception ex)
        {
            Tickets.Add($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = "Working...";
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void ClearTickets()
    {
        Tickets.Clear();
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