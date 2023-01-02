namespace nng_server.Structs;

public readonly struct IntervalInfo
{
    public readonly int Hours;
    public readonly int Minutes;

    public IntervalInfo(int minutes, int hours)
    {
        Minutes = minutes;
        Hours = hours;
    }
}
