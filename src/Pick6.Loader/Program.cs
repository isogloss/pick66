using Pick6.Core;
using Pick6.Projection;
using Pick6.Loader.Update;

#if WINDOWS
using System.ComponentModel;
#endif

namespace Pick6.Loader;

/// <summary>
/// Unified entry point for Pick6 - replaces Pick6.GUI.exe, Pick6.Launcher.exe, and Pick6.UI.exe
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
            Console.WriteLine("Checking for updates...");
            await ExecuteUpdateSequence();
            Console.WriteLine("Update check completed.");
            return;
        }

        // Execute update sequence before determining run mode (non-blocking with timeout)
        if (ENABLE_DYNAMIC_PAYLOAD || args.Any(arg => arg.ToLower() == "--check-updates"))
        {
            await ExecuteUpdateSequence();
        }

        // Determine run mode - prefer auto-start console mode if CLI args present
        var runMode = DetermineRunMode(args);
        
        // Auto-start mode: if command line args present (except help and gui-only flags), use console mode
        bool hasNonGuiArgs = args.Any(arg => !arg.ToLower().StartsWith("--help") && 
                                           !arg.ToLower().StartsWith("-h") &&
                                           arg.ToLower() != "--gui");
        
        if (hasNonGuiArgs && runMode == RunMode.Gui)
        {
            runMode = RunMode.Console;
            Console.WriteLine("[INFO] Switching to console mode due to command-line arguments");
        }

        // If dynamic payload is enabled and available, delegate to payload
        if (ENABLE_DYNAMIC_PAYLOAD && PayloadLauncher.IsPayloadAvailable())
        {
            var payloadInfo = PayloadLauncher.GetCachedPayloadInfo();
            if (payloadInfo != null)
            {
                Console.WriteLine("Delegating to dynamic payload...");
                var success = PayloadLauncher.TryLaunchPayload(payloadInfo, args);
                if (success)
                {
                    return; // Payload handled execution
                }
                Console.WriteLine("Payload launch failed, falling back to built-in functionality");
            }
        }

        // Fallback to built-in functionality
        switch (runMode)
        {
            case RunMode.Gui:
                RunGuiMode();
                break;
            case RunMode.Console:
                RunConsoleMode(args);
                break;
        }
    }

    private static async Task ExecuteUpdateSequence()
    {
        try
        {
            Console.WriteLine("Checking for updates...");
            
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
                Console.WriteLine("Update check timed out (30s), continuing with existing functionality");
            }
            
            if (!updateSuccess)
            {
                Console.WriteLine("Update check failed, checking for existing cached payload...");
                
                if (!PayloadLauncher.IsPayloadAvailable())
                {
                    Console.WriteLine("No cached payload available, attempting to extract embedded payload...");
                    
                    // Try to extract embedded payload as fallback
                    var extractSuccess = InitialPayloadExtractor.TryExtractEmbeddedPayload();
                    if (!extractSuccess)
                    {
                        Console.WriteLine("No embedded payload available, continuing with built-in functionality");
                    }
                }
                else
                {
                    Console.WriteLine("Using existing cached payload");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Update sequence failed: {ex.Message}");
            Console.WriteLine("Continuing with built-in functionality");
        }
    }

    private static RunMode DetermineRunMode(string[] args)
    {
        // Check for explicit flags
        bool forceGui = args.Any(arg => arg.ToLower() == "--gui");
        bool forceConsole = args.Any(arg => arg.ToLower() == "--console");

        // If both are specified, prefer GUI
        if (forceGui && forceConsole)
        {
            forceConsole = false;
        }

        // Force console mode
        if (forceConsole)
        {
            return RunMode.Console;
        }

        // Force GUI mode (only valid on Windows)
        if (forceGui)
        {
#if WINDOWS
            return RunMode.Gui;
#else
            Console.WriteLine("Warning: GUI mode is only available on Windows. Starting console mode.");
            return RunMode.Console;
#endif
        }

        // Default behavior: Always prefer GUI on Windows (unless --console is specified)
        // This makes the loader a proper GUI application by default
#if WINDOWS
        return RunMode.Gui;
#else
        return RunMode.Console;
#endif
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
            MessageBox.Show($"Application error: {ex.Message}", "Pick6 Loader Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
#else
    private static void RunGuiMode()
    {
        // This should never be called on non-Windows, but just in case
        Console.WriteLine("GUI mode is only available on Windows. Starting console mode.");
        RunConsoleMode(Array.Empty<string>());
    }
#endif

    private static void RunConsoleMode(string[] args)
    {
        var consoleMenu = new ConsoleMenu();
        consoleMenu.Run(args);
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Pick6 - High-Performance OBS Game Capture Clone for FiveM");
        Console.WriteLine();
        Console.WriteLine("Usage: pick6.exe [options]");
        Console.WriteLine();
        Console.WriteLine("Basic Options:");
        Console.WriteLine("  --fps <number>            Set target FPS (1-600, default: 60)");
        Console.WriteLine("  --resolution <w> <h>      Set output resolution (e.g., 1920 1080)");
        Console.WriteLine("  --monitor <index>         Target monitor index (0-based)");
        Console.WriteLine("  --no-projection           Capture only, disable projection window");
        Console.WriteLine();
        Console.WriteLine("Logging & Debug:");
        Console.WriteLine("  --log-level <level>       Set log level: Debug, Info, Warning, Error");
        Console.WriteLine("                            Debug mode shows FPS statistics");
        Console.WriteLine();
        Console.WriteLine("Update System:");
        Console.WriteLine("  --check-updates           Check for updates at startup");
        Console.WriteLine("  --check-updates-only      Check for updates and exit");
        Console.WriteLine();
        Console.WriteLine("Mode Control:");
        Console.WriteLine("  --gui                     Force GUI mode");
        Console.WriteLine("  --interactive             Use interactive menu (legacy mode)");
        Console.WriteLine("  --help, -h                Show this help message");
        Console.WriteLine();
        Console.WriteLine("Default Behavior:");
        Console.WriteLine("  With no arguments: Opens GUI mode on Windows");
        Console.WriteLine("  With arguments: Auto-starts capture/projection in console mode");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  pick6.exe                             # GUI mode");
        Console.WriteLine("  pick6.exe --fps 144 --log-level Debug # High FPS with debug logging");
        Console.WriteLine("  pick6.exe --fps 60 --no-projection   # Capture only, no display");
        Console.WriteLine("  pick6.exe --interactive              # Traditional menu mode");
        Console.WriteLine("  pick6.exe --check-updates-only       # Check for updates and exit");
    }

    private enum RunMode
    {
        Gui,
        Console
    }
}