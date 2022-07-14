using nng.VkFrameworks;
using nng_server.Logging;

namespace nng_server.Tasks;

public class StatusServer : ServerTask
{
    private readonly LogContext _logContext;
    private readonly VkFramework _vkFramework;

    public StatusServer(VkFramework vkFramework, Func<bool> finish) : base(finish)
    {
        _vkFramework = vkFramework;
        _logContext = new LogContext(nameof(StatusServer));
    }

    public override void Start()
    {
        var groups = Data.GroupList.ToList();
        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];

            if (groups.Count.Equals(i) && _vkFramework.GetGroupData(group).AllUsers.Count < 100)
            {
                _vkFramework.SetGroupStatus(group, "редактор после 50 и 100 подписчиков (или через бота)");
                _logContext.Log($"Установили статус в {group}: редактор после 50 и 100 подписчиков (или через бота)");
                return;
            }

            _logContext.Log($"Очистили статус в {group}");
            _vkFramework.SetGroupStatus(group, string.Empty);
        }

        Finished();
    }
}
