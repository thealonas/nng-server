using nng_server.Intervals;

namespace nng_server.Models;

public class TimeData
{
    public TimeData(Type task, DateTime finishedTime, DelayInterval interval)
    {
        Task = task;
        FinishedTime = finishedTime;
        Interval = interval;
    }

    public Type Task { get; }
    public DateTime FinishedTime { get; }
    public DelayInterval Interval { get; }
}
