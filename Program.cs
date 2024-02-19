using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using nng_server.Configs;
using nng_server.Managers;
using nng_server.Models;
using nng_server.Tasks;
using nng.DatabaseProviders;
using nng.Services;
using nng.VkFrameworks;
using Redis.OM;
using Sentry;

namespace nng_server;

public static class Program
{
    private static ILogger? _mainLogger;

    public static void Main()
    {
        var config = ConfigurationManager.Configuration;
        using (SentrySdk.Init(options =>
               {
                   options.Dsn = "https://7331872167a64e7ba32f9f1b1b6f0c77@o555933.ingest.sentry.io/6379574";
                   options.Environment = "dev";
                   options.TracesSampleRate = 1.0;
               }))
        {
            Console.OutputEncoding = OperatingSystem.IsWindows() ? Encoding.Unicode : Encoding.UTF8;
            Console.InputEncoding = Encoding.Unicode;

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.ColorBehavior = LoggerColorBehavior.Default;
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.UseUtcTimestamp = false;
                });
            });

            _mainLogger = loggerFactory.CreateLogger("nng server");

            var redisUrl = config.RedisUrl;

            var connectionProvider = new RedisConnectionProvider(redisUrl);

            var tokens = new TokensDatabaseProvider(loggerFactory.CreateLogger<TokensDatabaseProvider>(),
                connectionProvider);

            var version = typeof(Program).Assembly.GetName().Version;

            var info = new ProgramInformationService(version ?? throw new ArgumentNullException(nameof(version)),
                false);

            var groupsDb = new GroupsDatabaseProvider(loggerFactory.CreateLogger<GroupsDatabaseProvider>(),
                connectionProvider);
            var statsDb = new GroupStatsDatabaseProvider(loggerFactory.CreateLogger<GroupStatsDatabaseProvider>(),
                connectionProvider);
            var settingsDb = new SettingsDatabaseProvider(loggerFactory.CreateLogger<SettingsDatabaseProvider>(),
                connectionProvider);
            var usersDb = new UsersDatabaseProvider(loggerFactory.CreateLogger<UsersDatabaseProvider>(),
                connectionProvider);
            var serverStartupsDb = new ServerStartupsDatabaseProvider(
                loggerFactory.CreateLogger<ServerStartupsDatabaseProvider>(), connectionProvider);

            TimeDataManager.Init(serverStartupsDb);

            VkFramework.OnCaptchaWait += HandleCaptcha;

            var tasks = new List<ServerTask>
            {
                new StatusServer(info, tokens, groupsDb),
                new DogsServer(info, tokens, usersDb),
                new RevokeServer(info, tokens, usersDb),
                new WallServer(info, tokens, groupsDb),
                new EditorServer(info, tokens, groupsDb, statsDb),
                new BanServer(info, tokens, usersDb, groupsDb, settingsDb)
            };

            var manager = new TaskManager(info, tasks);
            var cts = new CancellationTokenSource().Token;
            manager.RunTasks(cts);

            VkFramework.OnCaptchaWait -= HandleCaptcha;
        }
    }

    private static void HandleCaptcha(object? sender, CaptchaEventArgs captchaEventArgs)
    {
        _mainLogger?.LogWarning("Каптча! Ждем {Seconds} секунд…", captchaEventArgs.SecondsToWait.TotalSeconds);
    }
}
