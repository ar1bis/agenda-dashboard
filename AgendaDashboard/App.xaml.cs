using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using AgendaDashboard.Utilities;

namespace AgendaDashboard;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public readonly Configuration Configuration = DataStore.LoadConfiguration();
    public readonly Credentials Credentials = DataStore.LoadCredentials();
    public new MainWindow MainWindow = null!; // Suppress warning - variable set correctly before being used
    public new static App Current => (Application.Current as App)!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set up logging
#if DEBUG
        Trace.Listeners.Add(new TimestampConsoleTraceListener(true));
#else
        Trace.Listeners.Add(new TimestampTextWriterTraceListener($"log_{DateTime.Now:yyyyMMdd}.txt"));
#endif
        Trace.AutoFlush = true;

        // Create and show main window
        base.MainWindow = MainWindow = new MainWindow();
        MainWindow.Loaded += MainWindow_Loaded;
        MainWindow.Show();
    }

    private static void MainWindow_Loaded(object sender, RoutedEventArgs routedEventArgs)
    {
        // Set ctrl+win+# as a global keybind that temporarily raises mainWindow to the top of the z-order
        var raiseKeybind = new GlobalKeybind(
            Current.MainWindow,
            Key.OemQuestion,
            ModifierKeys.Control | ModifierKeys.Windows,
            10000);
        raiseKeybind.Pressed += Current.MainWindow.ToggleRaise;
    }
}