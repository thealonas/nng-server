using nng_server.Intervals;
using nng_server.Structs;
using nng.DatabaseProviders;
using nng.Enums;
using nng.Services;
using nng.VkFrameworks;
using Sentry;
using VkNet.Model.Attachments;

namespace nng_server.Tasks;

public class WallServer : ServerTask
{
    private readonly GroupsDatabaseProvider _groups;

    public WallServer(ProgramInformationService info, TokensDatabaseProvider tokens, GroupsDatabaseProvider groups)
        : base(nameof(WallServer), tokens, info, new TimeInterval(new IntervalInfo(13, 20)))
    {
        _groups = groups;
    }

    public override void Start()
    {
        base.Start();
        Logger.Log("Начинаем очистку постов…");
        CleanUp();

        Logger.Log("Начинаем процесс репостов…");
        Repost();
    }

    #region CleanUp

    private void CleanUp()
    {
        var post = GetLastPostFromMainGroup();

        Logger.Log($"Последний пост в основной группе — {post}");
        var groups = _groups.Collection.ToList().Select(x => x.GroupId);

        foreach (var group in groups)
        {
            Logger.Log($"Группа: {group}");
            var posts = GetPosts(group).ToList();
            if (!NeedToCleanUp(post, posts))
            {
                Logger.Log($"{group} не требует очистки");
                continue;
            }

            Logger.Log($"Начинаем очистку {group}");
            DeletePosts(group, posts, post);
        }
    }

    private static bool NeedToCleanUp(long lastPostId, IEnumerable<Post> posts)
    {
        return posts.Any(x => x.CopyHistory.Any(y => y.Id != lastPostId));
    }

    private long GetLastPostFromMainGroup()
    {
        var targetPost = GetPosts(ServerConstants.ServerConstants.MainGroup).First();
        return targetPost.Id ?? throw new InvalidOperationException();
    }

    private IEnumerable<Post> GetPosts(long group)
    {
        return Framework.GetAllPostsVkScript(group).WallPosts.ToList();
    }

    private void DeletePosts(long group, IEnumerable<Post> posts, long exceptionId)
    {
        var targetPosts = GetPostsWithoutReposted(posts, exceptionId).ToList();

        if (!targetPosts.Any())
        {
            Logger.Log($"В группе {group} нет постов для удаления");
            return;
        }

        foreach (var post in targetPosts)
        {
            Logger.Log($"Удаляем пост {post.Id} из {group}");
            Framework.DeletePost(group, post.Id ?? throw new InvalidOperationException());
        }
    }

    private static IEnumerable<Post> GetPostsWithoutReposted(IEnumerable<Post> posts, long originalPostId)
    {
        return posts.Where(x => x.CopyHistory is null || x.CopyHistory.All(y => y.Id != originalPostId));
    }

    #endregion

    #region Repost

    private void Repost()
    {
        var groups = _groups.Collection.ToList().Select(x => x.GroupId);

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

    #endregion
}
