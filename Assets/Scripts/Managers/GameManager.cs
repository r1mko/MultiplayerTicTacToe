using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Singltone;

    public int CurrentPlayerTurnID;
    private int startOffSet;

    public int TurnIndex;

    private bool isPlaying;
    public bool IsPlaying => isPlaying;

    private CellHistoryManager cellHistoryManager;

    private int[] wins = new int[] { 0, 0 };


    private void Awake()
    {
        Singltone = this;
        cellHistoryManager = new CellHistoryManager();
    }

    public void StartGame()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            NetworkPlayer.Singletone.StartGameRpc();
        }
        UpdateUI();
        PrepareGame();
        StartTimer();
    }

    public void UpdateCurrentPlayerID(int clientID)
    {
        Debug.Log($"[GameManager]2. Вызвали UpdateCurrentPlayerID {clientID}");
        CurrentPlayerTurnID = clientID;
        UIManager.Singletone.UpdateCurrentPlayerText();
        StartTimer();
    }
    public void NextTurn()
    {
        TurnIndex++;
    }

    public bool IsOurTurn()
    {
        Debug.Log($"Проверяем наш ли ход currentturnID {CurrentPlayerTurnID}");
        return CurrentPlayerTurnID == (int)NetworkPlayer.Singletone.NetworkManager.LocalClientId;
    }

    public void Restart()
    {
        TurnIndex = 0;
        BoardManager.Singltone.ClearAndUnbloackCells();
    }

    public void SetWin(int winnerID)
    {
        wins[winnerID]++;
        UIManager.Singletone.SetWinLoseCountText(wins);
    }

    public void PrepareGame(int? offSet = null)
    {
        TurnIndex = 0;
        cellHistoryManager.Clear();

        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            if (NetworkPlayer.Singletone.IsServer)
            {
                startOffSet = UnityEngine.Random.Range(0, NetworkManager.Singleton.ConnectedClientsIds.Count);
                NetworkPlayer.Singletone.UpdateOffSetRpc(startOffSet);
            }
            else
            {
                Debug.LogWarning("PrepareGame вызван на клиенте без offSet");
                return;
            }
        }
        else
        {
            startOffSet = UnityEngine.Random.Range(0, 2);
            UpdateOffSet(startOffSet);
        }

        isPlaying = true;
        BoardManager.Singltone.ClearAndUnbloackCells();
    }

    private void GameOver()
    {
        UIManager.Singletone.ShowRestartButton();
        BoardManager.Singltone.BlockAllButtons();
        isPlaying = false;
    }

    public void RestartGame()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            Debug.Log("Проверка мультиплеера");
            NetworkPlayer.Singletone.RestartGameRpc();
        }
        else
        {
            Restart();
            PrepareGame();
        }
    }

    public void StartTimer() //нужно будет разобраться с isourturn. в сингл игре не должен работать так
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            if (IsOurTurn())
            {
                return;
            }
        }
        else
        {
            if (!IsOurTurn())
            {
                return;
            }
        }

        TimerController.Singletone.EndTime();
        TimerController.Singletone.StartTime();
    }

    private void ServerSelectNextPlayer()
    {
        Debug.Log("[GameManager] Вызвали ServerSelectNextPlayer метод");
        var playersCount = 2;
        var currentPlayerIndex = (TurnIndex + startOffSet) % playersCount;
        UpdateCurrentPlayerID(currentPlayerIndex);
    }

    public void OnClick(int row, int col)
    {
        BoardManager.Singltone.FillCell(row, col, CurrentPlayerTurnID);
        NextTurn();
        cellHistoryManager.Add(BoardManager.Singltone.GetCell(row, col), CurrentPlayerTurnID);

        TimerController.Singletone.EndTime();

        if (BoardManager.Singltone.IsWon(row, col))
        {
            GameOver();
            SetWin(CurrentPlayerTurnID);
            UIManager.Singletone.SetWinText();
            return;
        }

        if (BoardManager.Singltone.IsGameDraw())
        {
            GameOver();
            UIManager.Singletone.SetDrawText("Ничья");
            return;
        }

        //if (IsServer)
        {
            ServerSelectNextPlayer();
        }
    }

    public void MoveToNextPlayer()
    {
        cellHistoryManager.SkipTurn(CurrentPlayerTurnID);
        NextTurn();

        //if (IsServer)
        {
            ServerSelectNextPlayer();
        }
    }

    public void UpdateOffSet(int clientID)
    {
        Debug.Log($"[GameManager]1. Вызвали UpdateOffSet {clientID}");
        startOffSet = clientID;
        UpdateCurrentPlayerID(clientID);
        UIManager.Singletone.HideRestartButton();
    }

    public void UpdateUI()
    {
        Debug.Log("[GameManager] Вызываем обновление меню");
        UIManager.Singletone.HideActiveSessionInfo();
        UIManager.Singletone.ShowMoveInfo();
        UIManager.Singletone.ShowWinLoseCountInfo();
        UIManager.Singletone.ShowSmileScreen();
        BoardManager.Singltone.ClearAndUnbloackCells();
    }

}
