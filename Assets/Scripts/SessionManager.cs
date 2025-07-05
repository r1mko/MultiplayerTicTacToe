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
    private CancellationTokenSource pollCts; //����� ������ � ��� ��������� ����� �� ���� ����������� ������

    private ISession ActiveSession
    {
        get => activeSession;
        set
        {
            if (activeSession == value) return;

            StopPolling(); // ������������� ���������� �����

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
            while (!pollCts.IsCancellationRequested)
            {
                var sessionStatusText = UIManager.Singletone.sessionInfoText;
                var sessions = await QuerySessions();

                if (sessions.Count > 0)
                {
                    sessionStatusText.text = $"�������� ������: {sessions.Count}\n���������������!";
                }
                else
                {
                    sessionStatusText.text = "��� �������� ������";
                }

                await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: pollCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // ���������� � ��� ��������� ���������
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



    async void Start()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync(); // ������������� Unity Gaming Services
            }
            else
            {
                Debug.Log("UnityServices ��� ����������������.");
            }

            // ���������, ����������� �� �����.
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // ��������� ����������� ������
                Debug.Log($"Sign in anonymously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");
            }

            // �������� ����� ������ ����� ����� �����������
            ActiveSession = null; // ���� ���������, ��� ��� �������� ������
            StartPolling().Forget();

        }
        catch (Exception e)
        {
            await HandleError(e);
        }
    }



    /// <summary>
    /// �������� ���������������� �������� (��������, ��� ������).
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
            var playerProperties = await GetPlayerProperties();

            var options = new SessionOptions
            {
                MaxPlayers = 2,
                IsLocked = false,
                IsPrivate = false,
                PlayerProperties = playerProperties,
                Name = $"{UnityEngine.Random.Range(0, 10000)}"
            }.WithRelayNetwork("europe-north1"); // ��� WithDistributedAuthorityNetwork() ��� �������������� ����������

            ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code} Properties.Count:{ActiveSession.Properties.Count}");


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

            // ���� ��� ������ (�� ����), ��������� ���� heartbeat
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
                // ������ ��� ������ �� ������ �� �������� � ������ �������� �
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
            var sessions = await QuerySessions();
            if (sessions.Count > 0)
            {
                // ������������ � ������ ��������� ������
                await JoinSessionById(sessions[0].Id).AsCompletedTask();
            }
            else
            {
                // ���� ������ �� ������� � ������ �����
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

    //public async UniTaskVoid FindSessionOrStartWithBot()
    //{
    //    try
    //    {
    //        var sessions = await QuerySessions();
    //        var possibleSessions = new List<ISessionInfo>();


    //        if (possibleSessions.Count > 0)
    //        {
    //            var targetSession = possibleSessions[UnityEngine.Random.Range(0, possibleSessions.Count)];
    //            await JoinSessionById(targetSession.Id).AsCompletedTask();
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        await HandleError(e);
    //    }
    //}

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
    /// ������������ ��������� ������ � ������� ��������� � ����������� ����� �� MENU.
    /// </summary>
    /// <param name="ex">����������, ������� ���������.</param>
    public async UniTask HandleError(Exception ex)
    {
        Debug.LogWarning($"[SessionManager] Error occurred: {ex.Message}\n{ex.StackTrace}");
    }

    //HEATBEAT

    /// <summary>
    /// ���������� ��������� �������� ����������� � �������/�����.
    /// </summary>
    private async UniTask HeartbeatLoop()
    {
        Debug.Log("Heartbeat �������.");
        while (ActiveSession != null)
        {
            try
            {
                // ��������� �������� heartbeat �� ������
                await PingServer();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"������ heartbeat: {ex.Message}");
                NotifyServerDisconnected();
                break;
            }
            // �������� ����� ���������� (��������, 5 ������)
            await UniTask.Delay(TimeSpan.FromSeconds(5));
        }
        Debug.Log("Heartbeat ��������.");
    }

    /// <summary>
    /// �������� �������� ������� �� ������. ����� ����� ����������� �������� ������ ��������.
    /// </summary>
    private async UniTask PingServer()
    {
        // ��� ������������ ������ ��� 100 ��.
        await UniTask.Delay(100);

        // ���� ����������, ����� �������� ������� ��������, ��������:
        // if (ActiveSession == null || !ActiveSession.IsConnected)
        //     throw new Exception("������ �� ��������");
    }

    /// <summary>
    /// ���������� ������� � ���, ��� ������ ��� ���� �����������.
    /// </summary>
    private void NotifyServerDisconnected()
    {
        Debug.LogWarning("������/���� �����������. ������� � ����.");
        // ����� ����� �������� �������������� ������ ����������� ������������ (��������, �������� UI-������)
    }
}
