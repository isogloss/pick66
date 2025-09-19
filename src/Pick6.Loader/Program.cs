using Pick6.Core;
using Pick6.Projection;
using Pick6.Loader.Update;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Pick6.Loader;

/// <summary>
/// Unified entry point for Pick6 - Windows-only GUI application with minimal black & white interface
/// </summary>
public class Program
{
    // Feature flag to enable dynamic payload loading (disabled by default for stability)
    private const bool ENABLE_DYNAMIC_PAYLOAD = false;
    
    // TODO: Configure the actual manifest URL for your deployment
    private const string MANIFEST_URL = "https://example.com/pick6/manifest.json";

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [STAThread]
    public static async Task Main(string[] args)
    {
        // Allocate a console window for GUI applications when needed
        if (args.Any(arg => arg.ToLower() == "--help" || arg.ToLower() == "-h" || arg.ToLower() == "--check-updates-only"))
        {
            TryAllocConsole();
        }

        // Set up console logging for early output (help, errors, etc.)
        Log.AddSink(new ConsoleLogSink());

        try
        {
            await MainInternal(args);
        }
        catch (Exception ex)
        {
            Log.Error($"Fatal error: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");
            
            // For GUI applications, also show a message box for critical errors
            try
            {
                // Allocate console if we don't have one for error display
                TryAllocConsole();
                Console.WriteLine();
                Console.WriteLine("=== FATAL ERROR ===");
                Console.WriteLine($"A fatal error occurred: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                
                MessageBox.Show($"A fatal error occurred:\n\n{ex.Message}\n\nThe application will now exit.", 
                    "Pick6 Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // If both console and MessageBox fail, at least try to write to Debug output
                System.Diagnostics.Debug.WriteLine($"Fatal error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Safely allocate a console window for a Windows GUI application
    /// </summary>
    private static void TryAllocConsole()
    {
        try
        {
            // Only allocate if we don't already have a console
            if (GetConsoleWindow() == IntPtr.Zero)
            {
                AllocConsole();
            }
        }
        catch
        {
            // Ignore failures - console allocation is optional
        }
    }

    private static async Task MainInternal(string[] args)
    {
        // Handle help first
        if (args.Any(arg => arg.ToLower() == "--help" || arg.ToLower() == "-h"))
        {
            ShowHelp();
            
            // Wait for user input when showing help so they can read it
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Handle check-updates-only mode 
        if (args.Any(arg => arg.ToLower() == "--check-updates-only"))
        {
            Log.Info("Checking for updates...");
            await ExecuteUpdateSequence();
            Log.Info("Update check completed.");
            
            // Wait for user input so they can see the results
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
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
            Log.Debug($"Update sequence stack trace: {ex.StackTrace}");
        }
    }

    private static void RunGuiMode()
    {
        try
        {
            Log.Info("Initializing GUI components...");
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            // Use the existing MainForm instead of ModGui
            Log.Info("Starting Pick6 interface");
            var mainForm = new MainForm();
            Application.Run(mainForm);
            
            Log.Info("Application closed normally");
        }
        catch (Exception ex)
        {
            Log.Error($"GUI Error: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");
            
            try
            {
                MessageBox.Show($"GUI Error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Pick6 Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // If MessageBox fails, try console output with a pause
                TryAllocConsole();
                Console.WriteLine($"GUI Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            
            throw; // Re-throw to be caught by main exception handler
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