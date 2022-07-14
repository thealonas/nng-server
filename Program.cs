using System.Text;
using nng.VkFrameworks;
using nng_server.Configs;
using nng_server.Tasks;
using Sentry;

namespace nng_server;

public static class Program
{
    public static void Main()
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

            var editor = new EditorServer(vk, () => true);

            var dogs = new DogsServer(vk, () =>
            {
                Task.Delay(TimeSpan.FromDays(7)).GetAwaiter().GetResult();
                return true;
            });

            var status = new StatusServer(vk, () =>
            {
                Task.Delay(TimeSpan.FromDays(5)).GetAwaiter().GetResult();
                return true;
            });

            new Thread(dogs.Start).Start();
            new Thread(status.Start).Start();
            editor.Start();
        }
    }
}
