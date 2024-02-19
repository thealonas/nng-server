using nng_server.Intervals;
using nng.Constants;
using nng.DatabaseProviders;
using nng.Enums;
using nng.Services;
using nng.VkFrameworks;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Utils;

namespace nng_server.Tasks;

public class DogsServer : ServerTask
{
    private readonly UsersDatabaseProvider _users;

    public DogsServer(ProgramInformationService info, TokensDatabaseProvider tokens, UsersDatabaseProvider users)
        : base(nameof(DogsServer), tokens, info, new DelayInterval(TimeSpan.FromDays(7)))
    {
        _users = users;
    }

    public override void Start()
    {
        base.Start();

        VkFramework.CaptchaSecondsToWait = Constants.CaptchaBlockWaitTime;

        var bdUsers = _users.Collection.ToList();

        Logger.Log("Начинаем обработку пользователей…");
        var users = VkFrameworkExecution.ExecuteWithReturn(() =>
        {
            return Framework.Api.Call("execute.getUsers", new VkParameters
            {
                {"user_ids", string.Join(",", bdUsers.Select(x => x.UserId))}
            }).ToVkCollectionOf(User.FromJson);
        });

        Logger.Log($"Общее количество пользователей: {users.Count}");

        var banned = users.Where(x => x.IsDeactivated).ToList();

        Logger.Log($"Количество забаненных пользователей: {banned.Count}");

        foreach (var bannedUser in bdUsers.Where(x => banned.Any(y => y.Id.Equals(x.UserId))))
        {
            Logger.Log($"Обрабатываю пользователя {bannedUser.UserId}");

            if (bannedUser.Groups is null || !bannedUser.Groups.Any())
            {
                Logger.Log($"У пользователя {bannedUser.UserId} нет групп");
                continue;
            }

            foreach (var group in bannedUser.Groups)
            {
                Logger.Log($"Снимаю редактора у {bannedUser.UserId} из группы {group}");
                DeleteDog(group, bannedUser.UserId);
            }
        }

        Logger.Log("Все сообщества были обработаны…");
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
