using System;
using System.Collections.Generic;

namespace Pick6.Core;

/// <summary>
/// Configuration for injection process with fallback strategies
/// </summary>
public class InjectionConfig
{
    /// <summary>
    /// Maximum time to search for processes (milliseconds)
    /// </summary>
    public int ProcessSearchTimeoutMs { get; set; } = 90000; // 90 seconds (increased for stability)

    /// <summary>
    /// Polling interval for process search (milliseconds)
    /// </summary>
    public int PollIntervalMs { get; set; } = 250; // 250ms

    /// <summary>
    /// Time to wait for graphics modules to load (milliseconds)
    /// </summary>
    public int ModuleWaitTimeoutMs { get; set; } = 20000; // 20 seconds (increased for stability)

    /// <summary>
    /// Ordered list of injection strategies to attempt
    /// </summary>
    public InjectionStrategy[] InjectionOrder { get; set; } = 
    {
        InjectionStrategy.Direct,
        InjectionStrategy.DxgiProxy,
        InjectionStrategy.D3D11Proxy,
        InjectionStrategy.VulkanProxy
    };

    /// <summary>
    /// Log verbosity level
    /// </summary>
    public LogLevel LogVerbosity { get; set; } = LogLevel.Info;

    /// <summary>
    /// Get default configuration
    /// </summary>
    public static InjectionConfig Default => new();
}

/// <summary>
/// Available injection strategies
/// </summary>
public enum InjectionStrategy
{
    /// <summary>
    /// Direct LoadLibrary remote thread injection
    /// </summary>
    Direct,

    /// <summary>
    /// Proxy DLL injection via dxgi.dll
    /// </summary>
    DxgiProxy,

    /// <summary>
    /// Proxy DLL injection via d3d11.dll
    /// </summary>
    D3D11Proxy,

    /// <summary>
    /// Proxy DLL injection via vulkan-1.dll
    /// </summary>
    VulkanProxy
}

/// <summary>
/// Result of an injection attempt
/// </summary>
public class InjectionResult
{
    public bool Success { get; set; }
    public InjectionStrategy Strategy { get; set; }
    public string Message { get; set; } = "";
    public Exception? Exception { get; set; }
    
    /// <summary>
    /// Detailed error information from all failed attempts (if applicable)
    /// </summary>
    public List<StrategyFailureInfo>? FailedAttempts { get; set; }

    public static InjectionResult Failed(InjectionStrategy strategy, string message, Exception? ex = null)
    {
        return new InjectionResult
        {
            Success = false,
            Strategy = strategy,
            Message = message,
            Exception = ex
        };
    }

    public static InjectionResult Succeeded(InjectionStrategy strategy, string message = "")
    {
        return new InjectionResult
        {
            Success = true,
            Strategy = strategy,
            Message = message
        };
    }
    
    /// <summary>
    /// Create a failed result with aggregated failure information
    /// </summary>
    public static InjectionResult FailedWithDetails(InjectionStrategy lastStrategy, string message, List<StrategyFailureInfo> failedAttempts)
    {
        return new InjectionResult
        {
            Success = false,
            Strategy = lastStrategy,
            Message = message,
            FailedAttempts = failedAttempts
        };
    }
    
    /// <summary>
    /// Get a comprehensive error summary including all failed strategies
    /// </summary>
    public string GetDetailedErrorMessage()
    {
        if (Success)
            return Message;
            
        var details = new System.Text.StringBuilder();
        details.AppendLine(Message);
        
        if (FailedAttempts != null && FailedAttempts.Count > 0)
        {
            details.AppendLine();
            details.AppendLine("Failed injection strategies:");
            foreach (var failure in FailedAttempts)
            {
                details.AppendLine($"  • {failure.Strategy}: {failure.ErrorMessage}");
                if (failure.Exception != null)
                {
                    details.AppendLine($"    Exception: {failure.Exception.GetType().Name} - {failure.Exception.Message}");
                }
            }
        }
        
        return details.ToString();
    }
}

/// <summary>
/// Information about a failed injection strategy attempt
/// </summary>
public class StrategyFailureInfo
{
    public InjectionStrategy Strategy { get; set; }
    public string ErrorMessage { get; set; } = "";
    public Exception? Exception { get; set; }
}