using nng_server.Configs;
using nng_server.Interfaces;
using nng.Helpers;
using nng.Logging;
using nng.Models;
using nng.Services;
using nng.VkFrameworks;

namespace nng_server.Tasks;

public class ServerTask
{
    private readonly Config _config = ConfigurationManager.Configuration;
    private protected readonly VkFramework Framework;
    private protected readonly Logger Logger;
    private protected DataModel Data = null!;

    public ServerTask(string name, ProgramInformationService info, VkFramework framework, IUpdatable interval)
    {
        Logger = new Logger(info, name);
        Framework = framework;
        Interval = interval;
    }

    public IUpdatable Interval { get; }

    public virtual void Start()
    {
        UpdateData();
    }

    private void UpdateData()
    {
        Data = DataHelper.GetDataAsync(_config.DataUrl).GetAwaiter().GetResult();
    }
}
