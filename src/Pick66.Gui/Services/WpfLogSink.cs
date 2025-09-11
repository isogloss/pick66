using Microsoft.Extensions.Logging;
using Pick6.Core;
using Pick66.Gui.Services;

namespace Pick66.Gui.Services;

/// <summary>
/// Log sink that integrates Pick6.Core logging with the WPF logging service
/// </summary>
public class WpfLogSink : ILogSink
{
    private readonly LoggingService _loggingService;

    public WpfLogSink(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public void WriteLog(Pick6.Core.LogLevel level, DateTime timestamp, string message)
    {
        // Convert Pick6.Core.LogLevel to Microsoft.Extensions.Logging.LogLevel
        var microsoftLogLevel = level switch
        {
            Pick6.Core.LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
            Pick6.Core.LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
            Pick6.Core.LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            Pick6.Core.LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            _ => Microsoft.Extensions.Logging.LogLevel.Information
        };
        
        _loggingService.AddLogEntry(microsoftLogLevel, message, "Pick6.Core");
    }
}