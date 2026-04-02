using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace AgendaDashboard.Utilities;

public class NotifMgr
{
    private readonly Action<string, string> _showMsgAction;
    private readonly DispatcherTimer _msgTimer;
    // (message, status)
    private readonly Queue<(string message, string status)> _msgQueue;
    private bool _statusBarEmpty;

    public NotifMgr(Action<string, string> showMsgAction)
    {
        _showMsgAction = showMsgAction ?? throw new ArgumentNullException(nameof(showMsgAction),
            "Notification action cannot be null.");

        // Start a timer that shows messages every two seconds
        _msgTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _msgTimer.Tick += (_, _) => ShowNextMessage();
        _msgTimer.Start();

        _msgQueue = [];
        _statusBarEmpty = true;
    }

    internal async Task ExecNotifyAsync(Func<Task> asyncFunc, string successMessage)
    {
        try
        {
            await asyncFunc();
        }
        catch (Exception ex)
        {
            // Show an error message if loading fails
            QueueMessage($"{asyncFunc.Method.Name}(): {ex.Message}", "Error");
            Trace.WriteLine($"{asyncFunc.Method.Name}(): {ex.Message}");
            return;
        }

        // Successful, show success message
        QueueMessage(successMessage, "Success");
    }

    private void QueueMessage(string message, string status)
    {
        _msgQueue.Enqueue((message, status));

        if (!_statusBarEmpty) return; // Nothing else to do if the status bar is not empty

        // Immediately show message if the status bar is empty - InvokeAsync not necessary, quick operations
        Application.Current.Dispatcher.Invoke(ShowNextMessage, DispatcherPriority.Normal);
        _statusBarEmpty = false;
        // Reset the timer
        _msgTimer.Stop();
        _msgTimer.Start();
    }

    private void ShowNextMessage()
    {
        if (_msgQueue.Count == 0)
        {
            // Queue a "ready" status message
            _msgQueue.Enqueue(("", "Ready"));
            _statusBarEmpty = true;
        }

        var (message, status) = _msgQueue.Dequeue();
        _showMsgAction(message, status); // Show the message
    }
}