using nng_server.Containers;
using nng_server.Interfaces;
using nng.DatabaseProviders;
using nng.Logging;
using nng.Services;
using nng.VkFrameworks;

namespace nng_server.Tasks;

public class ServerTask
{
    private readonly VkFrameworkContainer _container;
    private readonly TokensDatabaseProvider _tokens;
    private protected readonly Logger Logger;

    protected ServerTask(string name, TokensDatabaseProvider tokens, ProgramInformationService info,
        IUpdatable interval)
    {
        _tokens = tokens;
        Logger = new Logger(info, name);
        Interval = interval;
        _container = VkFrameworkContainer.GetInstance();
    }

    private protected VkFramework Framework => _container.Framework;

    public IUpdatable Interval { get; }

    private void UpdateToken()
    {
        _container.UpdateToken(_tokens);
    }

    public virtual void Start()
    {
        UpdateToken();
    }
}
