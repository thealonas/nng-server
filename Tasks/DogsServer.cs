using nng_server.Configs;
using nng.Constants;
using nng.Enums;
using nng.Helpers;
using nng.Services;
using nng.VkFrameworks;
using VkNet.Exception;

namespace nng_server.Tasks;

public class DogsServer : ServerTask
{
    private readonly Config _config = ConfigurationManager.Configuration;
    private long[] _groups;

    public DogsServer(ProgramInformationService info, VkFramework framework) : base(nameof(DogsServer), info, framework,
        TimeSpan.FromDays(7))
    {
        _groups = DataHelper.GetDataAsync(_config.DataUrl).GetAwaiter().GetResult().GroupList;
    }

    public override void Start()
    {
        VkFramework.CaptchaSecondsToWait = Constants.CaptchaBlockWaitTime;
        foreach (var group in _groups)
        {
            Logger.Log($"Переходим к сообществу {group}");
            var members = Framework.GetGroupData(group);
            var bannedManagers = members.Managers.Where(x => x.IsDeactivated).ToList();
            if (!bannedManagers.Any()) Logger.Log("Собачек не было найдено", LogType.Warning);
            foreach (var manager in bannedManagers) DeleteDog(group, manager.Id);
        }

        Logger.Log("Все сообщества были обработаны…");
        Logger.Log("Пауза на 7 дней");
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        _groups = DataHelper.GetDataAsync(_config.DataUrl).GetAwaiter().GetResult().GroupList;
    }

    private void DeleteDog(long group, long user)
    {
        try
        {
            Framework.EditManager(user, group, null);
            Logger.Log($"Сняли редактора {user} в сообществе {group}");
        }
        catch (VkApiException e)
        {
            Logger.Log($"Невозможно удалить {user} из сообщества {group}", LogType.Error);
            Logger.Log($"{e.GetType()}: {e.Message}\n{e.StackTrace}", LogType.Error);
        }
    }
}
