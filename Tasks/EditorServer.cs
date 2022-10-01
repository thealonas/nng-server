using nng_server.Configs;
using nng.Enums;
using nng.Helpers;
using nng.Models;
using nng.Services;
using nng.VkFrameworks;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;

namespace nng_server.Tasks;

public class EditorServer : ServerTask
{
    private readonly Config _config;
    private readonly List<int> _processedGroups;
    private int _group;
    private GroupData _groupData;

    public EditorServer(ProgramInformationService info, VkFramework framework) : base(nameof(EditorServer), info,
        framework, TimeSpan.FromDays(5))
    {
        _processedGroups = new List<int>();
        _config = ConfigurationManager.Configuration;

        Data = DataHelper.GetDataAsync(_config.DataUrl).GetAwaiter().GetResult();

        _group = GetGroup(Data);
    }

    public override void Start()
    {
        Data = DataHelper.GetDataAsync(_config.DataUrl).GetAwaiter().GetResult();
        var exit = false;
        while (!exit)
            try
            {
                _groupData = Framework.GetGroupData(_group);
                if (_groupData.Managers.Count >= _groupData.AllUsers.Count)
                {
                    Logger.Log($"Все пользователи в группе {_group} менеджеры");
                    exit = true;
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
                        exit = true;
                        continue;
                    case >= 100:
                        Logger.Log($"В группе {_group} больше 100 человек, процесс завершен");
                        exit = true;
                        continue;
                }
            }
            catch (Exception e)
            {
                Logger.Log($"{e.GetType()}: {e.Message}", LogType.Error);
            }
    }

    protected override void UpdateData()
    {
        base.UpdateData();
        _group = GetGroup(Data);
    }

    private void GiveEditor()
    {
        var counter = 0;
        Logger.Log($"Группа: {_group}");
        foreach (var user in _groupData.AllUsers.Where(user => !IsManager(user.Id)))
        {
            try
            {
                Framework.EditManager(user.Id, _group, ManagerRole.Editor);
                Logger.Log($"Выдали редактора {user}");
            }
            catch (VkApiException e)
            {
                Logger.Log($"Не удалось выдать редактора {user}: {e.Message}", LogType.Error);
                continue;
            }

            counter++;
        }

        Logger.Log("Процесс выдачи завершен, выдано редакторов: " + counter);
    }

    private bool IsManager(long user)
    {
        return _groupData.Managers.Any(x => x.Id == user);
    }

    private int GetGroup(DataModel data)
    {
        try
        {
            var group = (int) data.GroupList.Reverse().First();

            if (_processedGroups.Contains(group)) throw new InvalidOperationException();

            _processedGroups.Add(group);
            return group;
        }
        catch (InvalidOperationException)
        {
            Logger.Log("Все сообщества были обработаны…");
            Logger.Log("Пауза на 5 дней");
            _processedGroups.Clear();

            Thread.Sleep(TimeSpan.FromDays(5));

            Logger.Log("Проходимся по списку еще раз…");
            return GetGroup(data);
        }
    }
}
