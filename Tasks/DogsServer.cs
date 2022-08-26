using nng_server.Configs;
using nng_server.Logging;
using nng.Helpers;
using nng.VkFrameworks;
using VkNet.Exception;

namespace nng_server.Tasks;

public class DogsServer : ServerTask
{
    private readonly Config _config = ConfigurationManager.Configuration;
    private readonly VkFramework _framework;
    private readonly LogContext _logger;
    private long[] _groups;

    public DogsServer(VkFramework framework, Func<bool> finished) : base(finished)
    {
        _framework = framework;
        _groups = DataHelper.GetData(_config.DataUrl).GroupList;
        _logger = new LogContext(nameof(DogsServer));
    }

    public override void Start()
    {
        foreach (var group in _groups)
        {
            _logger.Log($"Переходим к сообществу {group}");
            var members = _framework.GetGroupData(group);
            var bannedManagers = members.Managers.Where(x => x.IsDeactivated).ToList();
            if (!bannedManagers.Any()) _logger.Log("Собачек не было найдено", LogType.Warning);
            foreach (var manager in bannedManagers) DeleteDog(group, manager.Id);
        }

        _logger.Log("Все сообщества были обработаны…");
        _logger.Log("Пауза на 7 дней");
        Finished();
    }

    protected override void UpdateData()
    {
        base.UpdateData();

        _groups = DataHelper.GetData(_config.DataUrl).GroupList;
    }

    private void DeleteDog(long group, long user)
    {
        VkFramework.CaptchaSecondsToWait = 3600;
        try
        {
            _framework.EditManager(user, group, null);
            _logger.Log($"Сняли редактора {user} в сообществе {group}");
        }
        catch (VkApiException e)
        {
            _logger.Log($"Невозможно удалить {user} из сообщества {group}", LogType.Error);
            _logger.Log($"{e.GetType()}: {e.Message}\n{e.StackTrace}", LogType.Error);
        }
    }
}
