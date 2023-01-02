using nng_server.Interfaces;
using nng_server.Structs;

namespace nng_server.Intervals;

public class TimeInterval : IUpdatable
{
    private readonly IntervalInfo _period;

    public TimeInterval(IntervalInfo period, bool updateAtStartup = true)
    {
        _period = period;
        UpdateAtStartup = updateAtStartup;
    }

    public TimeSpan WaitTime
    {
        get
        {
            var now = DateTime.Now;
            var next = new DateTime(now.Year, now.Month, now.Day, _period.Hours, _period.Minutes, 0);
            if (next < now)
                next = next.AddDays(1);
            return next - now;
        }
    }

    public bool UpdateAtStartup { get; }
}
