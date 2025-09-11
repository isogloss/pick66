using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Pick6.Loader.Update;

/// <summary>
/// Handles fetching manifest, comparing versions, downloading and verifying payload updates
/// </summary>
public class Updater
{
    private static readonly HttpClient httpClient = new();

    /// <summary>
    /// Attempts to check for and download updates from the specified manifest URL
    /// </summary>
    /// <param name="manifestUrl">URL to the JSON manifest file</param>
    /// <returns>True if update was successful or no update needed, false on error</returns>
    public static async Task<bool> CheckAndUpdateAsync(string manifestUrl)
    {
        try
        {
            Console.WriteLine("Checking for payload updates...");
            
            // Fetch manifest
            var payloadInfo = await FetchManifestAsync(manifestUrl);
            if (payloadInfo == null)
            {
                Console.WriteLine("Warning: Failed to fetch or parse update manifest");
                return false;
            }

            // Compare versions
            var currentVersion = VersionStore.GetCurrentVersion();
            if (currentVersion == payloadInfo.PayloadVersion)
            {
                Console.WriteLine($"Payload is up to date (version {currentVersion})");
                return true;
            }

            Console.WriteLine($"Update available: {currentVersion ?? "none"} -> {payloadInfo.PayloadVersion}");

            // Ensure cache directory exists
            if (!VersionStore.EnsurePayloadCacheDirectory())
            {
                return false;
            }

            // Download and extract payload
            var success = await DownloadAndExtractPayloadAsync(payloadInfo);
            if (success)
            {
                // Update version store
                VersionStore.SetCurrentVersion(payloadInfo.PayloadVersion);
                Console.WriteLine($"Successfully updated to payload version {payloadInfo.PayloadVersion}");
            }

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Update check failed: {ex.Message}");
            return false;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Simple JSON deserialization of known manifest structure")]
    private static async Task<PayloadInfo?> FetchManifestAsync(string manifestUrl)
    {
        try
        {
            using var response = await httpClient.GetAsync(manifestUrl);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            var manifestData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

            if (manifestData == null)
                return null;

            // Extract required fields
            var payloadVersion = manifestData.GetValueOrDefault("payloadVersion")?.ToString();
            var payloadUrl = manifestData.GetValueOrDefault("payloadUrl")?.ToString();
            var sha256 = manifestData.GetValueOrDefault("sha256")?.ToString();
            var entryAssembly = manifestData.GetValueOrDefault("entryAssembly")?.ToString();
            var entryType = manifestData.GetValueOrDefault("entryType")?.ToString();
            var entryMethod = manifestData.GetValueOrDefault("entryMethod")?.ToString();

            if (string.IsNullOrEmpty(payloadVersion) || string.IsNullOrEmpty(payloadUrl) ||
                string.IsNullOrEmpty(sha256) || string.IsNullOrEmpty(entryAssembly) ||
                string.IsNullOrEmpty(entryType) || string.IsNullOrEmpty(entryMethod))
            {
                Console.WriteLine("Warning: Manifest is missing required fields");
                return null;
            }

            return new PayloadInfo(payloadVersion, payloadUrl, sha256, entryAssembly, entryType, entryMethod);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to fetch manifest: {ex.Message}");
            return null;
        }
    }

    private static async Task<bool> DownloadAndExtractPayloadAsync(PayloadInfo payloadInfo)
    {
        try
        {
            Console.WriteLine($"Downloading payload from {payloadInfo.PayloadUrl}...");

            // Download payload
            using var response = await httpClient.GetAsync(payloadInfo.PayloadUrl);
            response.EnsureSuccessStatusCode();

            var payloadData = await response.Content.ReadAsByteArrayAsync();

            // Verify SHA256
            if (!VerifyPayloadIntegrity(payloadData, payloadInfo.Sha256))
            {
                Console.WriteLine("Warning: Payload integrity verification failed");
                return false;
            }

            Console.WriteLine("Payload integrity verified, extracting...");

            // Extract to cache directory
            var cachePath = VersionStore.GetPayloadCachePath();
            
            // Clear existing payload
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, recursive: true);
            }
            Directory.CreateDirectory(cachePath);

            // Extract ZIP
            using var zipStream = new MemoryStream(payloadData);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            
            archive.ExtractToDirectory(cachePath);

            Console.WriteLine($"Payload extracted to {cachePath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to download/extract payload: {ex.Message}");
            return false;
        }
    }

    private static bool VerifyPayloadIntegrity(byte[] data, string expectedSha256)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();
            
            return hashString.Equals(expectedSha256.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to verify payload integrity: {ex.Message}");
            return false;
        }
    }
}