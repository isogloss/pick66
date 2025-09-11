using System;
using System.Collections.Generic;

namespace Pick6.Core;

/// <summary>
/// Lightweight silent logger abstraction to replace Console.WriteLine calls.
/// No-op by default with pluggable sink support for future diagnostics.
/// </summary>
public static class Log
{
    private static readonly List<ILogSink> _sinks = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Log an informational message
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void Info(string message)
    {
        LogMessage(LogLevel.Info, message);
    }

    /// <summary>
    /// Log a warning message
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void Warn(string message)
    {
        LogMessage(LogLevel.Warning, message);
    }

    /// <summary>
    /// Log an error message
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void Error(string message)
    {
        LogMessage(LogLevel.Error, message);
    }

    /// <summary>
    /// Log a debug message
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void Debug(string message)
    {
        LogMessage(LogLevel.Debug, message);
    }

    /// <summary>
    /// Add a log sink to receive log messages
    /// </summary>
    /// <param name="sink">Sink to add</param>
    public static void AddSink(ILogSink sink)
    {
        if (sink == null) return;
        
        lock (_lock)
        {
            _sinks.Add(sink);
        }
    }

    /// <summary>
    /// Remove a log sink
    /// </summary>
    /// <param name="sink">Sink to remove</param>
    public static void RemoveSink(ILogSink sink)
    {
        if (sink == null) return;
        
        lock (_lock)
        {
            _sinks.Remove(sink);
        }
    }

    /// <summary>
    /// Clear all log sinks
    /// </summary>
    public static void ClearSinks()
    {
        lock (_lock)
        {
            _sinks.Clear();
        }
    }

    private static void LogMessage(LogLevel level, string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        lock (_lock)
        {
            // By default, this is a no-op (silent logger)
            // Log sinks can be added for debugging or diagnostics
            foreach (var sink in _sinks)
            {
                try
                {
                    sink.WriteLog(level, DateTime.Now, message);
                }
                catch
                {
                    // Ignore sink errors to prevent logging from breaking the application
                }
            }
        }
    }
}

/// <summary>
/// Log levels for Pick6 logging system
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// Interface for log sinks that can receive log messages
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Write a log message to the sink
    /// </summary>
    /// <param name="level">Log level</param>
    /// <param name="timestamp">Message timestamp</param>
    /// <param name="message">Log message</param>
    void WriteLog(LogLevel level, DateTime timestamp, string message);
}

/// <summary>
/// Debug log sink that outputs to System.Diagnostics.Debug
/// </summary>
public class DebugLogSink : ILogSink
{
    public void WriteLog(LogLevel level, DateTime timestamp, string message)
    {
        System.Diagnostics.Debug.WriteLine($"[{timestamp:HH:mm:ss.fff}] [{level}] {message}");
    }
}

/// <summary>
/// File log sink that writes to a specified file
/// </summary>
public class FileLogSink : ILogSink
{
    private readonly string _filePath;
    private readonly object _fileLock = new();

    public FileLogSink(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public void WriteLog(LogLevel level, DateTime timestamp, string message)
    {
        lock (_fileLock)
        {
            try
            {
                var logLine = $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(_filePath, logLine);
            }
            catch
            {
                // Ignore file write errors
            }
        }
    }
}