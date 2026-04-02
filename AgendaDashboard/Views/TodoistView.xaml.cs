using System.Windows;
using System.Windows.Controls;
using AgendaDashboard.ViewModels;

namespace AgendaDashboard.Views;

public partial class TodoistView : UserControl
{
    public TodoistView()
    {
        InitializeComponent();
        DataContext = new TodoistViewModel();
    }

    internal void RefreshButton_Click(object sender, RoutedEventArgs? e)
    {
        (DataContext as TodoistViewModel)!.Refresh();
    }
}
