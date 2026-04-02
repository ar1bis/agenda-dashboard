using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AgendaDashboard.ViewModels;

namespace AgendaDashboard.Views;

public partial class CalendarView : UserControl
{
    public const double LeftMargin = 4;
    public const double RightMargin = 10;
    public const double DateLabelOffset = 40;
    public const double EventCardMinHeight = 50;
    public const double EventCardsRightOffset = 46; // Offset matches end of date line
    public static readonly Thickness DateLinesMargin = new(DateLabelOffset, 0, 27, 0);
    // Right margin matches end of date line

    public CalendarView()
    {
        InitializeComponent();
        DataContext = new CalendarViewModel();
        Loaded += CalendarView_Loaded; // Subscribe to the Loaded event to load events when the window is ready
    }

    internal void RefreshButton_Click(object sender, RoutedEventArgs? e)
    {
        (DataContext as CalendarViewModel)!.Refresh();
    }

    private void CalendarView_Loaded(object sender, RoutedEventArgs e)
    {
        // Scroll to the current time on load
        GcalScrollViewer.ScrollToVerticalOffset(
            (DateTime.Now.TimeOfDay.TotalMinutes / 1440) * GcalScrollViewer.ScrollableHeight); // 1440 minutes in a day

        // Scroll to the current time line every 60 seconds
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        timer.Tick += (_, _) =>
        {
            GcalScrollViewer.ScrollToVerticalOffset((DateTime.Now.TimeOfDay.TotalMinutes / 1440) *
                                                    GcalScrollViewer.ScrollableHeight); // 1440 minutes in a day
        };
        timer.Start();
    }

    private void PreviousDayButton_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = (DataContext as CalendarViewModel)!;
        viewModel.DecrementTargetDate();
        viewModel.Refresh();
    }

    private void CurrentDayButton_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = (DataContext as CalendarViewModel)!;
        viewModel.ResetTargetDate();
        viewModel.Refresh();
    }

    private void NextDayButton_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = (DataContext as CalendarViewModel)!;
        viewModel.IncrementTargetDate();
        viewModel.Refresh();
    }

    private void GcalScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        const double scrollAmount = 30; // Adjust this value for desired speed
        if (e.Delta < 0)
            GcalScrollViewer.ScrollToVerticalOffset(GcalScrollViewer.VerticalOffset + scrollAmount);
        else
            GcalScrollViewer.ScrollToVerticalOffset(GcalScrollViewer.VerticalOffset - scrollAmount);
        e.Handled = true;
    }
}