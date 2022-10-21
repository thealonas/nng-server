using nng.Constants;
using nng.Services;
using nng.VkFrameworks;

namespace nng_server.Tasks;

public class StatusServer : ServerTask
{
    public StatusServer(ProgramInformationService info, VkFramework framework) : base(nameof(StatusServer), info,
        framework, TimeSpan.FromDays(3))
    {
    }

    public override void Start()
    {
        VkFramework.CaptchaSecondsToWait = Constants.CaptchaEditorWaitTime;

        UpdateData();
        var groups = Data.GroupList.ToList();
        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];

            if (i + 1 == groups.Count && Framework.GetGroupData(group).AllUsers.Count < 100)
            {
                Framework.SetGroupStatus(group, "редактор после 50 и 100 подписчиков (или через бота)");
                Logger.Log($"Установили статус в {group}: редактор после 50 и 100 подписчиков (или через бота)");
                return;
            }

            Framework.SetGroupStatus(group, string.Empty);
            Logger.Log($"Очистили статус в {group}");
        }
    }
}
