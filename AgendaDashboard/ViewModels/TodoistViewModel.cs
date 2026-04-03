using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows.Threading;
using AgendaDashboard.Utilities;

namespace AgendaDashboard.ViewModels;

public class TodoistViewModel : INotifyPropertyChanged
{
    public List<TodoistTask> TodoistTasks { get; private set; } = [];

    private HttpClient _client = new();
    private string _query = "";

    public TodoistViewModel()
    {
        _ = StartupAsync();
    }

    private async Task StartupAsync()
    {
        var config = App.Current.Configuration.Todoist;
        _query = config.Query;

        // Set up client
        var apiToken = App.Current.Credentials.Todoist.ApiToken;
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

        // Set up timer to periodically refresh tasks model
        var timer = new System.Timers.Timer(config.RefreshInterval * 1000);
        timer.Elapsed += (_, _) => { Refresh(); };
        timer.Start();

        Refresh();
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

        // Replace model and notify property change on Dispatcher
        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            TodoistTasks = todoistTasksNew;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TodoistTasks)));
        }, DispatcherPriority.Normal);
    }

    internal void Refresh()
    {
        _ = HelperMethods.ExecAndNotifyAsync(LoadTodoistTasksAsync, "Loaded Todoist tasks.");
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