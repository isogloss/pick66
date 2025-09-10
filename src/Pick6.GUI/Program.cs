using Pick6.Core;
using Pick6.Projection;

namespace Pick6.GUI;

/// <summary>
/// GUI Program entry point for Pick6 - OBS-style interface
/// </summary>
public class Program
{
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
}