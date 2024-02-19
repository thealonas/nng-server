using Microsoft.Extensions.Logging;
using nng.DatabaseProviders;
using Redis.OM;

namespace nng_server.Models;

public class ServerStartupsDatabaseProvider : DatabaseProvider<TimeData>
{
    public ServerStartupsDatabaseProvider(ILogger<DatabaseProvider<TimeData>> logger, RedisConnectionProvider provider)
        : base(logger, provider)
    {
    }
}
