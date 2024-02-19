using nng_server.Intervals;
using nng.DatabaseProviders;
using nng.Logging;
using nng.Services;
using nng.VkFrameworks;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace nng_server.Tasks;

public sealed class RevokeServer : ServerTask
{
    private readonly Logger _logger;
    private readonly UsersDatabaseProvider _users;

    public RevokeServer(ProgramInformationService info, TokensDatabaseProvider tokens, UsersDatabaseProvider users)
        : base(nameof(RevokeServer), tokens, info, new DelayInterval(TimeSpan.FromDays(30)))
    {
        _users = users;
        _logger = new Logger(info, nameof(RevokeServer));
    }

    public override void Start()
    {
        base.Start();

        var users = _users.Collection.ToList()
            .Where(x => x.BannedInfo is null && x is {Admin: false, Thanks: false, Groups.Count: > 0}).ToList();

        _logger.Log("Количество на проверку: " + users.Count);

        if (!users.Any()) return;

        var usersWithOnlineResponse = VkFrameworkExecution.ExecuteWithReturn(() =>
            Framework.Api.Call("execute.getUsers", new VkParameters
            {
                {"user_ids", string.Join(",", users.Select(x => x.UserId))}
            }));

        var usersWithOnline = usersWithOnlineResponse.ToReadOnlyCollectionOf(User.FromJson);

        _logger.Log("Количество с онлайном: " + usersWithOnline.Count);

        var usersToRevoke = usersWithOnline.Where(user =>
            user.LastSeen?.Time != null && user.LastSeen.Time.Value.AddMonths(6) < DateTime.Now).ToList();

        _logger.Log("Количество для отзыва: " + usersToRevoke.Count);

        foreach (var user in usersToRevoke)
        {
            var userGroups = users.First(x => x.UserId.Equals(user.Id)).Groups ?? new List<long>();

            if (!userGroups.Any())
            {
                _logger.Log($"У {user.Id} нет групп");
                continue;
            }

            foreach (var group in userGroups)
            {
                _logger.Log($"Отзываю редактора у {user.Id} в группе {group}");
                VkFrameworkExecution.Execute(() =>
                {
                    Framework.Api.Groups.EditManager(new GroupsEditManagerParams
                    {
                        GroupId = group,
                        UserId = user.Id
                    });
                });
            }
        }
    }
}
