namespace nng_server.Logging;

public enum LogType
{
    Info,
    Warning,
    Error
}

public static class LogTypeExtensions
{
    public static ConsoleColor GetColor(this LogType type)
    {
        return type switch
        {
            LogType.Info => ConsoleColor.Green,
            LogType.Warning => ConsoleColor.Yellow,
            LogType.Error => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
    }
}
