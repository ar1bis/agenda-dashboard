using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shell;

namespace AgendaDashboard.Controls;

public partial class TitleBar : UserControl
{
    public TitleBar()
    {
        InitializeComponent();
        Loaded += TitleBar_Loaded; // Subscribe to the Loaded event to initialize the window chrome
    }

    private void TitleBar_Loaded(object sender, RoutedEventArgs e)
    {
        // Lock the window by default; do this by checking the "Lock Window" menu item
        LockMenuItem.IsChecked = true;
    }

    private void MenuButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton button) return;
        var menu = button.ContextMenu!;
        menu.PlacementTarget = button;
        menu.Placement = PlacementMode.Bottom; // Needs to be set here - doesn't work in XAML
        menu.IsOpen = true;
    }
    
    private void MenuButton_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton button) return;
        button.ContextMenu!.IsOpen = false;
    }
    
    private void Menu_Closed(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton button) return;
        button.IsChecked = false;
    }

    private void RefreshMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = (Window.GetWindow(this) as MainWindow)!;
        mainWindow.CalendarView.RefreshButton_Click(this, null);
        mainWindow.TodoistView.RefreshButton_Click(this, null);
    }

    private void LockMenuItem_Checked(object sender, RoutedEventArgs e)
    {
        // Disable dragging and resizing
        WindowChrome.SetWindowChrome(Window.GetWindow(this), new WindowChrome
        {
            CaptionHeight = 0, // Prevent dragging by setting the caption height to 0
            ResizeBorderThickness = new Thickness(0) // Prevent resizing by setting the resize border thickness to 0
        });
    }

    private void LockMenuItem_Unchecked(object sender, RoutedEventArgs e)
    {
        // Enable dragging and resizing
        WindowChrome.SetWindowChrome(Window.GetWindow(this), new WindowChrome
        {
            CaptionHeight = ActualHeight, // Restore the caption height to allow dragging
            ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness // Restore the resize border thickness
        });
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        App.Current.Shutdown();
    }
}