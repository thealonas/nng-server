using System.Text.Json.Serialization;
using Redis.OM.Modeling;

namespace nng_server.Models;

[Document(StorageType = StorageType.Json, Prefixes = new[] {"info:startups:server"},
    IndexName = "info")]
public class TimeData
{
    [JsonConstructor]
    public TimeData(string task, DateTime finishedTime, double interval)
    {
        Task = task;
        FinishedTime = finishedTime;
        Interval = interval;
    }

    [JsonInclude]
    [RedisIdField]
    [Indexed(PropertyName = "task")]
    [JsonPropertyName("task")]
    public string Task { get; set; }

    [JsonInclude]
    [Indexed(PropertyName = "finished_time")]
    [JsonPropertyName("finished_time")]
    public DateTime FinishedTime { get; set; }

    [JsonInclude]
    [Indexed(PropertyName = "interval")]
    [JsonPropertyName("interval")]
    public double Interval { get; set; }
}
