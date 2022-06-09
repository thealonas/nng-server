using nng.Data;
using nng.Models;
using nng_server.Configs;

namespace nng_server.Tasks;

public class ServerTask
{
    private readonly Config _config = ConfigurationManager.Configuration;
    private readonly Func<bool> _finish;
    protected DataModel Data;

    protected ServerTask(Func<bool> finish)
    {
        _finish = finish;
    }

    public virtual void Start()
    {
    }

    protected virtual void UpdateData()
    {
        Data = DataHelper.GetData(_config.DataUrl);
    }

    private protected void Finished()
    {
        var repeat = _finish();
        if (!repeat) return;

        UpdateData();
        Start();
    }
}
