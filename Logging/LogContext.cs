namespace nng_server.Logging;

public class LogContext
{
    private readonly string _name;

    public LogContext(string name)
    {
        _name = name;
    }

    private static void LogBase(string message, ConsoleColor color)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = oldColor;
    }

    public void Log(string message, LogType logType)
    {
        LogBase($"[{_name}] {message}", logType.GetColor());
    }

    public void Log(string message)
    {
        LogBase($"[{_name}] {message}", LogType.Info.GetColor());
    }
}
