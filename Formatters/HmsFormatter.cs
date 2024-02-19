namespace nng_server.Formatters;

public class HmsFormatter : ICustomFormatter, IFormatProvider
{
    private static readonly Dictionary<string, string> TimeFormats = new()
    {
        {"S", "{0:P:секунд:секунда}"},
        {"M", "{0:P:минут:минута}"},
        {"H", "{0:P:часов:час}"},
        {"D", "{0:P:дней:день}"}
    };

    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        return string.Format(new PluralFormatter(), TimeFormats[format ??
                                                                throw new ArgumentNullException(nameof(format))], arg);
    }

    public object GetFormat(Type? formatType)
    {
        return (formatType == typeof(ICustomFormatter) ? this : null) ?? throw new NullReferenceException();
    }
}
