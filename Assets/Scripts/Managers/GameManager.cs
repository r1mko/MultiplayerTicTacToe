using System;
using System.Collections;
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
    private bool isBlocking;
    public bool IsPlaying => isPlaying;
    public bool IsBlocking => isBlocking;
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
        UpdateUI();
        PrepareGame();
        StartTimer();
    }
    private void SetPlayersHP()
    {

        int playerHP = hPHistoryManager.GetHP(0); // игрок (человек)
        int opponentHP = hPHistoryManager.GetHP(1); // бот
        UIManager.Singletone.SetPlayersHP(playerHP, opponentHP);

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
        return CurrentPlayerTurnID == 0;
        //return CurrentPlayerTurnID == (int)NetworkPlayer.Singletone.NetworkManager.LocalClientId;
    }

    public bool IsBotTurn()
    {
        // В одиночной игре: бот — всегда игрок 1
        return CurrentPlayerTurnID == 1;
    }

    public void Restart()
    {
        TurnIndex = 0;
        SetPlayersHP();
        UIManager.Singletone.ShowHPBar();
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
        hPHistoryManager.ResetPlayersHP();
        SetPlayersHP();

        startOffSet = UnityEngine.Random.Range(0, 2);
        UpdateOffSet(startOffSet);
        BoardManager.Singltone.ClearAndUnbloackCells();

        isPlaying = true;
    }

    private void GameOver()
    {
        UIManager.Singletone.HideHPBar();
        UIManager.Singletone.ShowRestartButton();
        BoardManager.Singltone.BlockAllButtons();
        isPlaying = false;
    }

    public void RestartGame()
    {

        hPHistoryManager.ResetPlayersHP();
        Restart();
        PrepareGame();
        MinmaxBot.Singletone.ResetBotMoveCount();

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
        BoardManager.Singltone.FillCell(row, col, CurrentPlayerTurnID);
        NextTurn();
        cellHistoryManager.Add(BoardManager.Singltone.GetCell(row, col), CurrentPlayerTurnID);

        TimerController.Singletone.EndTime();

        if (BoardManager.Singltone.IsRow(row, col))
        {
            int playerID = CurrentPlayerTurnID;
            int opponentID = 1 - playerID;
            hPHistoryManager.Damage(opponentID);
            SetPlayersHP();
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
                StartCoroutine(DamageDelay());
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

    private IEnumerator DamageDelay()
    {
        isBlocking = true;
        BoardManager.Singltone.BlockAllButtons();
        yield return new WaitForSeconds(3f); //поменять задержку чтобы бот не мог ходить
        isBlocking = false;
        cellHistoryManager.Clear();
        BoardManager.Singltone.ClearAndUnbloackCells();
    }
    public void PlayerSkipMove()
    {
        HandleSkipTurn();
        PassMoveToNextPlayer();
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
        UIManager.Singletone.ShowMoveInfo();
        UIManager.Singletone.ShowWinLoseCountInfo();
        UIManager.Singletone.ShowHPBar();
    }

}