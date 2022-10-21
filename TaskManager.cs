using System.Text;
using nng_server.Tasks;
using nng.Enums;
using nng.Logging;
using nng.Services;
using Sentry;

namespace nng_server;

public class TaskManager
{
    private readonly Dictionary<ServerTask, DateTime> _lastRun;
    private readonly Logger _logger;
    private readonly List<ServerTask> _tasks;

    public TaskManager(ProgramInformationService info, IEnumerable<ServerTask> tasks)
    {
        _tasks = new List<ServerTask>();
        _lastRun = new Dictionary<ServerTask, DateTime>();
        _logger = new Logger(info, "TaskManager");

        _tasks.AddRange(tasks);
    }

    public void RunTasks(CancellationToken token)
    {
        foreach (var st in _tasks) RunTask(st);

        while (!token.IsCancellationRequested)
        {
            var task = SleepUntilNextTask();
            RunTask(task);
        }
    }

    private ServerTask SleepUntilNextTask()
    {
        var min = TimeSpan.MaxValue;
        var now = DateTime.Now;
        var targetTask = new object();
        foreach (var (task, time) in _lastRun)
        {
            var target = time.Add(task.Interval) - now;

            if (target >= min) continue;

            min = target;
            targetTask = task;
        }

        var name = targetTask.GetType().Name;
        _logger.Log($"Следущая задача: {name}", LogType.Warning);

        if (min.CompareTo(TimeSpan.Zero) > 0)
        {
            _logger.Log($"Ждем {GetHumanReadableInfo(min)}…", LogType.Warning);
            Task.Delay(min).GetAwaiter().GetResult();
        }

        return (ServerTask) targetTask;
    }

    private void RunTask(ServerTask task)
    {
        var name = task.GetType().Name;
        _logger.Log($"Запускаем задачу «{name}»", LogType.Warning);
        try
        {
            task.Start();
        }
        catch (Exception e)
        {
            _logger.Log($"Ошибка при выполнении задачи {task.GetType().Name}", LogType.Error);
            _logger.Log(e);
            SentrySdk.CaptureException(e);
        }

        if (_lastRun.ContainsKey(task)) _lastRun[task] = DateTime.Now;
        else _lastRun.Add(task, DateTime.Now);

        _logger.Log($"Задача «{name}» запустится через {GetHumanReadableInfo(task.Interval)}", LogType.Warning);
    }

    private static string GetHumanReadableInfo(TimeSpan span)
    {
        var sb = new StringBuilder();
        if (span.TotalDays >= 1)
            sb.Append($"{span.TotalDays} дней ");
        else if (span.TotalHours > 0)
            sb.Append($"{span.TotalHours} часов ");
        else if (span.TotalMinutes > 0)
            sb.Append($"{span.TotalMinutes} минут ");
        else sb.Append($"{span.TotalSeconds} секунд ");

        return sb.ToString();
    }
}
