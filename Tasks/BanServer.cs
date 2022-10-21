using nng_server.Configs;
using nng.Constants;
using nng.Enums;
using nng.Helpers;
using nng.Services;
using nng.VkFrameworks;
using VkNet.Exception;
using VkNet.Model;

namespace nng_server.Tasks;

public class BanServer : ServerTask
{
    private readonly Config _config;
    private readonly List<long> _usersFailedToBan = new();

    private readonly Dictionary<long, Dictionary<long, bool>> _usersThatShouldBeBanned = new();
    private readonly Dictionary<long, List<long>> _usersThatShouldBeUnbanned = new();
    private readonly Dictionary<long, List<long>> _usersWithWrongBanReason = new();

    public BanServer(ProgramInformationService info, VkFramework framework) : base(nameof(BanServer), info, framework,
        TimeSpan.FromDays(21))
    {
        _config = ConfigurationManager.Configuration;
        Data = DataHelper.GetDataAsync(_config.DataUrl).GetAwaiter().GetResult();
    }

    public override void Start()
    {
        VkFramework.CaptchaSecondsToWait = Constants.CaptchaBlockWaitTime;

        Data = DataHelper.GetDataAsync(_config.DataUrl).GetAwaiter().GetResult();

        var bannedUsers = Data.Users.Where(x => !x.Deleted).Select(x => x.Id).ToList();
        foreach (var group in Data.GroupList)
        {
            var banned = Framework.GetBannedAlt(group).ToList();
            var users = banned.Select(x => x.Profile).ToList();
            var managers = Framework.GetGroupData(group).Managers.ToList();
            var shouldBeBanned =
                GetUsersThatShouldBeBanned(bannedUsers, users, managers.Select(x => x.Id).ToList()).ToList();
            var shouldNotBeBanned = GetUsersThatShouldNotBeBanned(bannedUsers, users).ToList();
            var withWrongBanReason =
                GetUsersWithWrongReason(banned).Where(x => !shouldNotBeBanned.Contains(x)).ToList();

            if (shouldBeBanned.Count > 0)
                _usersThatShouldBeBanned.Add(group, shouldBeBanned.ToDictionary(x => x.Key, x => x.Value));

            if (shouldNotBeBanned.Count > 0) _usersThatShouldBeUnbanned.Add(group, shouldNotBeBanned);

            if (withWrongBanReason.Count > 0) _usersWithWrongBanReason.Add(group, withWrongBanReason);

            if (shouldBeBanned.Any() || shouldNotBeBanned.Any() || withWrongBanReason.Any())
                Logger.Log($"В сообществе {group} найдены отклонения");
        }

        if (!_usersThatShouldBeBanned.Any() && !_usersThatShouldBeUnbanned.Any())
        {
            Logger.Log("Отклонений в сообществах нет");
            return;
        }

        if (_usersThatShouldBeBanned.Any())
        {
            Logger.Log("Начинаем блокировку");
            foreach (var (group, users) in _usersThatShouldBeBanned)
            {
                Logger.Log($"Переходим к сообществу {group}");
                ProcessUsersBan(group, users, _config.BanComment);
            }
        }
        else
        {
            Logger.Log("Отклонений на блокировку нет");
        }

        if (_usersThatShouldBeUnbanned.Any())
        {
            Logger.Log("Начинаем разблокировку");
            foreach (var (group, users) in _usersThatShouldBeUnbanned)
            {
                Logger.Log($"Переходим к сообществу {group}");
                ProcessUsersUnban(group, users);
            }
        }
        else
        {
            Logger.Log("Отклонений на разблокировку нет");
        }

        if (_usersWithWrongBanReason.Any())
        {
            Logger.Log("Начинаем исправление причины блокировки");
            foreach (var (group, users) in _usersWithWrongBanReason)
            {
                Logger.Log($"Переходим к сообществу {group}");
                ProcessUsersBan(group, users.ToDictionary(x => x, x => true), _config.BanComment);
            }
        }
        else
        {
            Logger.Log("Отклонений на исправление причины блокировки нет");
        }
    }

    private void ProcessUsersBan(long group, Dictionary<long, bool> users, string banComment)
    {
        foreach (var (user, shouldFire) in users)
        {
            if (_usersFailedToBan.Contains(user))
            {
                Logger.Log($"Пользователя {user} не получилось заблокировать группой раннее", LogType.Debug);
                continue;
            }

            if (shouldFire) FireEditor(user, group);

            BanUser(user, group, banComment);
        }
    }

    private void ProcessUsersUnban(long group, IEnumerable<long> users)
    {
        foreach (var user in users) UnblockUser(user, group);
    }

    private void BanUser(long user, long group, string banReason)
    {
        try
        {
            Framework.Block(group, user, banReason);
            Logger.Log($"Заблокировали {user} в сообществе {group}");
        }
        catch (VkApiException e)
        {
            Logger.Log(e);
            Logger.Log($"Не удалось заблокировать {user} в сообществе {group}", LogType.Error);
            _usersFailedToBan.Add(user);
        }
    }

    private void FireEditor(long user, long group)
    {
        try
        {
            Framework.EditManager(user, group, null);
            Logger.Log($"Сняли {user} в сообществе {group}");
        }
        catch (VkApiException e)
        {
            Logger.Log(e);
            Logger.Log($"Не удалось удалить из руководителей {user} в сообществе {group}", LogType.Error);
        }
    }

    private void UnblockUser(long user, long group)
    {
        try
        {
            Framework.UnBlock(group, user);
            Logger.Log($"Разблокировали {user} в сообществе {group}");
        }
        catch (VkApiException e)
        {
            Logger.Log(e);
            Logger.Log($"Не удалось разблокировать {user} в сообществе {group}", LogType.Error);
        }
    }

    private Dictionary<long, bool> GetUsersThatShouldBeBanned(IEnumerable<long> bannedUsers,
        IEnumerable<User> currentBannedUsers, ICollection<long> managers)
    {
        var targetsToBan = bannedUsers.Where(x => currentBannedUsers.All(y => y.Id != x)).ToList();
        var output = new Dictionary<long, bool>();
        foreach (var target in targetsToBan)
        {
            var isManager = managers.Contains(target);
            output.Add(target, isManager);
        }

        return output;
    }

    private IEnumerable<long> GetUsersThatShouldNotBeBanned(IEnumerable<long> bannedUsers,
        IEnumerable<User> currentBannedUsers)
    {
        return currentBannedUsers.Where(x => bannedUsers.All(y => y != x.Id)).Select(x => x.Id).ToList();
    }

    private IEnumerable<long> GetUsersWithWrongReason(IReadOnlyCollection<GetBannedResult> banned)
    {
        return banned.Where(x => x.BanInfo.Comment != _config.BanComment).Select(x => x.Profile.Id);
    }
}
