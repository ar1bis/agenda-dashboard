using System.Diagnostics;

namespace AgendaDashboard.Utilities;

internal class TimestampConsoleTraceListener(bool useErrorStream) : ConsoleTraceListener(useErrorStream)
{
    private static string GetTimestamp() => $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ";

    public override void Write(string? message)
    {
        base.Write(GetTimestamp() + message);
    }

    public override void WriteLine(string? message)
    {
        base.WriteLine(GetTimestamp() + message);
    }
}

internal class TimestampTextWriterTraceListener(string? fileName) : TextWriterTraceListener(fileName)
{
    private static string GetTimestamp() => $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ";

    public override void Write(string? message)
    {
        base.Write(GetTimestamp() + message);
    }

    public override void WriteLine(string? message)
    {
        base.WriteLine(GetTimestamp() + message);
    }
}