using Pick6.Core;
using Pick6.Projection;

#if WINDOWS
using System.ComponentModel;
#endif

namespace Pick6.Loader;

/// <summary>
/// Unified entry point for Pick6 - replaces Pick6.GUI.exe, Pick6.Launcher.exe, and Pick6.UI.exe
/// </summary>
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Handle help first
        if (args.Any(arg => arg.ToLower() == "--help" || arg.ToLower() == "-h"))
        {
            ConsoleMenu.ShowHelp();
            return;
        }

        // Determine run mode based on arguments and platform
        var runMode = DetermineRunMode(args);

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

        // Default behavior: GUI on Windows, console elsewhere
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