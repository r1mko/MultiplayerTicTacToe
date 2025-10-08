using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Singletone;

    public int CurrentPlayerTurnID;
    public int TurnIndex;
    public Canvas Canvas;

    private int startOffSet;

    private bool isPlaying;
    private bool isBlocking;

    private string lastSlideButtonText;
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
        UpdateSlideButtonState();
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

        SkillCooldownManager.Initialize(startOffSet);
        UpdateSlideButtonText();
        isPlaying = true;

        UpdateSlideButtonState();
    }

    private void GameOver()
    {
        TimerController.Singletone.EndTime();
        BoardManager.Singltone.BlockAllButtons();
        UIManager.Singletone.HideHPBar();
        UIManager.Singletone.ShowRestartButton();
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

        UpdateSlideButtonState();
        UpdateSlideButtonText();

    }

    private void UpdateSlideButtonState()
    {
        if (IsBlocking)
        {
            UIManager.Singletone.BlockSlideButton();
            StartCoroutine(WaitForBlockingEnd());
            return;
        }

        CheckSlideButton();
    }

    private IEnumerator WaitForBlockingEnd()
    {
        yield return new WaitUntil(() => !IsBlocking);
        CheckSlideButton();


    }

    private void CheckSlideButton()
    {
        if (IsOurTurn())
        {
            if (!SkillCooldownManager.IsOnCooldown())
            {
                UIManager.Singletone.UnblockSlideButton();
            }
        }
        else
        {
            UIManager.Singletone.BlockSlideButton();
        }
    }

    private void UpdateSlideButtonText()
    {
        string newText = SkillCooldownManager.GetSlideButtonText(TurnIndex);

        if (newText != lastSlideButtonText)
        {
            UIManager.Singletone.SetCooldownText(newText);
            lastSlideButtonText = newText;
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

    public void ApplyMultipleDamages(List<int> victimPlayerIDs)
    {
        if (victimPlayerIDs == null || victimPlayerIDs.Count == 0)
        {
            Debug.LogError("PlayerIDs is null or not found");
            return;
        }

        Debug.Log($"[ApplyMultipleDamages] Наносим урон {victimPlayerIDs.Count} игрокам: [{string.Join(", ", victimPlayerIDs)}]");

        var uniqueVictims = new HashSet<int>(victimPlayerIDs);

        foreach (int playerId in uniqueVictims)
        {
            hPHistoryManager.Damage(playerId);
            Debug.Log($"У игрока с айди {playerId} осталось хп: {hPHistoryManager.GetHP(playerId)}");
        }

        SetPlayersHP();

        bool player0Lost = hPHistoryManager.LosePlayer(0);
        bool player1Lost = hPHistoryManager.LosePlayer(1);

        if (player0Lost && player1Lost)
        {
            Debug.Log("Оба игрока проиграли! Ничья.");
            GameOver();
            UIManager.Singletone.SetDrawText("Ничья");
        }
        else if (player0Lost)
        {
            Debug.Log("Игрок 0 проиграл! Игрок 1 победил.");
            GameOver();
            SetWin(1);
            UIManager.Singletone.SetWinText();
        }
        else if (player1Lost)
        {
            Debug.Log("Игрок 1 проиграл! Игрок 0 победил.");
            GameOver();
            SetWin(0);
            UIManager.Singletone.SetWinText();
        }
        else
        {
            StartCoroutine(DamageDelay());
        }
    }

    private IEnumerator DamageDelay()
    {
        isBlocking = true;
        BoardManager.Singltone.BlockAllButtons();
        UIManager.Singletone.BlockSlideButton();
        yield return new WaitForSeconds(3f); //поменять задержку чтобы бот не мог ходить
        UpdateSlideButtonState();
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

    public void ApplyShot()
    {
        if (!IsOurTurn()) return; // Только наш ход

        StartCoroutine(ShootEffect());
    }

    private IEnumerator ShootEffect()
    {
        RectTransform canvasRect = Canvas.GetComponent<RectTransform>();

        // Старт: снизу по центру
        Vector2 startPosition = new Vector2(0, -canvasRect.rect.height / 2 - 50);

        // Выбираем случайную ячейку через BoardManager
        List<Cell> allCells = BoardManager.Singltone.GetAllCells();
        if (allCells.Count == 0)
        {
            Debug.LogError("Нет ячеек на доске!");
            yield break;
        }

        Cell targetCell = allCells[Random.Range(0, allCells.Count)];
        int targetRow = targetCell.row;
        int targetCol = targetCell.coll;

        Vector2 endPosition = BoardManager.Singltone.GetCellScreenPosition(targetRow, targetCol);

        Debug.Log($"[ApplyShot] Летим в ячейку [{targetRow}, {targetCol}]");

        // Создаём объект выстрела
        GameObject shot = new GameObject("Shot");
        Image image = shot.AddComponent<Image>();
        image.color = new Color(1f, 0.2f, 0.3f); // Яркий цвет

        RectTransform rectTransform = shot.GetComponent<RectTransform>();
        rectTransform.SetParent(Canvas.transform, false);
        rectTransform.sizeDelta = new Vector2(40, 40);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = startPosition;

        // Анимация полёта
        float duration = 0.6f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        // === ПОПАДАНИЕ ===
        Destroy(shot); // Удаляем снаряд

        // Просто очищаем ячейку
        targetCell.Clear();

        cellHistoryManager.RemoveMoveFromAnyPlayer(targetCell);
    }

    public void ApplySlideGravity()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            NetworkPlayer.Singletone.ApplyGravitySlideRpc();
        }
        else
        {
            SlideGravity();
        }

        UIManager.Singletone.BlockSlideButton();
        SkillCooldownManager.OnSlideUsed(TurnIndex);
        UpdateSlideButtonText();
    }

    public void SlideGravity()
    {
        BoardManager.Singltone.ApplyGravity();

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
        UIManager.Singletone.ShowShotButton();
    }

}