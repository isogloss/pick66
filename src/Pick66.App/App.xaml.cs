using System.Windows;
using Pick66.App.ViewModels;

namespace Pick66.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Create and show main window
        var mainWindow = new MainWindow
        {
            DataContext = new MainViewModel()
        };

        mainWindow.Show();
    }
}