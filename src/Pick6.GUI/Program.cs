#if WINDOWS
using Pick6.Core;
using Pick6.Projection;
#endif

namespace Pick6.GUI;

/// <summary>
/// GUI Program entry point for Pick6 - OBS-style interface
/// </summary>
public class Program
{
#if WINDOWS
    [STAThread]
    public static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        try
        {
            var mainForm = new MainForm();
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application error: {ex.Message}", "Pick6 Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
#else
    public static void Main(string[] args)
    {
        Console.WriteLine("Pick6 GUI is only available on Windows. Please use Pick6.Launcher for console mode.");
        Environment.Exit(1);
    }
#endif
}