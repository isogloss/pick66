using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Pick6.Core;

namespace Pick6.Loader.Update;

/// <summary>
/// Handles checking for and downloading loader.exe updates from GitHub releases
/// </summary>
public class UpdateService
{
    private static readonly HttpClient httpClient = new();
    private const string GITHUB_REPO_OWNER = "isogloss";
    private const string GITHUB_REPO_NAME = "pick66";
    
    /// <summary>
    /// Checks for loader.exe updates from GitHub releases and downloads if available
    /// </summary>
    /// <param name="currentVersion">Current version of the loader</param>
    /// <returns>True if no update needed or update successful, false on error</returns>
    public static async Task<bool> CheckAndUpdateLoaderAsync(string currentVersion)
    {
        try
        {
            Log.Info("Checking for loader updates from GitHub releases...");
            
            // Fetch latest release info
            var latestRelease = await FetchLatestReleaseAsync();
            if (latestRelease == null)
            {
                Log.Warn("Failed to fetch latest release information");
                return false;
            }
            
            var latestVersion = latestRelease.TagName?.TrimStart('v');
            if (string.IsNullOrEmpty(latestVersion))
            {
                Log.Warn("Latest release has no version tag");
                return false;
            }
            
            // Compare versions
            if (currentVersion == latestVersion)
            {
                Log.Info($"Loader is up to date (version {currentVersion})");
                return true;
            }
            
            Log.Info($"Loader update available: {currentVersion} -> {latestVersion}");
            
            // Find the .zip asset in the release
            var zipAsset = FindZipAsset(latestRelease);
            if (zipAsset == null)
            {
                Log.Warn("No .zip asset found in latest release");
                return false;
            }
            
            // Download and prepare update
            var success = await DownloadAndPrepareUpdateAsync(zipAsset.BrowserDownloadUrl, latestVersion);
            return success;
        }
        catch (Exception ex)
        {
            Log.Warn($"Loader update check failed: {ex.Message}");
            return false;
        }
    }
    
    private static async Task<GitHubRelease?> FetchLatestReleaseAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{GITHUB_REPO_OWNER}/{GITHUB_REPO_NAME}/releases/latest";
            
            // GitHub API requires a User-Agent header
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Pick6-Loader");
            
            using var response = await httpClient.GetAsync(url);
            
            // If not found (404), there are no releases yet
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Log.Info("No releases found in repository");
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(jsonContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            return release;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to fetch latest release: {ex.Message}");
            return null;
        }
    }
    
    private static GitHubAsset? FindZipAsset(GitHubRelease release)
    {
        if (release.Assets == null || release.Assets.Length == 0)
            return null;
        
        foreach (var asset in release.Assets)
        {
            if (asset.Name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)
            {
                return asset;
            }
        }
        
        return null;
    }
    
    private static async Task<bool> DownloadAndPrepareUpdateAsync(string downloadUrl, string newVersion)
    {
        try
        {
            Log.Info($"Downloading update from {downloadUrl}...");
            
            // Download the zip file
            using var response = await httpClient.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();
            
            var zipData = await response.Content.ReadAsByteArrayAsync();
            
            Log.Info($"Downloaded {zipData.Length} bytes, extracting...");
            
            // Create temporary directory for the update
            var tempUpdateDir = Path.Combine(Path.GetTempPath(), "Pick6Update_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempUpdateDir);
            
            // Extract the zip
            using var zipStream = new MemoryStream(zipData);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(tempUpdateDir);
            
            // Find loader.exe in the extracted files
            var newLoaderPath = FindLoaderExe(tempUpdateDir);
            if (newLoaderPath == null)
            {
                Log.Warn("loader.exe not found in downloaded update");
                CleanupDirectory(tempUpdateDir);
                return false;
            }
            
            Log.Info($"Found new loader.exe at: {newLoaderPath}");
            
            // Prepare the updater script
            var currentLoaderPath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(currentLoaderPath))
            {
                Log.Warn("Could not determine current loader.exe path");
                CleanupDirectory(tempUpdateDir);
                return false;
            }
            
            var updaterScriptPath = Path.Combine(Path.GetTempPath(), "updater.bat");
            CreateUpdaterScript(updaterScriptPath, currentLoaderPath, newLoaderPath, tempUpdateDir);
            
            Log.Info("Update prepared. Launching updater and exiting...");
            
            // Launch the updater script and exit
            var processInfo = new ProcessStartInfo
            {
                FileName = updaterScriptPath,
                UseShellExecute = true,
                CreateNoWindow = false,
                WorkingDirectory = Path.GetDirectoryName(currentLoaderPath)
            };
            
            Process.Start(processInfo);
            
            // Signal that we should exit to allow the update to proceed
            Environment.Exit(0);
            
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to download and prepare update: {ex.Message}");
            return false;
        }
    }
    
    private static string? FindLoaderExe(string directory)
    {
        // Search for loader.exe recursively
        var files = Directory.GetFiles(directory, "loader.exe", SearchOption.AllDirectories);
        return files.Length > 0 ? files[0] : null;
    }
    
    private static void CreateUpdaterScript(string scriptPath, string oldLoaderPath, string newLoaderPath, string tempDir)
    {
        // Create a batch script that will:
        // 1. Wait for the current process to exit
        // 2. Replace the old loader.exe with the new one
        // 3. Restart the loader
        // 4. Clean up temporary files
        
        var script = $@"@echo off
echo Pick6 Updater - Installing new version...
echo.

REM Wait for the current process to exit
timeout /t 2 /nobreak >nul

REM Backup the old loader
if exist ""{oldLoaderPath}.bak"" del ""{oldLoaderPath}.bak""
move ""{oldLoaderPath}"" ""{oldLoaderPath}.bak"" >nul 2>&1

REM Copy the new loader
echo Copying new loader.exe...
copy ""{newLoaderPath}"" ""{oldLoaderPath}"" >nul
if errorlevel 1 (
    echo ERROR: Failed to copy new loader.exe
    echo Restoring backup...
    move ""{oldLoaderPath}.bak"" ""{oldLoaderPath}"" >nul 2>&1
    pause
    exit /b 1
)

echo Update successful!
echo.

REM Clean up temporary directory
echo Cleaning up...
rd /s /q ""{tempDir}"" >nul 2>&1

REM Remove backup if successful
del ""{oldLoaderPath}.bak"" >nul 2>&1

REM Restart the loader
echo Restarting Pick6 Loader...
start """" ""{oldLoaderPath}""

REM Delete this script
del ""%~f0""
";
        
        File.WriteAllText(scriptPath, script);
    }
    
    private static void CleanupDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
    
    // GitHub API models
    private class GitHubRelease
    {
        public string? TagName { get; set; }
        public string? Name { get; set; }
        public GitHubAsset[]? Assets { get; set; }
    }
    
    private class GitHubAsset
    {
        public string? Name { get; set; }
        public string? BrowserDownloadUrl { get; set; }
    }
}
