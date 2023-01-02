using nng_server.Interfaces;

namespace nng_server.Intervals;

public class DelayInterval : IUpdatable
{
    private readonly TimeSpan _delay;

    public DelayInterval(TimeSpan delay, bool updateAtStart = true)
    {
        _delay = delay;
        UpdateAtStartup = updateAtStart;
    }

    public TimeSpan WaitTime
    {
        get
        {
            var delayedRun = DateTime.Now.Add(_delay);
            var nextRun = delayedRun - DateTime.Now;
            if (nextRun < TimeSpan.Zero) nextRun = TimeSpan.Zero;
            return nextRun;
        }
    }

    public bool UpdateAtStartup { get; }
}
