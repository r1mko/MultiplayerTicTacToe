using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils;

public class SessionManager : Singleton<SessionManager>
{
    public bool AutoStart;

    private ISession activeSession;
    private CancellationTokenSource pollCts; //показ сессий и его остановка чтобы не было бесконечных циклов

    private ISession ActiveSession
    {
        get => activeSession;
        set
        {
            if (activeSession == value) return;

            StopPolling(); // Останавливаем предыдущий опрос

            activeSession = value;
            StartPolling().Forget();

            Debug.Log($"Active session: {activeSession}");
        }
    }

    private async UniTaskVoid StartPolling()
    {
        pollCts = new CancellationTokenSource();

        try
        {
            while (!pollCts.IsCancellationRequested && ActiveSession == null) //смотрим есть ли уже сессия и не остановлен ли
            {
                var sessions = await QuerySessions();
                if (sessions.Count > 0)
                {
                    UIManager.Singletone.SetSessionInfoText($"Доступно сессий: {sessions.Count}\nПрисоединяйтесь!");
                }
                else
                {
                    UIManager.Singletone.SetSessionInfoText("Нет активных сессий");
                }

                await UniTask.Delay(TimeSpan.FromSeconds(7), cancellationToken: pollCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Игнорируем — это ожидаемое поведение
        }
    }

    private void StopPolling()
    {
        if (pollCts != null)
        {
            pollCts.Cancel();
            pollCts.Dispose();
            pollCts = null;
        }
    }

    private void UpdateSessionUI()
    {
        if (ActiveSession != null)
        {
            if (ActiveSession.IsHost)
            {
                if (ActiveSession.Players.Count == 1)
                {
                    UIManager.Singletone.SetSessionInfoText("Создали сессию, ожидаем...");
                }
            }
        }
    }


    async void Start()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync(); // Инициализация Unity Gaming Services
            }
            else
            {
                Debug.Log("UnityServices уже инициализированы.");
            }

            // Проверяем, авторизован ли игрок.
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Анонимная авторизация игрока
                Debug.Log($"Sign in anonymously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");
            }

