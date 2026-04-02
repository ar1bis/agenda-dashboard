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
    public NotifMgr? NotifMgr { get; private set; }
    public ConfigMgr ConfigMgr { get; private set; } = new();
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

        // Create the main window, config and notification managers
        var mainWindow = new MainWindow();
        NotifMgr = new NotifMgr(mainWindow.ShowNotification);
        mainWindow.Loaded += MainWindow_Loaded;
        mainWindow.Show();
    }

    private static void MainWindow_Loaded(object sender, RoutedEventArgs routedEventArgs)
    {
        var mainWindow = (sender as MainWindow)!;
        // Set ctrl+win+# as a global keybind that temporarily raises mainWindow to the top of the z-order
        var raiseKeybind = new GlobalKeybind(
            mainWindow,
            Key.OemQuestion,
            ModifierKeys.Control | ModifierKeys.Windows,
            10000);
        raiseKeybind.Pressed += mainWindow.ToggleRaise;
    }
}