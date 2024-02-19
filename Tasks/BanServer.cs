using nng_server.Intervals;
using nng.Constants;
using nng.DatabaseProviders;
using nng.Enums;
using nng.Extensions;
using nng.Services;
using nng.VkFrameworks;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Utils;

namespace nng_server.Tasks;

public class BanServer : ServerTask
{
    private readonly GroupsDatabaseProvider _groups;
    private readonly SettingsDatabaseProvider _settings;
    private readonly UsersDatabaseProvider _users;

    private readonly Dictionary<long, Dictionary<long, bool>> _usersThatShouldBeBanned = new();
    private readonly Dictionary<long, List<long>> _usersThatShouldBeUnbanned = new();
    private readonly Dictionary<long, List<long>> _usersWithWrongBanReason = new();

    public BanServer(ProgramInformationService info, TokensDatabaseProvider tokens, UsersDatabaseProvider users,
        GroupsDatabaseProvider groups, SettingsDatabaseProvider settings)
        : base(nameof(BanServer), tokens, info, new DelayInterval(TimeSpan.FromDays(2)))
    {
        _users = users;
        _groups = groups;
        _settings = settings;
    }

    private List<GetBannedResult> GetAllBanned(long group)
    {
        return VkFrameworkExecution.ExecuteWithReturn(() =>
        {
            return Framework.Api.Call("execute.getAllBanned", new VkParameters
            {
                {"group", group}
            }).ToCollectionOf(x => new GetBannedResult
            {
                BanInfo = x["ban_info"],
                Profile = x["profile"]
            }).ToList();
        });
    }

