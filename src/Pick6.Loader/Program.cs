using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Pick6.Core;
using Pick6.Projection;
using Pick6.Loader.Update;
using System.ComponentModel;

namespace Pick6.Loader;

/// <summary>
/// Pick6 - FiveM Game Capture & Projection Tool with Enhanced Multi-Strategy Injection
/// 
/// USAGE:
///   loader.exe                  - Start with GUI interface
///   loader.exe --help           - Show help information
///   loader.exe --check-updates-only - Check for updates only
/// 
/// FEATURES:
///   - Enhanced FiveM process detection with pattern matching
///   - Multi-strategy injection: Direct + Proxy DLL fallbacks (dxgi, d3d11, vulkan)  
///   - Automatic privilege elevation (SeDebugPrivilege)
///   - Rolling file logging to logs/injector.log
///   - Real-time game capture and borderless projection
/// 
/// REQUIREMENTS:
///   - Windows 10/11 x64
///   - .NET 8 Runtime  
///   - Administrator privileges recommended for injection
///   - FiveM or compatible game process
/// 
/// Build from source: Run install.bat from repository root
/// </summary>
public class Program
{
    // Current version of the loader - should match the GitHub release tag (without 'v' prefix)
    private const string LOADER_VERSION = "1.0.0";
    
    // Feature flag to enable loader self-updates from GitHub releases
    private const bool ENABLE_LOADER_AUTO_UPDATE = true;
    
    // Feature flag to enable dynamic payload loading (disabled by default for stability)
    private const bool ENABLE_DYNAMIC_PAYLOAD = false;
    
    // TODO: Configure the actual manifest URL for your deployment
    private const string MANIFEST_URL = "https://example.com/pick6/manifest.json";

    [STAThread]
    public static async Task Main(string[] args)
    {
        // Handle help first
        if (args.Any(arg => arg.ToLower() == "--help" || arg.ToLower() == "-h"))
        {
            ShowHelp();
            return;
        }

        // Handle check-updates-only mode 
        if (args.Any(arg => arg.ToLower() == "--check-updates-only"))
        {
            Log.Info("Checking for updates...");
            
            // Check for loader updates first
            if (ENABLE_LOADER_AUTO_UPDATE)
            {
                await CheckLoaderUpdates();
            }
            
            // Then check for payload updates
            await ExecuteUpdateSequence();
            Log.Info("Update check completed.");
            return;
        }

        // Check for loader self-updates on startup (non-blocking with timeout)
        if (ENABLE_LOADER_AUTO_UPDATE && !args.Any(arg => arg.ToLower() == "--skip-loader-update"))
        {
            await CheckLoaderUpdates();
        }

        // Execute update sequence before starting GUI (non-blocking with timeout)
        if (ENABLE_DYNAMIC_PAYLOAD || args.Any(arg => arg.ToLower() == "--check-updates"))
        {
            await ExecuteUpdateSequence();
        }

        // If dynamic payload is enabled and available, delegate to payload
        if (ENABLE_DYNAMIC_PAYLOAD && PayloadLauncher.IsPayloadAvailable())
        {
            var payloadInfo = PayloadLauncher.GetCachedPayloadInfo();
            if (payloadInfo != null)
            {
                Log.Info("Delegating to dynamic payload...");
                var success = PayloadLauncher.TryLaunchPayload(payloadInfo, args);
                if (success)
                {
                    return; // Payload handled execution
                }
                Log.Info("Payload launch failed, falling back to built-in functionality");
            }
        }

        // Always run GUI mode
        RunGuiMode();
    }

    private static async Task CheckLoaderUpdates()
    {
        try
        {
            Log.Info($"Current loader version: {LOADER_VERSION}");
            
            // Use timeout to prevent hanging on network issues
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            var updateTask = UpdateService.CheckAndUpdateLoaderAsync(LOADER_VERSION);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
            var completedTask = await Task.WhenAny(updateTask, timeoutTask);
            
            if (completedTask == updateTask)
            {
                var updateSuccess = await updateTask;
                cts.Cancel(); // Cancel timeout
                
                if (!updateSuccess)
                {
                    Log.Info("Loader update check completed (no update needed or update failed)");
                }
                // Note: If update is successful, UpdateService will exit the application
            }
            else
            {
                Log.Info("Loader update check timed out (30s), continuing with current version");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Loader update check failed: {ex.Message}");
        }
    }

    private static async Task ExecuteUpdateSequence()
    {
        try
        {
            Log.Info("Checking for updates...");
            
            // Use timeout to prevent hanging on network issues
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            // Try to update from manifest with timeout
            var updateTask = Updater.CheckAndUpdateAsync(MANIFEST_URL);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
            var completedTask = await Task.WhenAny(updateTask, timeoutTask);
            
            bool updateSuccess = false;
            if (completedTask == updateTask)
            {
                updateSuccess = await updateTask;
                cts.Cancel(); // Cancel timeout
            }
            else
            {
                Log.Info("Update check timed out (30s), continuing with existing functionality");
            }
            
            if (!updateSuccess)
            {
                Log.Info("Update check failed, checking for existing cached payload...");
                
                if (!PayloadLauncher.IsPayloadAvailable())
                {
                    Log.Info("No cached payload available, attempting to extract embedded payload...");
                    
                    // Try to extract embedded payload as fallback
                    var extractSuccess = InitialPayloadExtractor.TryExtractEmbeddedPayload();
                    if (!extractSuccess)
                    {
                        Log.Info("No embedded payload available, continuing with built-in functionality");
                    }
                }
                else
                {
                    Log.Info("Using existing cached payload");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Update failed: {ex.Message}");
        }
    }

    private static void RunGuiMode()
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            // Use the existing MainForm instead of ModGui
            Log.Info("Starting Pick6 interface");
            var mainForm = new MainForm();
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            Log.Error($"Error: {ex.Message}");
            MessageBox.Show($"error: {ex.Message}", "pick6 error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void ShowHelp()
    {
        Log.Info("Pick6 - High-Performance OBS Game Capture Clone for FiveM (Windows Only)");
        Log.Info("");
        Log.Info("Usage: pick6.exe [options]");
        Log.Info("");
        Log.Info("Options:");
        Log.Info("  --check-updates           Check for updates at startup");
        Log.Info("  --check-updates-only      Check for updates and exit");
        Log.Info("  --skip-loader-update      Skip loader self-update check");
        Log.Info("  --help, -h                Show this help message");
        Log.Info("");
        Log.Info("Default Behavior:");
        Log.Info("  Opens GUI mode with minimal black & white interface");
        Log.Info("  Automatically checks for loader updates from GitHub releases");
        Log.Info("");
        Log.Info("Examples:");
        Log.Info("  pick6.exe                             # GUI mode with auto-update");
        Log.Info("  pick6.exe --check-updates             # GUI mode with update check");
        Log.Info("  pick6.exe --check-updates-only        # Check for updates and exit");
        Log.Info("  pick6.exe --skip-loader-update        # GUI mode without loader update check");
    }
}