using System.Windows;
using System.Windows.Controls;
using AgendaDashboard.ViewModels;

namespace AgendaDashboard.Views;

public partial class TodoistView : UserControl
{
    private readonly TodoistViewModel _viewModel;

    public TodoistView()
    {
        InitializeComponent();
        DataContext = _viewModel = new TodoistViewModel();
    }

    internal void RefreshButton_Click(object sender, RoutedEventArgs? e)
    {
        _viewModel.Refresh();
    }
}
