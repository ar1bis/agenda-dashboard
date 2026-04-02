using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows.Threading;
using AgendaDashboard.Utilities;

namespace AgendaDashboard.ViewModels;

public class TodoistViewModel : INotifyPropertyChanged
{
    public List<TodoistTask> TodoistTasks { get; private set; } = [];

    private NotifMgr _notifMgr;
    private HttpClient _client = new();
    private string _query = "";

    public TodoistViewModel()
    {
        Startup();
    }

    internal void Refresh()
    {
        _ = _notifMgr.ExecNotifyAsync(LoadTodoistTasksAsync, "Loaded Todoist tasks.");
    }

    private void Startup()
    {
        _notifMgr = App.Current.NotifMgr;

        var config = App.Current.Config.Todoist;
        var refreshInterval = config.RefreshInterval;
        _query = config.Query;

        // Set up client
        var apiToken = App.Current.Creds.Todoist.ApiToken;
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        // Set up a timer to refresh the tasks model
        var timer = new System.Timers.Timer(refreshInterval * 1000);
        timer.Elapsed += (_, _) => { Refresh(); };
        timer.Start();

        // Do initial refresh once the app is idle
        App.Current.Dispatcher.InvokeAsync(Refresh, DispatcherPriority.ApplicationIdle);
    }

    private async Task LoadTodoistTasksAsync()
    {
        var response = await _client.GetAsync($"https://api.todoist.com/api/v1/tasks/filter?query={_query}");
        response.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        // Get an array enumerator for the results element in the JSON response
        var tasksEnumerator = json.RootElement.GetProperty("results").EnumerateArray();

        // Create a new list to hold the TodoistTask objects
        var todoistTasksNew = tasksEnumerator.Select(task => new TodoistTask() // TODO: error handling
            {
                Id = task.GetProperty("id").GetString()!,
                Content = task.GetProperty("content").GetString()!,
                Checked = task.GetProperty("checked").GetBoolean(),
                DueDate = DateTime.Parse(task.GetProperty("due").GetProperty("date").GetString()!),
                DayOrder = task.GetProperty("day_order").GetInt16(),
                ChildOrder = task.GetProperty("child_order").GetInt16()
            })
            .ToList();

        // Sort the tasks by day order
        todoistTasksNew.Sort((x, y) =>
        {
            // If either task has a DayOrder of -1, it should be sorted to the end
            if (x.DayOrder == -1)
                return 1;
            if (y.DayOrder == -1)
                return -1;

            return x.DayOrder.CompareTo(y.DayOrder);
        });

        // Replace model and notify property change on Dispatcher - InvokeAsync not necessary, quick operations
        App.Current.Dispatcher.Invoke(() =>
        {
            TodoistTasks = todoistTasksNew;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TodoistTasks)));
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class TodoistTask
{
    public string Id { get; init; } = "";
    public string Content { get; init; } = "";
    public DateTime DueDate { get; init; }
    public bool Checked { get; init; }
    public short DayOrder { get; init; }
    public short ChildOrder { get; init; }
}