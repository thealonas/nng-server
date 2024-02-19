namespace nng_server.Formatters;

public class PluralFormatter : ICustomFormatter, IFormatProvider
{
    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        if (arg is null) return string.Format(format ?? string.Empty, arg);
        var parts = format?.Split(':');
        if (parts?[0] != "P") return string.Format(format ?? string.Empty, arg);
        var partIndex = arg.ToString() == "1" ? 2 : 1;
        return $"{arg} {(parts.Length > partIndex ? parts[partIndex] : "")}";
    }

    public object GetFormat(Type formatType)
    {
        return (formatType == typeof(ICustomFormatter) ? this : null) ?? throw new NullReferenceException();
    }
}
