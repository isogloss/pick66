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
            ConsoleMenu.ShowHelp();
            return;
        }

        // Execute update sequence before determining run mode
        if (ENABLE_DYNAMIC_PAYLOAD)
        {
            await ExecuteUpdateSequence();
        }

        // Determine run mode based on arguments and platform
        var runMode = DetermineRunMode(args);

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
            
            // Try to update from manifest
            var updateSuccess = await Updater.CheckAndUpdateAsync(MANIFEST_URL);
            
            if (!updateSuccess)
            {
                Console.WriteLine("Update failed, checking for existing cached payload...");
                
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

    private enum RunMode
    {
        Gui,
        Console
    }
}