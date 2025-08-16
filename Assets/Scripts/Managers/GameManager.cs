using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Singletone;

    public int CurrentPlayerTurnID;
    public int TurnIndex;
    private int startOffSet;
    private bool isPlaying;
    public bool IsPlaying => isPlaying;
    private HPHistoryManager hPHistoryManager;
    private CellHistoryManager cellHistoryManager;
    public CellHistoryManager CellHistoryManager => cellHistoryManager;
    public HPHistoryManager HPHistoryManager => hPHistoryManager;


    private int[] wins = new int[] { 0, 0 };


    private void Awake()
    {
        Singletone = this;
        cellHistoryManager = new CellHistoryManager();
        hPHistoryManager = new HPHistoryManager();
    }

    public void StartGame()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            NetworkPlayer.Singletone.StartGameRpc();
        }
        else
        {
            UpdateUI();
            PrepareGame();
            hPHistoryManager.ResetPlayersHP();
        }

        StartTimer();
    }

    public void UpdateCurrentPlayerID(int clientID)
    {
        //Debug.Log($"[GameManager] Вызвали UpdateCurrentPlayerID {clientID}");
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
        return CurrentPlayerTurnID == (int)NetworkPlayer.Singletone.NetworkManager.LocalClientId;
    }

    public bool IsBotTurn()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
            return !IsOurTurn();

        // В одиночной игре: бот — всегда игрок 1
        return CurrentPlayerTurnID == 1;
    }

    public void Restart()
    {
        TurnIndex = 0;
    }

    public void SetWin(int winnerID)
    {
        wins[winnerID]++;
        UIManager.Singletone.SetWinLoseCountText(wins);
    }

    public void PrepareGame()
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
        }
        else
        {
            startOffSet = UnityEngine.Random.Range(0, 2);
            UpdateOffSet(startOffSet);
            BoardManager.Singltone.ClearAndUnbloackCells();
        }

        isPlaying = true;

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
            NetworkPlayer.Singletone.RestartGameRpc();
        }
        else
        {
            hPHistoryManager.ResetPlayersHP();
            Restart();
            PrepareGame();
            MinmaxBot.Singletone.ResetBotMoveCount();
        }
    }

    public void StartTimer()
    {
        if (!IsOurTurn())
        {
            return;
        }

        TimerController.Singletone.StartTime();
    }

    public void PassMoveToNextPlayer()
    {
        var playersCount = 2;
        var currentPlayerIndex = (TurnIndex + startOffSet) % playersCount;
        UpdateCurrentPlayerID(currentPlayerIndex);
    }

    public void OnClick(int row, int col)
    {
        Debug.Log($"<color=green>Turn index равен {TurnIndex}</color>");
        BoardManager.Singltone.FillCell(row, col, CurrentPlayerTurnID);
        NextTurn();
        cellHistoryManager.Add(BoardManager.Singltone.GetCell(row, col), CurrentPlayerTurnID);

        TimerController.Singletone.EndTime();

        if (BoardManager.Singltone.IsRow(row, col))
        {
            int playerID = CurrentPlayerTurnID;
            int opponentID = 1 - playerID;
            hPHistoryManager.Damage(opponentID);
            if (hPHistoryManager.LosePlayer(opponentID))
            {
                Debug.Log($"Игрок с айди {opponentID} умер");
                GameOver();
                SetWin(CurrentPlayerTurnID);
                UIManager.Singletone.SetWinText();
                return;
            }
            else
            {
                cellHistoryManager.Clear();
                BoardManager.Singltone.ClearAndUnbloackCells();
            }
        }

        if (BoardManager.Singltone.IsGameDraw())
        {
            GameOver();
            UIManager.Singletone.SetDrawText("Ничья");
            return;
        }

        PassMoveToNextPlayer();
    }

    public void PlayerSkipMove()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            NetworkPlayer.Singletone.MoveToNextPlayerRpc();
        }
        else
        {
            HandleSkipTurn();
            PassMoveToNextPlayer();
        }
    }

    public void HandleSkipTurn()
    {
        cellHistoryManager.SkipTurn(CurrentPlayerTurnID);
        NextTurn();
    }

    public void UpdateOffSet(int clientID)
    {
        startOffSet = clientID;
        UpdateCurrentPlayerID(clientID);
        UIManager.Singletone.HideRestartButton();
    }

    public void UpdateUI()
    {
        UIManager.Singletone.HideActiveSessionInfo();
        UIManager.Singletone.ShowMoveInfo();
        UIManager.Singletone.ShowWinLoseCountInfo();
        UIManager.Singletone.ShowSmileScreen();
        //Debug.Log($"[GameManager] Вызвали UpdateUI");
    }

}