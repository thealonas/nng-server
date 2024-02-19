using nng_server.Formatters;

namespace nng_server.Extensions;

public static class TimeSpanExtensions
{
    public static string ToHumanReadableString(this TimeSpan ts)
    {
        var cutoff = new SortedList<long, string>
        {
            {59, "{3:S}"},
            {60, "{2:M}"},
            {60 * 60 - 1, "{2:M}, {3:S}"},
            {60 * 60, "{1:H}"},
            {24 * 60 * 60 - 1, "{1:H}, {2:M}"},
            {24 * 60 * 60, "{0:D}"},
            {long.MaxValue, "{0:D}, {1:H}"}
        };

        var find = cutoff.Keys.ToList()
            .BinarySearch((long) ts.TotalSeconds);
        var near = find < 0 ? Math.Abs(find) - 1 : find;
        return string.Format(
            new HmsFormatter(),
            cutoff[cutoff.Keys[near]],
            ts.Days,
            ts.Hours,
            ts.Minutes,
            ts.Seconds);
    }
}