            // Начинаем опрос сессий сразу после авторизации
            ActiveSession = null; // явно указываем, что нет активной сессии
            StartPolling().Forget();

        }
        catch (Exception e)
        {
            await HandleError(e);
        }
    }



    /// <summary>
    /// Получает пользовательские свойства (например, имя игрока).
    /// </summary>
    public async UniTask<Dictionary<string, PlayerProperty>> GetPlayerProperties()
    {
        try
        {
            var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
            var playerNameProperty = new PlayerProperty(playerName, VisibilityPropertyOptions.Member);
            return new Dictionary<string, PlayerProperty> { { "player", playerNameProperty } };
        }
        catch (Exception e)
        {
            await HandleError(e);
            return new Dictionary<string, PlayerProperty>();
        }
    }



    public async UniTaskVoid StartSessionAsHost()
    {
        try
        {
            StopPolling();

            UIManager.Singletone.SetSessionInfoText("Создаём сессию...");

            var playerProperties = await GetPlayerProperties();
            var options = new SessionOptions
            {
                MaxPlayers = 2,
                IsLocked = false,
                IsPrivate = false,
                PlayerProperties = playerProperties,
                Name = $"{UnityEngine.Random.Range(0, 10000)}"
            }.WithRelayNetwork("europe-north1");

            ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            UpdateSessionUI();

            Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");
        }
        catch (Exception e)
        {
            await HandleError(e);
        }
    }

    public async UniTaskVoid JoinSessionById(string sessionId)
    {
        try
        {
            ActiveSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
            Debug.Log($"Session {ActiveSession.Id} joined! Properties.Count:{ActiveSession.Properties.Count}");

            // Если это клиент (не хост), запускаем цикл heartbeat
            if (!ActiveSession.IsHost)
            {
                HeartbeatLoop().Forget();
            }

        }
        catch (Exception e)
        {
            await HandleError(e);
        }
    }

    public async UniTaskVoid JoinSessionByCode(string sessionCode)
    {
        try
        {
            ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
            Debug.Log($"Session {ActiveSession.Id} joined!");
        }
        catch (Exception e)
        {
            await HandleError(e);
        }
    }

    public async UniTaskVoid KickPlayer(string playerId)
    {
        try
        {
            if (!ActiveSession.IsHost)
                return;

            await ActiveSession.AsHost().RemovePlayerAsync(playerId);
        }
        catch (Exception e)
        {
            await HandleError(e);
        }
    }

    public async UniTask<IList<ISessionInfo>> QuerySessions()
    {
        try
        {
            var sessionQueryOptions = new QuerySessionsOptions();
            QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(sessionQueryOptions);
            return results.Sessions;
        }
        catch (Exception e)
        {
            await HandleError(e);
            return new List<ISessionInfo>();
        }
    }

    public async UniTaskVoid LeaveSession()
    {
        if (ActiveSession != null)
        {
            try
            {
                await ActiveSession.LeaveAsync();
            }
            catch (Exception e)
            {
                // Ошибка при выходе из сессии не критична – просто логируем её
                Debug.LogWarning(e);
            }
            finally
            {
                ActiveSession = null;
            }
        }
    }

    public async UniTaskVoid FindAndJoinSession()
    {
        try
        {
            StopPolling();

            UIManager.Singletone.SetSessionInfoText("Подключаемся к сессии...");

            var sessions = await QuerySessions();
            if (sessions.Count > 0)
            {
                // Подключаемся к первой найденной сессии
                await JoinSessionById(sessions[0].Id).AsCompletedTask();
            }
            else
            {
                // Если сессий не найдено – создаём новую
                await StartSessionAsHost().AsCompletedTask();
            }
        }
        catch (Exception e)
        {
            await HandleError(e);
        }
    }

    public bool CanConnectionToSession()
    {
        return false;
    }

    [Button]
    public void FindAndJoinSessionButton()
    {
        FindAndJoinSession().Forget();
    }

    [Button]
    public void LeaveSessionButton()
    {
        LeaveSession().Forget();
    }

    [Button]
    public void StartSessionAsHostButton()
    {
        StartSessionAsHost().Forget();
    }

    public string Code;

    [Button]
    public void JoinSessionByCodeButton()
    {
        JoinSessionByCode(Code).Forget();
    }

    private async void OnDisable()
    {
        try
        {
            await LeaveSession().AsCompletedTask();
        }
        catch (Exception e)
        {
            await HandleError(e);
        }
    }

    /// <summary>
    /// Обрабатывает возникшие ошибки – выводит сообщение и переключает сцену на MENU.
    /// </summary>
    /// <param name="ex">Исключение, которое произошло.</param>
    public async UniTask HandleError(Exception ex)
    {
        Debug.LogWarning($"[SessionManager] Error occurred: {ex.Message}\n{ex.StackTrace}");
    }

    //HEATBEAT

    /// <summary>
    /// Циклически выполняет проверку подключения к серверу/хосту.
    /// </summary>
    private async UniTask HeartbeatLoop()
    {
        Debug.Log("Heartbeat запущен.");
        while (ActiveSession != null)
        {
            try
            {
                // Имитируем отправку heartbeat на сервер
                await PingServer();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Ошибка heartbeat: {ex.Message}");
                NotifyServerDisconnected();
                break;
            }
            // Интервал между проверками (например, 5 секунд)
            await UniTask.Delay(TimeSpan.FromSeconds(5));
        }
        Debug.Log("Heartbeat завершён.");
    }

    /// <summary>
    /// Имитация отправки запроса на сервер. Здесь можно реализовать реальную логику проверки.
    /// </summary>
    private async UniTask PingServer()
    {
        // Для демонстрации просто ждём 100 мс.
        await UniTask.Delay(100);

        // Если необходимо, можно добавить условие проверки, например:
        // if (ActiveSession == null || !ActiveSession.IsConnected)
        //     throw new Exception("Сервер не отвечает");
    }

    /// <summary>
    /// Уведомляет клиента о том, что сервер или хост отключились.
    /// </summary>
    private void NotifyServerDisconnected()
    {
        Debug.LogWarning("Сервер/Хост отключились. Возврат в меню.");
        // Здесь можно добавить дополнительную логику уведомления пользователя (например, показать UI-диалог)
    }
}
