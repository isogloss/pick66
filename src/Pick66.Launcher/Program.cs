using System;
#if WINDOWS
using System.Windows.Forms;
#endif

namespace Pick66.Launcher;

/// <summary>
/// Main entry point for Pick66 GUI Launcher
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main()
    {
#if WINDOWS
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        try
        {
            var mainForm = new MainLauncherForm();
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application error: {ex.Message}", "Pick66 Launcher Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
#else
        Console.WriteLine("Pick66 Launcher requires Windows with Windows Forms support.");
        Console.WriteLine("This application is designed for D3D11/DXGI proxy DLL installation.");
        Environment.Exit(1);
#endif
    }
}