    public override void Start()
    {
        base.Start();

        ClearAll();

        VkFramework.CaptchaSecondsToWait = Constants.CaptchaBlockWaitTime;
        var banComment = _settings.Collection.FindById("main")?.BanComment ??
                         throw new NullReferenceException(typeof(SettingsDatabaseProvider).ToString());

        var targetUsers = _users.Collection.ToList().Where(x => x.Banned).Select(x => x.UserId).ToList();
        foreach (var group in _groups.Collection.ToList().Select(x => x.GroupId))
        {
            Logger.Log($"Обрабатываю группу {group}");
            var bannedResult = GetAllBanned(group);

            Logger.Log($"Всего забанено: {bannedResult.Count}");

            bannedResult.RemoveAll(x => x.Type == SearchResultType.Profile
                                        && x.Profile.IsDeactivated);

            Logger.Log($"Без учета заблокированных: {bannedResult.Count}");

            var currentBannedUsers = bannedResult.Select(x => x.Profile).ToList();

            var managers = Framework.GetGroupData(group).Managers.ToList();

            var shouldBeBanned = GetUsersThatShouldBeBanned(targetUsers,
                currentBannedUsers, managers.Select(x => x.Id).ToList()).ToList();

            if (shouldBeBanned.Any())
                Logger.Log($"Найдено {shouldBeBanned.Count} пользователей, которых нужно забанить");

            var shouldNotBeBanned = GetUsersThatShouldNotBeBanned(targetUsers, currentBannedUsers)
                .ToList();

            if (shouldNotBeBanned.Any())
                Logger.Log($"Найдено {shouldNotBeBanned.Count} пользователей, которых нужно разбанить");

            var withWrongBanReason = GetUsersWithWrongReason(bannedResult, banComment)
                .Where(x => !shouldNotBeBanned.Contains(x)).ToList();

            if (withWrongBanReason.Any())
                Logger.Log(
                    $"Найдено {withWrongBanReason.Count} пользователей, у которых неправильная причина блокировки");

            if (shouldBeBanned.Count > 0)
                _usersThatShouldBeBanned.Add(group, shouldBeBanned.ToDictionary(x => x.Key, x => x.Value));

            if (shouldNotBeBanned.Count > 0) _usersThatShouldBeUnbanned.Add(group, shouldNotBeBanned);

            if (withWrongBanReason.Count > 0) _usersWithWrongBanReason.Add(group, withWrongBanReason);

            if (shouldBeBanned.Any() || shouldNotBeBanned.Any() || withWrongBanReason.Any())
                Logger.Log($"В сообществе {group} найдены отклонения");
        }

        if (!_usersThatShouldBeBanned.Any() && !_usersThatShouldBeUnbanned.Any() && !_usersWithWrongBanReason.Any())
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
                ProcessUsersBan(group, users, banComment);
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
                ProcessUsersBan(group, users.ToDictionary(x => x, _ => false), banComment);
            }
        }
        else
        {
            Logger.Log("Отклонений на исправление причины блокировки нет");
        }
    }

    private void ProcessUsersBan(long group, Dictionary<long, bool> users, string banComment)
    {
        var usersToFire = users.Where(x => x.Value).Select(x => x.Key)
            .ToList().TakeBy(25);

        var counter = 0;
        if (usersToFire.Any()) Logger.Log("Начинаю снимать права у администраторов");

        foreach (var usersBunch in usersToFire)
            try
            {
                VkFrameworkExecution.Execute(() =>
                {
                    Framework.Api.Call("execute.editManagers", new VkParameters
                    {
                        {"users", string.Join(",", usersBunch)},
                        {"group", group},
                        {"role", string.Empty}
                    });
                });

                counter += usersBunch.Count;
                Logger.Log($"Сняли права у {counter} из {users.Count}");
            }
            catch (Exception e)
            {
                Logger.Log($"Произошла ошибка при снятии прав у администраторов: {e.GetType()}: {e.Message}",
                    LogType.Error);
            }

        var allUsers = users.Select(x => x.Key).ToList().TakeBy(25);
        Logger.Log("Начинаю блокировать пользователей");
        counter = 0;
        foreach (var usersBunch in allUsers)
        {
            try
            {
                VkFrameworkExecution.Execute(() =>
                {
                    Framework.Api.Call("execute.banUsers", new VkParameters
                    {
                        {"users", string.Join(",", usersBunch)},
                        {"group", group},
                        {"comment", banComment}
                    });
                });
            }
            catch (Exception e)
            {
                Logger.Log($"Произошла ошибка при блокировке пользователей: {e.GetType()}: {e.Message}", LogType.Error);
            }

            counter += usersBunch.Count;
            Logger.Log($"Заблокировали {counter} из {users.Count}");
        }
    }

    private void ProcessUsersUnban(long group, IReadOnlyCollection<long> users)
    {
        Logger.Log("Начинаю разблокировать пользователей");
        var counter = 0;
        foreach (var usersBunch in users.TakeBy(25))
            try
            {
                VkFrameworkExecution.Execute(() =>
                {
                    Framework.Api.Call("execute.unbanUsers", new VkParameters
                    {
                        {"users", string.Join(",", usersBunch)},
                        {"group", group}
                    });
                });

                counter += usersBunch.Count;
                Logger.Log($"Разблокировали {counter} из {users.Count}");
            }
            catch (Exception e)
            {
                Logger.Log($"Произошла ошибка при разблокировке пользователей: {e.GetType()}: {e.Message}",
                    LogType.Error);
            }
    }

    private static Dictionary<long, bool> GetUsersThatShouldBeBanned(IEnumerable<long> targetUsers,
        IEnumerable<User> actuallyBannedUsers, ICollection<long> managers)
    {
        var targetsToBan = targetUsers.Where(x => !actuallyBannedUsers.Any(y => y.Id.Equals(x))).ToList();
        var output = new Dictionary<long, bool>();
        foreach (var target in targetsToBan)
        {
            var isManager = managers.Contains(target);
            output.Add(target, isManager);
        }

        return output;
    }

    private static IEnumerable<long> GetUsersThatShouldNotBeBanned(IEnumerable<long> bannedUsers,
        IEnumerable<User> currentBannedUsers)
    {
        return currentBannedUsers.Where(x => bannedUsers.All(y => !y.Equals(x.Id))).Select(x => x.Id).ToList();
    }

    private static IEnumerable<long> GetUsersWithWrongReason(IEnumerable<GetBannedResult> banned, string banComment)
    {
        return banned.Where(x => x.BanInfo.Comment != banComment).Select(x => x.Profile.Id);
    }

    private void ClearAll()
    {
        _usersThatShouldBeBanned.Clear();
        _usersThatShouldBeUnbanned.Clear();
        _usersWithWrongBanReason.Clear();
    }
}
