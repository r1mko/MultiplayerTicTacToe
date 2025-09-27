using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Singletone;

    public int CurrentPlayerTurnID;
    public int TurnIndex;
    private int startOffSet;
    private int slideCooldownTurnIndex;
    private bool isSlideOnCooldown;
    private bool isPlaying;
    private bool isBlocking;
    public bool IsPlaying => isPlaying;
    public bool IsBlocking => isBlocking;

    private HPHistoryManager hPHistoryManager;

    public CellHistoryManager cellHistoryManager;
    public CellHistoryManager CellHistoryManager => cellHistoryManager;
    public HPHistoryManager HPHistoryManager => hPHistoryManager;

    public SkillCooldownManager SkillCooldownManager { get; private set; }


    private int[] wins = new int[] { 0, 0 };


    private void Awake()
    {
        Singletone = this;
        cellHistoryManager = new CellHistoryManager();
        hPHistoryManager = new HPHistoryManager();
        SkillCooldownManager = new SkillCooldownManager();
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
        }

        StartTimer();
    }
    private void SetPlayersHP()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            var opId = NetworkManager.Singleton.LocalClientId == 0 ? 1 : 0;
            var playerHP = hPHistoryManager.GetHP((int)NetworkManager.Singleton.LocalClientId);
            int opponentHP = hPHistoryManager.GetHP(opId);
            UIManager.Singletone.SetPlayersHP(playerHP, opponentHP);
        }
        else
        {
            int playerHP = hPHistoryManager.GetHP(0); // игрок (человек)
            int opponentHP = hPHistoryManager.GetHP(1); // бот
            UIManager.Singletone.SetPlayersHP(playerHP, opponentHP);
        }
    }
    public void UpdateCurrentPlayerID(int clientID)
    {
        CurrentPlayerTurnID = clientID;
        UIManager.Singletone.UpdateCurrentPlayerText();
        StartTimer();
    }

    public void ChangeTurnIndex(int index = 1)
    {
        TurnIndex += index;
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
        SkillCooldownManager.Initialize(startOffSet);
        SkillCooldownManager.OnSlideUsed(TurnIndex);
        hPHistoryManager.ResetPlayersHP();
        SetPlayersHP();

        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            if (NetworkPlayer.Singletone.IsServer)
            {
                startOffSet = Random.Range(0, NetworkManager.Singleton.ConnectedClientsIds.Count);
                NetworkPlayer.Singletone.UpdateOffSetRpc(startOffSet);
            }
        }
        else
        {
            startOffSet = Random.Range(0, 2);
            UpdateOffSet(startOffSet);
            BoardManager.Singltone.ClearAndUnbloackCells();
        }

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
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            NetworkPlayer.Singletone.RestartGameRpc();
        }
        else
        {
            hPHistoryManager.ResetPlayersHP();
            SkillCooldownManager.Reset();
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

        if (SkillCooldownManager.ShouldRemoveCooldown(TurnIndex, CurrentPlayerTurnID))
        {
            SkillCooldownManager.RemoveCooldown();
            UIManager.Singletone.UnblockSlideButton();
            Debug.Log("[Slide] Кулдаун завершён. Кнопка разблокирована.");
        }
    }


    public void OnClick(int row, int col)
    {
        BoardManager.Singltone.FillCell(row, col, CurrentPlayerTurnID);
        ChangeTurnIndex();
        cellHistoryManager.AddMove(BoardManager.Singltone.GetCell(row, col), CurrentPlayerTurnID);

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

    public void ApplySlideGravity()
    {
        BoardManager.Singltone.ApplyGravity();
        UIManager.Singletone.BlockSlideButton();
        SkillCooldownManager.OnSlideUsed(TurnIndex);
    }

    public void HandleSkipTurn()
    {
        cellHistoryManager.SkipTurn(CurrentPlayerTurnID);
        ChangeTurnIndex();
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
        UIManager.Singletone.ShowHPBar();
        UIManager.Singletone.ShowSlideButton();
    }

}