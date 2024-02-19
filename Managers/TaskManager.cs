using nng_server.Extensions;
using nng_server.Intervals;
using nng_server.Tasks;
using nng.Enums;
using nng.Logging;
using nng.Services;
using Sentry;

namespace nng_server.Managers;

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
        TimeDataManager.CleanUp();

        foreach (var task in _tasks.Where(task => task.IsAllowedToRun()))
        {
            if (!task.Interval.UpdateAtStartup)
            {
                _logger.Log($"Задача {task.GetType().Name} зарегестрирована на отложенный запуск через " +
                            $"{task.Interval.WaitTime.ToHumanReadableString()}", LogType.Warning);

                _lastRun[task] = DateTime.Now;

                continue;
            }

            RunTask(task);
        }

        while (!token.IsCancellationRequested)
        {
            var task = SleepUntilNextTask();

            RunTask(task);
        }
    }

    private void RunTask(ServerTask task)
    {
        TimeDataManager.CleanUp();

        if (!_tasks.Any())
        {
            _logger.Log("Нет задач для запуска", LogType.Error);
            return;
        }

        var name = task.GetType().Name;

        _logger.Log($"Запускаем задачу «{name}»", LogType.Warning);
        try
        {
            task.Start();
        }
        catch (Exception e)
        {
            _logger.Log($"Ошибка при выполнении задачи {task.GetType().Name}", LogType.Error);
            _logger.Log($"{e.GetType()}: {e.Message}\n{e.StackTrace}", LogType.Error);

            SentrySdk.CaptureException(e);
        }

        _lastRun[task] = DateTime.Now;

        if (task.Interval is DelayInterval interval) task.AddTask(DateTime.Now, interval);
    }

    private ServerTask SleepUntilNextTask()
    {
        var min = DateTime.Now.Ticks + TimeSpan.FromDays(1).Ticks;
        ServerTask? targetTask = null;

        foreach (var (task, time) in _lastRun)
        {
            var target = time.Add(task.Interval.WaitTime).Ticks;

            if (target >= min) continue;

            min = target;
            targetTask = task;
        }

        if (targetTask is null)
        {
            var task = FindTaskWithLeastTimeToWaitFor();
            _logger.Log($"Задача {task.GetType().Name} будет запущена через " +
                        $"{task.Interval.WaitTime.ToHumanReadableString()}", LogType.Warning);
            Task.Delay(task.Interval.WaitTime).GetAwaiter().GetResult();
            return task;
        }

        var name = targetTask.GetType().Name;

        _logger.Log($"Следущая задача: {name}", LogType.Warning);

        var timeToWait = targetTask.Interval.WaitTime - (DateTime.Now - _lastRun[targetTask]);
        _logger.Log($"Задача {name} будет запущена через {timeToWait.ToHumanReadableString()}", LogType.Warning);

        if (timeToWait.TotalMilliseconds > 0) Task.Delay(timeToWait).GetAwaiter().GetResult();

        return targetTask;
    }

    private ServerTask FindTaskWithLeastTimeToWaitFor()
    {
        var min = TimeSpan.MaxValue;
        ServerTask? targetTask = null;

        foreach (var task in _tasks)
        {
            var target = task.Interval.WaitTime;

            if (target >= min) continue;

            min = target;
            targetTask = task;
        }

        if (targetTask is null) throw new Exception("Нет задач для запуска");

        return targetTask;
    }
}
