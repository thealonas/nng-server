using System.Text.Json.Serialization;
using nng_server.Intervals;
using nng.Constants;
using nng.DatabaseProviders;
using nng.Extensions;
using nng.Services;
using nng.VkFrameworks;
using VkNet.Utils;

namespace nng_server.Tasks;

public class StatusServer : ServerTask
{
    private const string Status = "редактор после 50 и 100 подписчиков (или через бота)";
    private readonly GroupsDatabaseProvider _groups;

    public StatusServer(ProgramInformationService info, TokensDatabaseProvider tokens, GroupsDatabaseProvider groups)
        : base(nameof(StatusServer), tokens, info, new DelayInterval(TimeSpan.FromDays(3)))
    {
        _groups = groups;
    }

    private IEnumerable<MembersResponse> GetMembersCount(IEnumerable<long> groups)
    {
        var groupsIds = string.Join(",", groups);

        var response = VkFrameworkExecution.ExecuteWithReturn(() =>
            Framework.Api.Call("execute.getMembersInGroups", new VkParameters
            {
                {"groups", groupsIds}
            })).ToCollectionOf(vkResponse =>
        {
            var id = long.Parse(vkResponse["id"]);
            var membersCount = long.Parse(vkResponse["members"]);
            return new MembersResponse(membersCount, id);
        });

        return response.ToList();
    }

    public override void Start()
    {
        base.Start();

        VkFramework.CaptchaSecondsToWait = Constants.CaptchaEditorWaitTime;

        var groups = _groups.Collection.ToList().Select(x => x.GroupId).ToList();

        Logger.Log($"Всего групп: {groups.Count}");

        Logger.Log("Получаю статусы в группах");

        var statuses = VkFrameworkExecution.ExecuteWithReturn(() =>
        {
            var response = Framework.Api.Call("execute.getGroupsStatus", new VkParameters
            {
                {"groups", string.Join(",", groups)}
            });

            return response.ToCollectionOf(vkResponse =>
            {
                var id = long.Parse(vkResponse["id"]);
                var status = vkResponse["status"];
                return new StatusResponse(status, id);
            });
        }).ToList();

        var groupsWithStatus = statuses.Where(x => !string.IsNullOrEmpty(x.GroupStatus)).ToList();

        Logger.Log($"Количество групп со статусом: {groupsWithStatus.Count}, начинаю получать участников…");

        var groupsBy12 = groupsWithStatus.TakeBy(12);

        var members = new List<MembersResponse>();

        foreach (var group in groupsBy12) members.AddRange(GetMembersCount(group.Select(x => x.Id)));

        foreach (var group in members)
        {
            Logger.Log("Обрабатываю группу " + group.Id);

            if (group.MembersCount < 100)
            {
                Logger.Log("У группы меньше 100 участников");
                var status = statuses.First(x => x.Id.Equals(group.Id)).GroupStatus;
                if (status.Equals(Status))
                {
                    Logger.Log("Статус группы совпадает с требуемым, продолжаю");
                    continue;
                }

                Logger.Log("Статус группы не совпадает с требуемым, устанавливаю статус…");

                Framework.SetGroupStatus(group.Id, Status);
                continue;
            }

            Logger.Log("У группы больше 100 участников, удаляю статус…");

            Framework.SetGroupStatus(group.Id, string.Empty);
        }
    }

    private class MembersResponse
    {
        public MembersResponse(long membersCount, long id)
        {
            MembersCount = membersCount;
            Id = id;
        }

        [JsonPropertyName("id")] public long Id { get; }

        [JsonPropertyName("members_count")] public long MembersCount { get; }
    }

    private class StatusResponse
    {
        public StatusResponse(string groupStatus, long id)
        {
            GroupStatus = groupStatus;
            Id = id;
        }

        [JsonPropertyName("id")] public long Id { get; }

        [JsonPropertyName("status")] public string GroupStatus { get; }
    }
}
