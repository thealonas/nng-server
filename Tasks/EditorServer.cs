using nng_server.Intervals;
using nng.Constants;
using nng.DatabaseProviders;
using nng.Enums;
using nng.Models;
using nng.Services;
using nng.VkFrameworks;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;

namespace nng_server.Tasks;

public sealed class EditorServer : ServerTask
{
    private readonly GroupsDatabaseProvider _groups;
    private readonly GroupStatsDatabaseProvider _stats;

    private bool _exit;
    private long _group;
    private GroupData _groupData;

    public EditorServer(ProgramInformationService info, TokensDatabaseProvider tokens, GroupsDatabaseProvider groups,
        GroupStatsDatabaseProvider stats) : base(nameof(EditorServer), tokens, info,
        new DelayInterval(TimeSpan.FromDays(5)))
    {
        _groups = groups;
        _stats = stats;
    }

    public override void Start()
    {
        base.Start();

        if (!TryGetGroup(out var group))
        {
            Logger.Log("Все сообщества обработаны");
            return;
        }

        _group = group;

        VkFramework.CaptchaSecondsToWait = Constants.CaptchaEditorWaitTime;

        while (!_exit)
            try
            {
                _groupData = Framework.GetGroupData(_group);
                if (_groupData.Managers.Count >= _groupData.AllUsers.Count)
                {
                    Logger.Log($"Все пользователи в группе {_group} менеджеры");
                    _exit = true;
                    continue;
                }

                switch (_groupData.AllUsers.Count)
                {
                    case < 54:
                        Logger.Log($"В группе {_group} меньше 54 человек ({_groupData.AllUsers.Count})");
                        return;
                    case < 100:
                        Logger.Log($"Начинаем выдачу в группе {_group}…");
                        GiveEditor();
                        _exit = true;
                        continue;
                    case >= 100:
                        Logger.Log($"В группе {_group} больше 100 человек, процесс завершен");
                        _exit = true;
                        continue;
                }
            }
            catch (Exception e)
            {
                Logger.Log($"{e.GetType()}: {e.Message}", LogType.Error);
            }
    }

    private void GiveEditor()
    {
        var counter = 0;
        foreach (var user in _groupData.AllUsers.Where(user => !IsManager(user.Id)))
            try
            {
                Framework.EditManager(user.Id, _group, ManagerRole.Editor);
                Logger.Log($"Выдали редактора {user.Id}");
                counter++;
            }
            catch (VkApiException e)
            {
                Logger.Log($"Не удалось выдать редактора {user.Id}: {e.Message}", LogType.Error);
            }

        Logger.Log("Процесс выдачи завершен, выдано редакторов: " + counter);
    }

    private bool IsManager(long user)
    {
        return _groupData.Managers.Any(x => x.Id == user);
    }

    private bool TryGetGroup(out long group)
    {
        group = 0;

        var groups = _groups.Collection.ToList();
        var stats = _stats.Collection.ToList();

        var group50 = groups.FirstOrDefault(x => stats.Any(y =>
            x.GroupId.Equals(y.GroupId) && y.Members < 50));
        if (group50 is not null)
        {
            group = group50.GroupId;
            return true;
        }

        var group100 = groups.FirstOrDefault(x => stats.Any(y =>
            x.GroupId.Equals(y.GroupId) && y.Members < 100));

        if (group100 is null) return false;

        group = group100.GroupId;
        return true;
    }
}
