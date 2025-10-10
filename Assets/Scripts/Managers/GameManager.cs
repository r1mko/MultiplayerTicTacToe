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
    private const int ArrowCount = 3;
    private bool isPlaying;
    private bool isBlocking;
    private string lastSlideButtonText;
    private string lastShotButtonText;
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
        UpdateShotButtonText();
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

        if (SkillCooldownManager.ShouldRemoveSlideCooldown(TurnIndex, CurrentPlayerTurnID))
        {
            SkillCooldownManager.RemoveSlideCooldown(); 
            UIManager.Singletone.UnblockSlideButton();
            Debug.Log("[Slide] Кулдаун завершён. Кнопка разблокирована.");
        }

        // Выстрел ← ДОБАВЬ ЭТО
        if (SkillCooldownManager.ShouldRemoveShotCooldown(TurnIndex, CurrentPlayerTurnID))
        {
            SkillCooldownManager.RemoveShotCooldown();
            UIManager.Singletone.UnblockShotButton();
            Debug.Log("[Shot] Кулдаун завершён. Кнопка выстрела разблокирована.");
        }

        UpdateSlideButtonState();
        UpdateSlideButtonText();
        UpdateShotButtonText();
    }

    private void UpdateSlideButtonState()
    {
        if (IsBlocking)
        {
            UIManager.Singletone.BlockSlideButton();
            UIManager.Singletone.BlockShotButton();
            StartCoroutine(WaitForBlockingEnd());
            return;
        }

        CheckSlideButton();
        CheckShotButton();
    }

    private IEnumerator WaitForBlockingEnd()
    {
        yield return new WaitUntil(() => !IsBlocking);
        CheckSlideButton();
        CheckShotButton(); // ← ДОБАВЬ
    }

    private void CheckShotButton()
    {

        if (IsOurTurn())
        {
            if (!SkillCooldownManager.IsShotOnCooldown())
            {
                UIManager.Singletone.UnblockShotButton();
            }
            else
            {
                UIManager.Singletone.BlockShotButton();
            }
        }
        else
        {
            UIManager.Singletone.BlockShotButton();
        }
    }

    private void CheckSlideButton()
    {

        if (IsOurTurn())
        {
            if (!SkillCooldownManager.IsSlideOnCooldown())
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
        if (!IsOurTurn()) return;
        if (SkillCooldownManager.IsShotOnCooldown())
        {
            Debug.Log("Выстрел на кулдауне!");
            return;
        }

        StartCoroutine(ShootThreeArrowsAndFill());
    }
    private void UpdateShotButtonText()
    {
        string newText = SkillCooldownManager.GetShotButtonText(TurnIndex);
        if (newText != lastShotButtonText)
        {
            UIManager.Singletone.SetShotCooldownText(newText);
            lastShotButtonText = newText;
        }
    }

    private IEnumerator ShootThreeArrowsAndFill()
    {
        if (!IsOurTurn()) yield break;
        if (SkillCooldownManager.IsShotOnCooldown()) yield break;

        // ← ДОБАВЬ ЭТО:
        SkillCooldownManager.OnShotUsed(TurnIndex);
        UIManager.Singletone.BlockShotButton();
        UpdateShotButtonText();

        int shooterID = CurrentPlayerTurnID;
        int opponentID = 1 - shooterID;

        // === ШАГ 1: УДАЛЯЕМ ВСЕ СВОИ ФИШКИ С ДОСКИ ===
        List<Cell> myCellsToRemove = new List<Cell>();
        foreach (var cell in BoardManager.Singltone.GetAllCells())
        {
            if (cell.IsFillCell && cell.IndexPlayer == shooterID)
            {
                myCellsToRemove.Add(cell);
            }
        }

        foreach (var cell in myCellsToRemove)
        {
            cellHistoryManager.RemoveMoveFromPlayer(cell, shooterID);
            cell.Clear();
            Debug.Log($"[Выстрел] Удалена моя фишка в ({cell.row}, {cell.coll}) перед выстрелом");
        }

        // === ШАГ 2: ВЫБИРАЕМ 3 СЛУЧАЙНЫЕ ЯЧЕЙКИ ===
        List<Cell> allCells = new List<Cell>(BoardManager.Singltone.GetAllCells());
        if (allCells.Count == 0) yield break;

        ShuffleList(allCells);
        int count = Mathf.Min(ArrowCount, allCells.Count);
        List<Cell> targets = allCells.GetRange(0, count);

        // === ШАГ 3: АНИМАЦИЯ + МГНОВЕННАЯ ОЧИСТКА И УСТАНОВКА ===
        foreach (Cell target in targets)
        {
            // Анимация выстрела
            yield return StartCoroutine(AnimateSingleArrow(target));

            // Очищаем ячейку, если там что-то есть (даже если это наша — но её уже не должно быть)
            if (target.IsFillCell)
            {
                int oldOwner = target.IndexPlayer;
                cellHistoryManager.RemoveMoveFromPlayer(target, oldOwner);
                target.Clear();
                Debug.Log($"[Выстрел] Уничтожена фишка игрока {oldOwner} в ({target.row}, {target.coll})");
            }

            // Сразу ставим свою фишку
            BoardManager.Singltone.FillCell(target.row, target.coll, shooterID);
            cellHistoryManager.AddMove(target, shooterID);
            Debug.Log($"[Выстрел] Установлена моя фишка в ({target.row}, {target.coll})");
        }

        // === ШАГ 4: ПРОВЕРКА РЯДА ТОЛЬКО В КОНЦЕ ===
        bool hasRow = false;
        foreach (Cell cell in targets)
        {
            if (BoardManager.Singltone.IsRow(cell.row, cell.coll))
            {
                hasRow = true;
                break;
            }
        }

        // === ШАГ 5: ОБРАБОТКА РЕЗУЛЬТАТА ===
        if (hasRow)
        {
            if (NetworkPlayer.Singletone.IsMultiplayer())
            {
                if (NetworkPlayer.Singletone.IsServer)
                {
                    NetworkPlayer.Singletone.TriggerMultipleDamageRpc(new int[] { opponentID });
                }
            }
            else
            {
                hPHistoryManager.Damage(opponentID);
                SetPlayersHP();

                if (hPHistoryManager.LosePlayer(opponentID))
                {
                    GameOver();
                    SetWin(shooterID);
                    UIManager.Singletone.SetWinText();
                    yield break;
                }
                else
                {
                    StartCoroutine(DamageDelay());
                    ChangeTurnIndex();
                    PassMoveToNextPlayer();
                    yield break;
                }
            }
        }

        // === ШАГ 6: ПРОВЕРКА НИЧЬЕЙ ===
        if (BoardManager.Singltone.IsGameDraw())
        {
            GameOver();
            UIManager.Singletone.SetDrawText("Ничья");
            yield break;
        }

        // === ШАГ 7: ПЕРЕДАЧА ХОДА ===
        ChangeTurnIndex();
        PassMoveToNextPlayer();
    }
    private IEnumerator AnimateSingleArrow(Cell targetCell)
    {
        RectTransform canvasRect = Canvas.GetComponent<RectTransform>();
        Vector2 startPosition = new Vector2(0, -canvasRect.rect.height / 2 - 50);
        Vector2 endPosition = BoardManager.Singltone.GetCellScreenPosition(targetCell.row, targetCell.coll);

        GameObject shot = new GameObject("Shot");
        Image image = shot.AddComponent<Image>();
        image.color = new Color(1f, 0.2f, 0.3f);
        RectTransform rectTransform = shot.GetComponent<RectTransform>();
        rectTransform.SetParent(Canvas.transform, false);
        rectTransform.sizeDelta = new Vector2(40, 40);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = startPosition;

        float duration = 0.6f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        Destroy(shot);

        Debug.Log($"[Выстрел - Анимация] Попали в ячейку ({targetCell.row}, {targetCell.coll}). Заполнена: {targetCell.IsFillCell}");
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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