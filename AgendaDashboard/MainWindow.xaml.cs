using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace AgendaDashboard;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _raised;
    private readonly DispatcherTimer _notifTimer;
    // (message, status)
    private readonly Queue<(string message, string status)> _notifQueue;
    private bool _statusBarEmpty;

    public MainWindow()
    {
        // Set up notification queue
        _notifQueue = [];
        _statusBarEmpty = true;
        _notifTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _notifTimer.Tick += (_, _) => ShowNextNotification();
        _notifTimer.Start();

        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Set the initial window position from settings TODO: error handling
        var config = App.Current.Configuration;
        Left = config.General.XPosition - 4; // Offset by 4px because of the title bar
        Top = config.General.YPosition - 4; // Same here

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

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize status bar
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0";
        QueueNotification($"Agenda Dashboard v{version}", "Ready");

        // Drop window to bottom of z-order
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

    private void ShowNextNotification()
    {
        if (_notifQueue.Count == 0)
        {
            // Queue a "ready" status message
            _notifQueue.Enqueue(("", "Ready"));
            _statusBarEmpty = true;
        }

        var (message, status) = _notifQueue.Dequeue();
        StatusBarMessage.Text = message;
        StatusBarStatusItem.Content = status;
    }

    internal void QueueNotification(string message, string status)
    {
        _notifQueue.Enqueue((message, status));

        if (!_statusBarEmpty) return; // Nothing else to do if the status bar is not empty

        // Immediately show message if the status bar is empty - queue on Dispatcher
        Application.Current.Dispatcher.InvokeAsync(ShowNextNotification, DispatcherPriority.Normal);
        _statusBarEmpty = false;
        // Reset the timer
        _notifTimer.Stop();
        _notifTimer.Start();
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLongPtr(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy,
        uint uFlags);
}