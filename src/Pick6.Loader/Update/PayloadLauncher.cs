using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Pick6.Loader.Update;

/// <summary>
/// Handles dynamic assembly loading and reflection invocation of payload entry points
/// </summary>
public class PayloadLauncher
{
    /// <summary>
    /// Attempts to load and invoke the payload with the specified parameters
    /// </summary>
    /// <param name="payloadInfo">Payload information including entry point details</param>
    /// <param name="args">Arguments to pass to the payload entry method</param>
    /// <returns>True if payload was successfully invoked, false on error</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Payload assemblies are not trimmed")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.All' in call to 'target method'. The return value of the source method does not have matching annotations.", Justification = "Payload assemblies are not trimmed")]
    public static bool TryLaunchPayload(PayloadInfo payloadInfo, string[] args)
    {
        try
        {
            Console.WriteLine($"Loading payload assembly: {payloadInfo.EntryAssembly}");

            var cachePath = VersionStore.GetPayloadCachePath();
            var assemblyPath = Path.Combine(cachePath, payloadInfo.EntryAssembly);

            if (!File.Exists(assemblyPath))
            {
                Console.WriteLine($"Warning: Payload assembly not found at {assemblyPath}");
                return false;
            }

            // Load the assembly
            var assembly = Assembly.LoadFrom(assemblyPath);
            if (assembly == null)
            {
                Console.WriteLine("Warning: Failed to load payload assembly");
                return false;
            }

            // Get the type containing the entry method
            var type = assembly.GetType(payloadInfo.EntryType);
            if (type == null)
            {
                Console.WriteLine($"Warning: Type '{payloadInfo.EntryType}' not found in payload assembly");
                return false;
            }

            // Get the entry method
            var method = type.GetMethod(payloadInfo.EntryMethod, BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                Console.WriteLine($"Warning: Static method '{payloadInfo.EntryMethod}' not found in type '{payloadInfo.EntryType}'");
                return false;
            }

            // Invoke the entry method
            Console.WriteLine($"Invoking payload entry point: {payloadInfo.EntryType}.{payloadInfo.EntryMethod}");
            
            var parameters = method.GetParameters();
            object? result;
            
            if (parameters.Length == 0)
            {
                // Method takes no parameters
                result = method.Invoke(null, null);
            }
            else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string[]))
            {
                // Method takes string[] args parameter
                result = method.Invoke(null, new object[] { args });
            }
            else
            {
                Console.WriteLine($"Warning: Unsupported method signature for '{payloadInfo.EntryMethod}'. Expected no parameters or string[] args.");
                return false;
            }

            Console.WriteLine("Payload entry point invoked successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to launch payload: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a payload is available in the cache
    /// </summary>
    /// <returns>True if payload files are available, false otherwise</returns>
    public static bool IsPayloadAvailable()
    {
        try
        {
            var cachePath = VersionStore.GetPayloadCachePath();
            return Directory.Exists(cachePath) && Directory.GetFiles(cachePath, "*.dll").Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets payload information for the currently cached payload, if available
    /// </summary>
    /// <returns>PayloadInfo if available and valid, null otherwise</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Simple JSON deserialization of known structure")]
    public static PayloadInfo? GetCachedPayloadInfo()
    {
        try
        {
            var cachePath = VersionStore.GetPayloadCachePath();
            var manifestPath = Path.Combine(cachePath, "payload-manifest.json");
            
            if (!File.Exists(manifestPath))
            {
                return null;
            }

            var jsonContent = File.ReadAllText(manifestPath);
            var manifestData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

            if (manifestData == null)
                return null;

            var payloadVersion = manifestData.GetValueOrDefault("payloadVersion")?.ToString();
            var payloadUrl = manifestData.GetValueOrDefault("payloadUrl")?.ToString() ?? "";
            var sha256 = manifestData.GetValueOrDefault("sha256")?.ToString() ?? "";
            var entryAssembly = manifestData.GetValueOrDefault("entryAssembly")?.ToString();
            var entryType = manifestData.GetValueOrDefault("entryType")?.ToString();
            var entryMethod = manifestData.GetValueOrDefault("entryMethod")?.ToString();

            if (string.IsNullOrEmpty(payloadVersion) || string.IsNullOrEmpty(entryAssembly) ||
                string.IsNullOrEmpty(entryType) || string.IsNullOrEmpty(entryMethod))
            {
                return null;
            }

            return new PayloadInfo(payloadVersion, payloadUrl, sha256, entryAssembly, entryType, entryMethod);
        }
        catch
        {
            return null;
        }
    }
}