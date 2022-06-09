using Newtonsoft.Json;

namespace nng_server.Configs;

public class Config
{
    public Config(string dataUrl, string token, int updateTimeInMinutes, string sentryEnvironment)
    {
        DataUrl = dataUrl;
        Token = token;
        UpdateTimeInMinutes = updateTimeInMinutes;
        SentryEnvironment = sentryEnvironment;
    }

    public string DataUrl { get; }
    public string Token { get; }
    public int UpdateTimeInMinutes { get; }

    public string SentryEnvironment { get; }
}

public static class ConfigurationManager
{
    public static Config Configuration =>
        JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Configs",
            "Configuration.json"))) ?? throw new NullReferenceException("Data is missing");
}
