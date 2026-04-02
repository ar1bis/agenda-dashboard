using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using YamlDotNet.RepresentationModel;

namespace AgendaDashboard;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _raised;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Set the initial window position from settings TODO: error handling
        var config = App.Current.ConfigMgr.Config;
        Left = double.Parse(((YamlScalarNode)config["general"]["x position"]).Value!) -
               4; // Offset by 4px because of the title bar
        Top = double.Parse(((YamlScalarNode)config["general"]["y position"]).Value!) - 4; // Same here

        var hwnd = new WindowInteropHelper(this).Handle;

        // Make the window a tool window: doesn't show up in taskbar or alt-tab switcher
        const int gwlpExstyle = -20; // Extended window styles
        const int wsExToolwindow = 0x00000080, wsExAppwindow = 0x00040000;
        var exStyle = GetWindowLongPtr(hwnd, gwlpExstyle);

        // Add TOOLWINDOW, remove APPWINDOW from the extended styles
        exStyle |= wsExToolwindow;
        exStyle &= ~wsExAppwindow;
        SetWindowLongPtr(hwnd, gwlpExstyle, exStyle);
    }

    internal void ToggleRaise(object? sender, EventArgs e)
    {
        if (_raised)
        {
            Drop();
            _raised = false;
        }
        else
        {
            Raise();
            _raised = true;
        }
    }

    internal void ShowNotification(string message, string? status)
    {
        StatusBarMessage.Text = message;
        StatusBarStatusItem.Content = status;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize status bar
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0";
        ShowNotification($"Agenda Dashboard v{version}", "Ready"); // Show version in status bar

        // Drop the window to the bottom of the z-order
        Drop();
        _raised = false;
    }

    private void Raise()
    {
        // Raise the window to the top of the z-order
        var hwnd = new WindowInteropHelper(this).Handle;
        const int hwndTopmost = -1;
        const int swpNosize = 0x0001, swpNomove = 0x0002, swpNoactivate = 0x0010;
        SetWindowPos(hwnd, hwndTopmost, 0, 0, 0, 0, swpNosize | swpNomove | swpNoactivate); // TODO: error handling
    }

    private new void Drop()
    {
        // Drop the window to the bottom of the z-order
        var hwnd = new WindowInteropHelper(this).Handle;
        const int hwndBottom = 1;
        const int swpNosize = 0x0001, swpNomove = 0x0002, swpNoactivate = 0x0010;
        SetWindowPos(hwnd, hwndBottom, 0, 0, 0, 0, swpNosize | swpNomove | swpNoactivate); // TODO: error handling
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLongPtr(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy,
        uint uFlags);
}