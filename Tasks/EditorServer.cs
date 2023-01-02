using nng_server.Intervals;
using nng.Constants;
using nng.Enums;
using nng.Models;
using nng.Services;
using nng.VkFrameworks;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;

namespace nng_server.Tasks;

public sealed class EditorServer : ServerTask
{
    private bool _exit;
    private long _group;
    private GroupData _groupData;

    public EditorServer(ProgramInformationService info, VkFramework framework) : base(nameof(EditorServer), info,
        framework, new DelayInterval(TimeSpan.FromDays(5)))
    {
    }

    public override void Start()
    {
        base.Start();

        _group = GetGroup();

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

    private long GetGroup()
    {
        var group = Data.GroupList.Reverse().First();
        return group;
    }
}
