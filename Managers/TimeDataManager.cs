using nng_server.Intervals;
using nng_server.Models;
using nng_server.Tasks;

namespace nng_server.Managers;

public static class TimeDataManager
{
    private static ServerStartupsDatabaseProvider _db = null!;

    public static void Init(ServerStartupsDatabaseProvider server)
    {
        _db = server;
    }

    public static bool IsAllowedToRun(this ServerTask task)
    {
        if (task.Interval is not DelayInterval delayInterval) return true;

        var timeData = _db.Collection.ToList();

        if (!timeData.Any(x => x.Task.Equals(task.GetType().Name))) return true;

        var data = timeData.First(x => x.Task.Equals(task.GetType().Name));
        return data.FinishedTime.AddSeconds(delayInterval.WaitTime.TotalSeconds) <= DateTime.Now;
    }

    public static void AddTask(this ServerTask task, DateTime startTime, DelayInterval interval)
    {
        var timeData = _db.Collection.ToList();
        if (timeData.Any(x => x.Task.Equals(task.GetType().Name))) return;

        var data = new TimeData(task.GetType().Name, startTime, interval.WaitTime.TotalSeconds);
        _db.Collection.Insert(data);
    }

    public static void CleanUp()
    {
        var allData = _db.Collection.ToList();

        foreach (var data in allData.Where(data => data.FinishedTime.AddSeconds(data.Interval)
                                                   <= DateTime.Now))
            _db.Collection.Delete(data);
    }
}
