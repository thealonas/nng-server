using Newtonsoft.Json;
using nng_server.Intervals;
using nng_server.Models;
using nng_server.Tasks;

namespace nng_server.Managers;

public static class TimeDataManager
{
    private const string JsonPath = "startup.json";

    private static HashSet<TimeData> Data
    {
        get
        {
            CheckPath();
            return ReadData();
        }

        set
        {
            CheckPath();
            SaveData(value);
        }
    }

    private static HashSet<TimeData> ReadData()
    {
        var json = File.ReadAllText(JsonPath);
        return JsonConvert.DeserializeObject<HashSet<TimeData>>(json) ?? new HashSet<TimeData>();
    }

    private static void SaveData(HashSet<TimeData> data)
    {
        var json = JsonConvert.SerializeObject(data);
        File.WriteAllText(JsonPath, json);
    }

    private static void CheckPath()
    {
        if (!File.Exists(JsonPath)) File.Create(JsonPath).Close();
    }

    public static bool IsAllowedToRun(this ServerTask task)
    {
        if (task.Interval is not DelayInterval delayInterval) return true;
        if (!Data.Any(x => x.Task.Name.Equals(task.GetType().Name))) return true;

        var data = Data.First(x => x.Task.Name.Equals(task.GetType().Name));
        return data.FinishedTime.AddSeconds(delayInterval.WaitTime.TotalSeconds) <= DateTime.Now;
    }

    public static void AddTask(this ServerTask task, DateTime startTime, DelayInterval interval)
    {
        if (Data.Any(x => x.Task.Name.Equals(task.GetType().Name))) return;

        var data = new TimeData(task.GetType(), startTime, interval);

        var newData = Data;
        newData.Add(data);
        Data = newData;
    }

    public static void CleanUp()
    {
        var optimisedData = Data;

        foreach (var data in Data.Where(data => data.FinishedTime.AddSeconds(data.Interval.WaitTime.TotalSeconds)
                                                <= DateTime.Now))
            optimisedData.Remove(data);

        Data = optimisedData;
    }
}
