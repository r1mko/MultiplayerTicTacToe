using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static BoardManager;

public class GameManager : MonoBehaviour
{
    // =============== SINGLETON & STATE ===============
    public static GameManager Singletone;
    public bool IsPlaying => isPlaying;
    public bool IsBlocking => isBlocking;
    private bool isBlocking;
    private bool isPlaying;

    // =============== ANIMATION TRACKING ===============
    private int activeAnimations = 0;
    public bool IsAnimating => activeAnimations > 0;

    public void BeginAnimation()
    {
        activeAnimations++;
    }

    public void EndAnimation()
    {
        activeAnimations = Mathf.Max(0, activeAnimations - 1);
    }

    // =============== GAMEPLAY CORE STATE ===============
    public int CurrentPlayerTurnID;
    public int TurnIndex;
    public Canvas Canvas;

    private int startOffSet;
    private const int ArrowCount = 3;
    private int[] wins = new int[] { 0, 0 };

    // =============== EFFECTS ===============
    [SerializeField] private GameObject winLinePrefab;
    [SerializeField] private GameObject shotPrefab;

    // =============== MANAGERS ===============
    private HPHistoryManager hPHistoryManager;
    public CellHistoryManager CellHistoryManager => cellHistoryManager;
    public CellHistoryManager cellHistoryManager;
    public SkillCooldownManager SkillCooldownManager { get; private set; }

    // =============== UI TEXT CACHING ===============
    private string lastSlideButtonText;
    private string lastShotButtonText;
    private string lastShuffleButtonText;

    // =============== INITIALIZATION ===============
    private void Awake()
    {
        Singletone = this;
        cellHistoryManager = new CellHistoryManager();
        hPHistoryManager = new HPHistoryManager();
        SkillCooldownManager = new SkillCooldownManager();
    }

    // =============== GAME LIFECYCLE ===============
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

        UpdateSkillsButtonState();
        UpdateShotButtonText();
        UpdateShuffleButtonText();

