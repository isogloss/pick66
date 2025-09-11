using Pick6.Core;

namespace Pick6.Loader.Logging;

/// <summary>
/// Log sink that forwards log messages to the GUI
/// </summary>
public class GuiLogSink : ILogSink
{
    private readonly int _maxEntries;

    public event EventHandler<LogEventArgs>? LogReceived;

    public GuiLogSink(int maxEntries = 200)
    {
        _maxEntries = maxEntries;
    }

    public void WriteLog(LogLevel level, DateTime timestamp, string message)
    {
        var eventArgs = new LogEventArgs(level.ToString(), timestamp, message);
        LogReceived?.Invoke(this, eventArgs);
    }
}

/// <summary>
/// Event args for log events from the GUI sink
/// </summary>
public class LogEventArgs : EventArgs
{
    public DateTime Timestamp { get; }
    public string Level { get; }
    public string Message { get; }

    public LogEventArgs(string level, DateTime timestamp, string message)
    {
        Level = level;
        Timestamp = timestamp;
        Message = message;
    }
}