using nng_server.Intervals;
using nng.Services;
using nng.VkFrameworks;
using Sentry;

namespace nng_server.Tasks;

public class CleanServer : ServerTask
{
    public CleanServer(ProgramInformationService info, VkFramework framework) :
        base(nameof(CleanServer), info, framework, new DelayInterval(TimeSpan.FromDays(10)))
    {
    }

    public override void Start()
    {
        base.Start();

        var targetPost = Framework.GetAllPostsVkScript(ServerConstants.ServerConstants.MainGroup).WallPosts.First();
        if (targetPost.Id is null)
        {
            Logger.Log("Последний пост не найден");
            return;
        }

        var postId = (long) targetPost.Id;

        foreach (var group in Data.GroupList)
        {
            Logger.Log($"Обрабатываем сообщество {group}");

            try
            {
                DeletePostsExceptOne(group, postId);
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                Logger.Log($"Не удалось удалить посты из группы {group}: {e.Message}");
            }
        }
    }

    private void DeletePostsExceptOne(long group, long targetPost)
    {
        var allPosts = Framework.GetAllPostsVkScript(group).WallPosts.ToList();
        var posts = allPosts
            .Where(x => x.CopyHistory is null || !x.CopyHistory.Any() || x.CopyHistory[0].Id != targetPost).ToList();
        if (!posts.Any())
        {
            Logger.Log($"В сообществе {group} нет постов на удаление");
            return;
        }

        Logger.Log($"targetPost: {targetPost}");

        foreach (var post in posts)
        {
            if (post.Id is null) continue;
            Logger.Log($"Удаление поста {post.Id} из группы {group}");
            try
            {
                Framework.DeletePost(group, (long) post.Id);
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                Logger.Log($"Не удалось удалить пост {post.Id} из группы {group}: {e.Message}");
            }
        }
    }
}
