using nng_server.Intervals;
using nng_server.Structs;
using nng.Enums;
using nng.Services;
using nng.VkFrameworks;
using Sentry;
using VkNet.Model.Attachments;

namespace nng_server.Tasks;

public class RepostServer : ServerTask
{
    public RepostServer(ProgramInformationService info, VkFramework framework) : base(nameof(RepostServer), info,
        framework,
        new TimeInterval(new IntervalInfo(13, 20)))
    {
    }

    public override void Start()
    {
        base.Start();

        var groups = Data.GroupList;

        var lastPostId = Framework.GetAllPostsVkScript(ServerConstants.ServerConstants.MainGroup).WallPosts.First().Id;

        if (lastPostId is null)
        {
            Logger.Log("Не удалось получить последний пост");
            return;
        }

        Logger.Log($"Последний пост: {lastPostId}");

        var posts = GetGroupsAndPosts(groups);

        var groupsToRepost = posts.Where(x => x.Value.All(post => !post.CopyHistory.Any() || post.CopyHistory
                .All(copy => copy != null && copy.Id != lastPostId)))
            .Select(x => x.Key).ToList();

        if (groupsToRepost.Count < 0)
        {
            Logger.Log("Нет новых постов");
            return;
        }

        VkFrameworkExecution.WaitTime = TimeSpan.FromHours(1);

        foreach (var group in groupsToRepost) RepostAndCloseComments(group, (long) lastPostId);

        VkFrameworkExecution.WaitTime = TimeSpan.FromSeconds(10);
    }

    private Dictionary<long, List<Post>> GetGroupsAndPosts(IEnumerable<long> groups)
    {
        var posts = new Dictionary<long, List<Post>>();

        foreach (var group in groups)
            try
            {
                Logger.Log($"Получаем посты сообщества {group}");
                posts.Add(group, Framework.GetAllPostsVkScript(group).WallPosts.ToList());
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                Logger.Log($"Ошибка при получении постов сообщества {group}: {e.Message}");
            }

        return posts;
    }

    private void RepostAndCloseComments(long group, long post)
    {
        try
        {
            var res = Framework.Repost(group, $"wall-{ServerConstants.ServerConstants.MainGroup}_{post}");
            Logger.Log($"Репостнули {post} в сообщество {group}");

            if (res.PostId == null) return;

            VkFrameworkExecution.Execute(() => Framework.Api.Wall.CloseComments(-group, (long) res.PostId));
            Logger.Log($"Закрыли комментарии в посте {res.PostId}", LogType.Debug);
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
            Logger.Log($"Ошибка при репосте поста {post} сообщества {group}: {e.Message}");
        }
    }
}
