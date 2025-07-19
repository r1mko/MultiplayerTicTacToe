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
    public bool IsPlaying=>isPlaying;

    private CellHistoryManager cellHistoryManager;

    private int[] wins = new int[]{0,0};


    private void Awake()
    {
        Singltone = this;
        cellHistoryManager = new CellHistoryManager();
    }

    public void StartGame()
    {
        UpdateUI();
        PrepareGame();
        StartTimer();
    }

    public void UpdateCurrentPlayerID(int clientID)
    {
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
        var randomIndex = UnityEngine.Random.Range(0, 2);
        UpdateOffSet(randomIndex);
        cellHistoryManager.Clear();
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
        BoardManager.Singltone.ClearAndUnbloackCells();
        Restart();
        //if (IsServer)
        {
            PrepareGame();
        }
    }

    public void StartTimer()
    {
        if (IsOurTurn())
        {
            return;
        }
        TimerController.Singltone.EndTime();
        TimerController.Singltone.StartTime();
    }

    private void ServerSelectNextPlayer()
    {
        var playersCount = 2;
        var currentPlayerIndex = (GameManager.Singltone.TurnIndex + startOffSet) % playersCount;
        UpdateCurrentPlayerID(currentPlayerIndex);
    }

    public void OnClick(int row, int col)
    {
        BoardManager.Singltone.FillCell(row, col, CurrentPlayerTurnID);
        NextTurn();
        cellHistoryManager.Add(BoardManager.Singltone.GetCell(row, col), CurrentPlayerTurnID);

        TimerController.Singltone.EndTime();

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
        GameManager.Singltone.NextTurn();

        //if (IsServer)
        {
            ServerSelectNextPlayer();
        }
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
        BoardManager.Singltone.ClearAndUnbloackCells();
    }

}
