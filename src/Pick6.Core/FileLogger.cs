using System;
using System.IO;
using System.Text;

namespace Pick6.Core;

/// <summary>
/// Rolling file logger for injection operations
/// </summary>
public class FileLogger : ILogSink, IDisposable
{
    private readonly string _logFilePath;
    private readonly long _maxFileSize;
    private readonly int _maxArchives;
    private readonly object _lockObject = new();
    private StreamWriter? _writer;
    private bool _disposed = false;

    public FileLogger(string logFilePath, long maxFileSize = 1024 * 1024, int maxArchives = 2)
    {
        _logFilePath = logFilePath;
        _maxFileSize = maxFileSize;
        _maxArchives = maxArchives;

        // Ensure log directory exists
        var logDir = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        InitializeWriter();
    }

    public void WriteLog(LogLevel level, DateTime timestamp, string message)
    {
        if (_disposed) return;

        lock (_lockObject)
        {
            try
            {
                CheckRollover();

                var timestampStr = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var levelStr = level.ToString().ToUpper().PadRight(7);
                var logLine = $"{timestampStr} [{levelStr}] {message}";

                _writer?.WriteLine(logLine);
                _writer?.Flush();
            }
            catch (Exception ex)
            {
                // Fallback to debug output if file logging fails
                System.Diagnostics.Debug.WriteLine($"FileLogger error: {ex.Message}");
            }
        }
    }

    private void InitializeWriter()
    {
        try
        {
            _writer?.Dispose();
            _writer = new StreamWriter(_logFilePath, append: true, encoding: Encoding.UTF8)
            {
                AutoFlush = false // We'll flush manually for better performance
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize file logger: {ex.Message}");
        }
    }

    private void CheckRollover()
    {
        try
        {
            if (!File.Exists(_logFilePath)) return;

            var fileInfo = new FileInfo(_logFilePath);
            if (fileInfo.Length <= _maxFileSize) return;

            // Close current writer
            _writer?.Dispose();
            _writer = null;

            // Rotate log files
            RotateLogFiles();

            // Reinitialize writer
            InitializeWriter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Log rotation failed: {ex.Message}");
            // Try to reinitialize writer even if rotation failed
            InitializeWriter();
        }
    }

    private void RotateLogFiles()
    {
        var directory = Path.GetDirectoryName(_logFilePath) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(_logFilePath);
        var extension = Path.GetExtension(_logFilePath);

        // Remove oldest archive if we're at the limit
        var oldestArchive = Path.Combine(directory, $"{fileNameWithoutExt}.{_maxArchives}{extension}");
        if (File.Exists(oldestArchive))
        {
            File.Delete(oldestArchive);
        }

        // Shift existing archives
        for (int i = _maxArchives - 1; i >= 1; i--)
        {
            var currentArchive = Path.Combine(directory, $"{fileNameWithoutExt}.{i}{extension}");
            var nextArchive = Path.Combine(directory, $"{fileNameWithoutExt}.{i + 1}{extension}");
            
            if (File.Exists(currentArchive))
            {
                File.Move(currentArchive, nextArchive);
            }
        }

        // Move current log to .1
        var firstArchive = Path.Combine(directory, $"{fileNameWithoutExt}.1{extension}");
        File.Move(_logFilePath, firstArchive);
    }

    public void Dispose()
    {
        lock (_lockObject)
        {
            if (_disposed) return;
            _disposed = true;

            _writer?.Dispose();
            _writer = null;
        }
    }
}

/// <summary>
/// Utility to set up file logging for the application
/// </summary>
public static class LoggingSetup
{
    private static FileLogger? _fileLogger;
    private static readonly object _setupLock = new();

    /// <summary>
    /// Initialize file logging to logs/injector.log
    /// </summary>
    public static void InitializeFileLogging()
    {
        lock (_setupLock)
        {
            if (_fileLogger != null) return;

            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "injector.log");
                _fileLogger = new FileLogger(logPath);
                Log.AddSink(_fileLogger);
                
                Log.Info("File logging initialized: " + logPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize file logging: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Shutdown file logging
    /// </summary>
    public static void ShutdownFileLogging()
    {
        lock (_setupLock)
        {
            _fileLogger?.Dispose();
            _fileLogger = null;
        }
    }
}