        UpdateUI();
    }

    public void Restart()
    {
        TurnIndex = 0;
        SetPlayersHP();
        UIManager.Singletone.ShowHPBar();
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

    private void GameOver()
    {
        TimerController.Singletone.EndTime();
        BoardManager.Singltone.BlockAllButtons();
        UIManager.Singletone.HideHPBar();
        UIManager.Singletone.ShowRestartButton();
        UIManager.Singletone.BlockShotButton();
        UIManager.Singletone.BlockSlideButton();
        UIManager.Singletone.BlockShuffleButton();
        isPlaying = false;
    }

    public void SetWin(int winnerID)
    {
        wins[winnerID]++;
        UIManager.Singletone.SetWinLoseCountText(wins);
    }

    // =============== TURN MANAGEMENT ===============
    public void UpdateCurrentPlayerID(int clientID)
    {
        CurrentPlayerTurnID = clientID;
        UIManager.Singletone.UpdateCurrentPlayerText();
        UpdateSkillsButtonState();
        StartTimer();
    }

    public void ChangeTurnIndex(int index = 1)
    {
        TurnIndex += index;
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
        }
        if (SkillCooldownManager.ShouldRemoveShotCooldown(TurnIndex, CurrentPlayerTurnID))
        {
            SkillCooldownManager.RemoveShotCooldown();
            UIManager.Singletone.UnblockShotButton();
        }
        if (SkillCooldownManager.ShouldRemoveShuffleCooldown(TurnIndex, CurrentPlayerTurnID))
        {
            SkillCooldownManager.RemoveShuffleCooldown();
            UIManager.Singletone.UnblockShuffleButton();
        }

        UpdateSkillsButtonState();
        UpdateSlideButtonText();
        UpdateShotButtonText();
        UpdateShuffleButtonText();
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

    // =============== PLAYER INPUT HANDLING ===============
    public void OnClick(int row, int col)
    {
        if (IsAnimating)
        {
            Debug.LogWarning("IsAnimating break call method");
            return;
        }
        BoardManager.Singltone.FillCell(row, col, CurrentPlayerTurnID);
        ChangeTurnIndex();
        cellHistoryManager.AddMove(BoardManager.Singltone.GetCell(row, col), CurrentPlayerTurnID, TurnIndex);

        TimerController.Singletone.EndTime();

        if (BoardManager.Singltone.IsRow(row, col))
        {
            SpawnWinLines();
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
                StartCoroutine(DamageDelay(() => {
                    PassMoveToNextPlayer();
                }));
                return;
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

    public IEnumerator PlayerSkipMove()
    {
        while (IsAnimating)
        {
            yield return null;
        }

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
        ChangeTurnIndex();
        cellHistoryManager.CheckCellHistory(TurnIndex);
    }

    // =============== SKILL MECHANICS ===============
    public void ApplyShot()
    {
        // Только клиент, который нажал, управляет UI и кулдауном
        UIManager.Singletone.BlockShotButton();
        SkillCooldownManager.OnShotUsed(TurnIndex);
        UpdateShotButtonText();
        TimerController.Singletone.EndTime();

        int determineRandom = Random.Range(1, 100);
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            NetworkPlayer.Singletone.ApplyShotRpc(determineRandom);
        }
        else
        {
            StartCoroutine(ShootThreeArrowsAndFill(determineRandom));
        }
    }

    public IEnumerator ShootThreeArrowsAndFill(int seed)
    {
        UIManager.Singletone.BlockSlideButton();
        UIManager.Singletone.BlockShotButton();
        UIManager.Singletone.BlockShuffleButton();

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

        // === ШАГ 2: ВЫБИРАЕМ 3 СЛУЧАЙНЫЕ ЯЧЕЙКИ С ДЕТЕРМИНИРОВАННЫМ РАНДОМОМ ===
        List<Cell> allCells = new List<Cell>(BoardManager.Singltone.GetAllCells());
        if (allCells.Count == 0) yield break;

        System.Random deterministicRandom = new System.Random(seed);
        // Fisher-Yates shuffle с детерминированным рандомом
        for (int i = allCells.Count - 1; i > 0; i--)
        {
            int j = deterministicRandom.Next(0, i + 1);
            (allCells[i], allCells[j]) = (allCells[j], allCells[i]);
        }

        int count = Mathf.Min(ArrowCount, allCells.Count);
        List<Cell> targets = allCells.GetRange(0, count);

        // === ШАГ 3: АНИМАЦИЯ + ЗАПОЛНЕНИЕ ===
        foreach (Cell target in targets)
        {
            yield return StartCoroutine(AnimateSingleArrow(target));

            if (target.IsFillCell)
            {
                int oldOwner = target.IndexPlayer;
                cellHistoryManager.ReplaceCellWithNull(target, oldOwner);
                target.Clear();
                Debug.Log($"[Выстрел] Уничтожена фишка игрока {oldOwner} в ({target.row}, {target.coll})");
            }

            BoardManager.Singltone.FillCell(target.row, target.coll, shooterID);
            cellHistoryManager.AddMove(target, shooterID, TurnIndex);
            Debug.Log($"[Выстрел] Установлена моя фишка в ({target.row}, {target.coll})");
        }

        // === ШАГ 4–7: ПРОВЕРКИ И ПЕРЕДАЧА ХОДА (без изменений) ===
        bool hasRow = false;
        foreach (Cell cell in targets)
        {
            if (BoardManager.Singltone.IsRow(cell.row, cell.coll))
            {
                hasRow = true;
                break;
            }
        }

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
                SpawnWinLines();
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
                    StartCoroutine(DamageDelay(() => {
                        ChangeTurnIndex();
                        PassMoveToNextPlayer();
                    }));
                    yield break;
                }
            }
        }

        if (BoardManager.Singltone.IsGameDraw())
        {
            GameOver();
            UIManager.Singletone.SetDrawText("Ничья");
            yield break;
        }

        ChangeTurnIndex();
        PassMoveToNextPlayer();
    }

    private IEnumerator AnimateSingleArrow(Cell targetCell)
    {
        BeginAnimation();
        RectTransform canvasRect = Canvas.GetComponent<RectTransform>();
        Vector2 startPosition = new Vector2(0, -canvasRect.rect.height / 2 - 50);
        Vector2 endPosition = BoardManager.Singltone.GetCellScreenPosition(targetCell.row, targetCell.coll);

        GameObject shot = Instantiate(shotPrefab);
        Image image = shot.GetComponent<Image>();
        image.color = new Color(1f, 0.2f, 0.3f);
        RectTransform rectTransform = shot.GetComponent<RectTransform>();
        rectTransform.SetParent(Canvas.transform, false);
        rectTransform.sizeDelta = new Vector2(40, 40);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = startPosition;

        float duration = 1f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        Destroy(shot);
        EndAnimation();
        Debug.Log($"[Выстрел - Анимация] Попали в ячейку ({targetCell.row}, {targetCell.coll})");
        yield break;
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

    public void ApplyShuffle()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            NetworkPlayer.Singletone.ApplyShuffleRpc();
        }
        else
        {
            ShuffleAllCells();
        }

        UIManager.Singletone.BlockShuffleButton();
        SkillCooldownManager.OnShuffleUsed(TurnIndex);
        UpdateShuffleButtonText();
    }

    public void ShuffleAllCells()
    {
        BoardManager.Singltone.ShuffleAllCells();
    }

    // =============== DAMAGE & HP SYSTEM ===============
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

    private void SpawnWinLines()
    {
        if (winLinePrefab == null)
        {
            Debug.LogError("winLinePrefab not assigned!");
            return;
        }

        List<WinLineType> winTypes = BoardManager.Singltone.GetAllWinLines();
        if (winTypes.Count == 0) return;

        foreach (WinLineType type in winTypes)
        {
            WinLineConfig config = System.Array.Find(
                BoardManager.Singltone.winLineConfigs,
                c => c.type == type
            );

            if (config.type == WinLineType.None)
            {
                Debug.LogWarning($"No config found for WinLineType: {type}");
                continue;
            }

            GameObject winLine = Instantiate(winLinePrefab, Canvas.transform);
            RectTransform rt = winLine.GetComponent<RectTransform>();

            rt.anchoredPosition = config.positionOffset;
            rt.localEulerAngles = new Vector3(0, 0, config.rotation);

            Destroy(winLine, 3f);
        }
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

        SpawnWinLines();
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

    private IEnumerator DamageDelay(System.Action onDamageComplete = null)
    {
        isBlocking = true;
        BoardManager.Singltone.BlockAllButtons();
        UIManager.Singletone.BlockSlideButton();
        UIManager.Singletone.BlockShotButton();
        UIManager.Singletone.BlockShuffleButton();

        yield return new WaitForSeconds(3f);

        UpdateSkillsButtonState();
        isBlocking = false;
        cellHistoryManager.Clear();
        BoardManager.Singltone.ClearAndUnbloackCells();

        onDamageComplete?.Invoke();
    }

    // =============== UI & BUTTON STATE MANAGEMENT ===============
    public void UpdateUI()
    {
        UIManager.Singletone.HideActiveSessionInfo();
        UIManager.Singletone.ShowMoveInfo();
        UIManager.Singletone.ShowWinLoseCountInfo();
        UIManager.Singletone.ShowSmileScreen();
        UIManager.Singletone.ShowHPBar();
        UIManager.Singletone.ShowSlideButton();
        UIManager.Singletone.ShowShotButton();
        UIManager.Singletone.ShowShuffleButton();
    }

    private void UpdateSkillsButtonState()
    {
        if (IsBlocking)
        {
            UIManager.Singletone.BlockSlideButton();
            UIManager.Singletone.BlockShotButton();
            UIManager.Singletone.BlockShuffleButton();
            StartCoroutine(WaitForBlockingEnd());
            return;
        }

        CheckSlideButton();
        CheckShotButton();
        CheckShuffleButton();
    }

    private IEnumerator WaitForBlockingEnd()
    {
        yield return new WaitUntil(() => !IsBlocking);
        CheckSlideButton();
        CheckShotButton();
        CheckShuffleButton();
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

    private void CheckShuffleButton()
    {
        if (IsOurTurn())
        {
            if (!SkillCooldownManager.IsShuffleOnCooldown())
            {
                UIManager.Singletone.UnblockShuffleButton();
            }
        }
        else
        {
            UIManager.Singletone.BlockShuffleButton();
        }
    }

    // =============== COOLDOWN TEXT UPDATES ===============
    private void UpdateSlideButtonText()
    {
        string newText = SkillCooldownManager.GetSlideButtonText(TurnIndex);

        if (newText != lastSlideButtonText)
        {
            UIManager.Singletone.SetCooldownText(newText);
            lastSlideButtonText = newText;
        }
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

    private void UpdateShuffleButtonText()
    {
        string newText = SkillCooldownManager.GetShuffleButtonText(TurnIndex);
        if (newText != lastShuffleButtonText)
        {
            UIManager.Singletone.SetShuffleCooldownText(newText);
            lastShuffleButtonText = newText;
        }
    }

    // =============== TIMER ===============
    public void StartTimer()
    {
        if (!IsOurTurn())
        {
            return;
        }

        TimerController.Singletone.StartTime();
    }

    // =============== OFFSET & INIT SYNC ===============
    public void UpdateOffSet(int clientID)
    {
        startOffSet = clientID;
        UpdateCurrentPlayerID(clientID);
        UIManager.Singletone.HideRestartButton();
    }

    public int GetOffSet()
    {
        return startOffSet;
    }
}