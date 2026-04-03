using System.Diagnostics;

namespace AgendaDashboard.Utilities;

public static class HelperMethods
{
    internal static async Task ExecAndNotifyAsync(Func<Task> asyncFunc, string successMessage)
    {
        try
        {
            await asyncFunc();
        }
        catch (Exception ex)
        {
            // Show an error message if loading fails
            App.Current.MainWindow.QueueNotification($"{asyncFunc.Method.Name}(): {ex.Message}", "Error");
            Trace.WriteLine($"{asyncFunc.Method.Name}(): {ex.Message}");
            return;
        }

        // Successful, show success message
        App.Current.MainWindow.QueueNotification(successMessage, "Success");
    }

    internal static string YearDiffToOrdinal(DateTime start, DateTime end)
    {
        var years = end.Year - start.Year;
        if (end.Month < start.Month || (end.Month == start.Month && end.Day < start.Day)) years--;

        var rem100 = years % 100;
        if (rem100 is >= 11 and <= 13) return $"{years}th";

        return (years % 10) switch
        {
            1 => $"{years}st",
            2 => $"{years}nd",
            3 => $"{years}rd",
            _ => $"{years}th"
        };
    }
}