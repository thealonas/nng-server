using nng.Constants;
using nng.Helpers;

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
    public static Config Configuration => GetConfiguration();

    private static Config GetConfiguration()
    {
        var dataUrl = EnvironmentHelper.GetString(EnvironmentConstants.DataUrl);
        var token = EnvironmentHelper.GetString(EnvironmentConstants.UserToken);
        var updateTimeInMinutes = EnvironmentHelper.GetInt("UpdateTimeInMinutes", 10);
        var sentryEnvironment = EnvironmentHelper.GetString(EnvironmentConstants.Sentry, "prod");

        return new Config(dataUrl, token, updateTimeInMinutes, sentryEnvironment);
    }
}
