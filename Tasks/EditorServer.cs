using nng.Constants;
using nng.Data;
using nng.Exceptions;
using nng.Models;
using nng.VkFrameworks;
using nng_server.Configs;
using nng_server.Logging;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;

namespace nng_server.Tasks;

public class EditorServer : ServerTask
{
    private readonly Config _config;
    private readonly LogContext _logger;
    private readonly List<int> _processedGroups;
    private readonly VkFramework _vkFramework;
    private int _group;
    private GroupData _groupData;

    public EditorServer(VkFramework vkFramework, Func<bool> finished) : base(finished)
    {
        _processedGroups = new List<int>();
        _vkFramework = vkFramework;
        _logger = new LogContext(nameof(EditorServer));

        _config = ConfigurationManager.Configuration;
        Data = DataHelper.GetData(_config.DataUrl);
        _group = GetGroup(Data);
    }

    public override void Start()
    {
        Data = DataHelper.GetData(_config.DataUrl);
        var exit = false;
        while (!exit)
            try
            {
                _groupData = _vkFramework.GetGroupData(_group);
                if (_groupData.Managers.Count >= _groupData.AllUsers.Count)
                {
                    _logger.Log($"Все пользователи в группе {_group} менеджеры");
                    exit = true;
                    continue;
                }

                switch (_groupData.AllUsers.Count)
                {
                    case < 54:
                        _logger.Log($"В группе {_group} меньше 54 человек ({_groupData.AllUsers.Count}), " +
                                    $"ждем {_config.UpdateTimeInMinutes} минут…");
                        Thread.Sleep(_config.UpdateTimeInMinutes * 60 * 1000);
                        continue;
                    case < 100:
                        _logger.Log($"Начинаем выдачу в группе {_group}…");
                        GiveEditor();
                        exit = true;
                        continue;
                    case >= 100:
                        _logger.Log($"В группе {_group} больше 100 человек, процесс завершен");
                        exit = true;
                        continue;
                }
            }
            catch (Exception e)
            {
                _logger.Log($"{e.GetType()}: {e.Message}", LogType.Error);
            }

        Finished();
    }

    protected override void UpdateData()
    {
        base.UpdateData();
        _group = GetGroup(Data);
    }

    private void GiveEditor()
    {
        var counter = 0;
        _logger.Log($"Группа: {_group}");
        for (var index = 0; index < _groupData.AllUsers.Count; index++)
        {
            var user = _groupData.AllUsers[index];
            if (IsManager(user.Id)) continue;
            try
            {
#pragma warning disable CS0618
                _vkFramework.EditManagerLegacy(user.Id, _group, ManagerRole.Editor);
#pragma warning restore CS0618
                _logger.Log($"Выдали редактора {user}");
            }
            catch (CaptchaNeededException)
            {
                _logger.Log($"Каптча! Ожидаем {Constants.CaptchaEditorWaitTime.TotalMinutes} минут",
                    LogType.Warning);
                Thread.Sleep(Constants.CaptchaEditorWaitTime);
                index--;
            }
            catch (VkFrameworkMethodException e)
            {
                _logger.Log($"Не удалось выдать редактора {user}: {e.Message}", LogType.Error);
                continue;
            }

            counter++;
        }

        _logger.Log("Процесс выдачи завершен, выдано редакторов: " + counter);
        Finished();
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
            _logger.Log("Все сообщества были обработаны…");
            _logger.Log("Пауза на 5 дней");
            _processedGroups.Clear();

            Thread.Sleep(TimeSpan.FromDays(5));

            _logger.Log("Проходимся по списку еще раз…");
            return GetGroup(data);
        }
    }
}
