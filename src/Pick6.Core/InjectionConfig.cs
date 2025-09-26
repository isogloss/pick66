using System;

namespace Pick6.Core;

/// <summary>
/// Configuration for injection process with fallback strategies
/// </summary>
public class InjectionConfig
{
    /// <summary>
    /// Maximum time to search for processes (milliseconds)
    /// </summary>
    public int ProcessSearchTimeoutMs { get; set; } = 60000; // 60 seconds

    /// <summary>
    /// Polling interval for process search (milliseconds)
    /// </summary>
    public int PollIntervalMs { get; set; } = 250; // 250ms

    /// <summary>
    /// Time to wait for graphics modules to load (milliseconds)
    /// </summary>
    public int ModuleWaitTimeoutMs { get; set; } = 15000; // 15 seconds

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
}