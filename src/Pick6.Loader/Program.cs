using Pick6.Core;
using Pick6.Projection;
using Pick6.Loader.Update;

#if WINDOWS
using System.ComponentModel;
#endif

namespace Pick6.Loader;

/// <summary>
/// Unified entry point for Pick6 - GUI-only application with minimal black & white interface
/// </summary>
public class Program
{
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
            await ExecuteUpdateSequence();
            Log.Info("Update check completed.");
            return;
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
            Log.Warn($"Update sequence failed: {ex.Message}");
            Log.Info("Continuing with built-in functionality");
        }
    }

#if WINDOWS
    private static void RunGuiMode()
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            var mainForm = new MainForm();
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            Log.Error($"Application error: {ex.Message}");
            MessageBox.Show($"Application error: {ex.Message}", "Pick6 Loader Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
#else
    private static void RunGuiMode()
    {
        // This should never be called on non-Windows, but just in case
        Log.Error("GUI mode is only available on Windows.");
        Environment.Exit(1);
    }
#endif

    private static void ShowHelp()
    {
        Log.Info("Pick6 - High-Performance OBS Game Capture Clone for FiveM");
        Log.Info("");
        Log.Info("Usage: pick6.exe [options]");
        Log.Info("");
        Log.Info("Options:");
        Log.Info("  --check-updates           Check for updates at startup");
        Log.Info("  --check-updates-only      Check for updates and exit");
        Log.Info("  --help, -h                Show this help message");
        Log.Info("");
        Log.Info("Default Behavior:");
        Log.Info("  Opens GUI mode with minimal black & white interface");
        Log.Info("");
        Log.Info("Examples:");
        Log.Info("  pick6.exe                             # GUI mode");
        Log.Info("  pick6.exe --check-updates             # GUI mode with update check");
        Log.Info("  pick6.exe --check-updates-only        # Check for updates and exit");
    }
}