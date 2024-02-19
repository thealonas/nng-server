using nng.Helpers;

namespace nng_server.Configs;

public class Config
{
    public Config(string redisUrl)
    {
        RedisUrl = redisUrl;
    }

    public string RedisUrl { get; }
}

public static class ConfigurationManager
{
    public static Config Configuration => GetConfiguration();

    private static Config GetConfiguration()
    {
        var redisUrl = EnvironmentHelper.GetString("REDIS_URL");
        return new Config(redisUrl);
    }
}
