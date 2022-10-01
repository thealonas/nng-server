using System.Text;
using nng_server.Configs;
using nng_server.Tasks;
using nng.Services;
using nng.VkFrameworks;
using Sentry;

namespace nng_server;

public static class Program
{
    public static async Task Main()
    {
        var config = ConfigurationManager.Configuration;
        using (SentrySdk.Init(options =>
               {
                   options.Dsn = "https://7331872167a64e7ba32f9f1b1b6f0c77@o555933.ingest.sentry.io/6379574";
                   options.Environment = config.SentryEnvironment;
                   options.TracesSampleRate = 1.0;
               }))
        {
            Console.OutputEncoding = OperatingSystem.IsWindows() ? Encoding.Unicode : Encoding.UTF8;
            Console.InputEncoding = Encoding.Unicode;

            var vk = new VkFramework(config.Token);
            var version = typeof(Program).Assembly.GetName().Version;
            var info = new ProgramInformationService(version ?? throw new ArgumentNullException(nameof(version)),
                false);

            var tasks = new List<ServerTask>
            {
                new StatusServer(info, vk),
                new DogsServer(info, vk),
                new EditorServer(info, vk),
                new BanServer(info, vk)
            };

            var manager = new TaskManager(info, tasks);
            var cts = new CancellationTokenSource().Token;
            await manager.RunTasks(cts);
        }
    }
}
