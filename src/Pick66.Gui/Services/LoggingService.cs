using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Pick66.Gui.Services;

/// <summary>
/// Centralized logging service for the WPF application
/// </summary>
public class LoggingService : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentQueue<LogEntry> _logEntries = new();
    private readonly int _maxLogEntries = 1000;
    
    public event EventHandler<LogEntryEventArgs>? LogEntryAdded;

    public LoggingService()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        });
    }

    public ILogger<T> CreateLogger<T>() => _loggerFactory.CreateLogger<T>();
    
    public void AddLogEntry(Microsoft.Extensions.Logging.LogLevel level, string message, string? source = null)
    {
        var entry = new LogEntry(DateTime.Now, level, message, source ?? "System");
        
        _logEntries.Enqueue(entry);
        
        // Maintain max log entries
        while (_logEntries.Count > _maxLogEntries)
        {
            _logEntries.TryDequeue(out _);
        }
        
        // Notify UI
        LogEntryAdded?.Invoke(this, new LogEntryEventArgs(entry));
    }
    
    public IEnumerable<LogEntry> GetLogEntries() => _logEntries.ToArray();
    
    public void Dispose()
    {
        _loggerFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents a single log entry
/// </summary>
public record LogEntry(DateTime Timestamp, Microsoft.Extensions.Logging.LogLevel Level, string Message, string Source);

/// <summary>
/// Event args for log entry events
/// </summary>
public class LogEntryEventArgs(LogEntry entry) : EventArgs
{
    public LogEntry Entry { get; } = entry;
}