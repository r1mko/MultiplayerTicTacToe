using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class BoardManager : MonoBehaviour
{

    public enum WinLineType
    {
        None,
        TopRow,
        MiddleRow,
        BottomRow,
        LeftColumn,
        MiddleColumn,
        RightColumn,
        DiagonalMain,      // 0,0 → 2,2
        DiagonalAnti       // 0,2 → 2,0
    }

    [System.Serializable]
    public struct WinLineConfig
    {
        public WinLineType type;
        public Vector2 positionOffset;
        public float rotation;
    }

    public WinLineConfig[] winLineConfigs;

    [SerializeField] private GameObject board;
    private Dictionary<Cell, Transform> activeChipTransforms = new Dictionary<Cell, Transform>();
    public Canvas Canvas;

    public static BoardManager Singltone;

    private Cell[,] buttons = new Cell[3, 3];

    private void Awake()
    {
        Singltone = this;
        ShowBoard();
    }

    private void Start()
    {
        var cells = GetComponentsInChildren<Cell>();
        int n = 0;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                buttons[i, j] = cells[n];
                n++;

                int row = i;
                int column = j;

                buttons[i, j].Init(row, column);

            }
        }
        BlockAllButtons();
    }

    public Cell GetCell(int row, int col)
    {
        return buttons[row, col];
    }

    public void BlockAllButtons()
    {
        foreach (var item in buttons)
        {
            item.Block();
        }
    }

    /// <summary>
    /// Возвращает позицию центра ячейки в координатах Canvas (для UI)
    /// </summary>
    public Vector2 GetCellScreenPosition(int row, int col)
    {
        if (row < 0 || row >= 3 || col < 0 || col >= 3) return Vector2.zero;

        RectTransform cellRect = buttons[row, col].GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        cellRect.GetWorldCorners(corners);

        // Центр ячейки в мировых координатах
        Vector3 worldCenter = (corners[0] + corners[2]) / 2f;

        // Преобразуем в локальные координаты Canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            Canvas.transform as RectTransform,
            worldCenter,
            null,
            out Vector2 localPosition
        );

        return localPosition;
    }

    public List<Cell> GetAllCells()
    {
        List<Cell> allCells = new List<Cell>();
        for (int i = 0; i < buttons.GetLength(0); i++)
        {
            for (int j = 0; j < buttons.GetLength(1); j++)
            {
                allCells.Add(buttons[i, j]);
            }
        }
        return allCells;
    }

    public void OnClickCell(int row, int coll, Cell cell)
    {
        if (!GameManager.Singletone.IsOurTurn())
        {
            return;
        }

        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            NetworkPlayer.Singletone.OnClickRpc(row, coll);
        }
        else
        {
            GameManager.Singletone.OnClick(row, coll);
        }

    }

    public void FillCell(int row, int col, int currentPlayerIndex)
    {
        buttons[row, col].Fill(currentPlayerIndex);
    }

    public bool IsRow(int row, int column)
    {
        Cell cell = buttons[row, column];
        if (!cell.IsFillCell) return false;

        int indexPlayer = cell.IndexPlayer;

        // Горизонталь
        if (buttons[row, 0].IsFillCell && buttons[row, 1].IsFillCell && buttons[row, 2].IsFillCell &&
            buttons[row, 0].IndexPlayer == indexPlayer &&
            buttons[row, 1].IndexPlayer == indexPlayer &&
            buttons[row, 2].IndexPlayer == indexPlayer)
        {
            return true;
        }

        // Вертикаль
        if (buttons[0, column].IsFillCell && buttons[1, column].IsFillCell && buttons[2, column].IsFillCell &&
            buttons[0, column].IndexPlayer == indexPlayer &&
            buttons[1, column].IndexPlayer == indexPlayer &&
            buttons[2, column].IndexPlayer == indexPlayer)
        {
            return true;
        }

        // Главная диагональ
        if (row == column &&
            buttons[0, 0].IsFillCell && buttons[1, 1].IsFillCell && buttons[2, 2].IsFillCell &&
            buttons[0, 0].IndexPlayer == indexPlayer &&
            buttons[1, 1].IndexPlayer == indexPlayer &&
            buttons[2, 2].IndexPlayer == indexPlayer)
        {
            return true;
        }

        // Побочная диагональ
        if (row + column == 2 &&
            buttons[0, 2].IsFillCell && buttons[1, 1].IsFillCell && buttons[2, 0].IsFillCell &&
            buttons[0, 2].IndexPlayer == indexPlayer &&
            buttons[1, 1].IndexPlayer == indexPlayer &&
            buttons[2, 0].IndexPlayer == indexPlayer)
        {
            return true;
        }

        return false;
    }

    public List<WinLineType> GetAllWinLines()
    {
        List<WinLineType> winLines = new List<WinLineType>();

        // Проверяем горизонтали
        for (int r = 0; r < 3; r++)
        {
            if (buttons[r, 0].IsFillCell && buttons[r, 1].IsFillCell && buttons[r, 2].IsFillCell)
            {
                int p = buttons[r, 0].IndexPlayer;
                if (buttons[r, 1].IndexPlayer == p && buttons[r, 2].IndexPlayer == p)
                {
                    winLines.Add(r switch { 0 => WinLineType.TopRow, 1 => WinLineType.MiddleRow, 2 => WinLineType.BottomRow, _ => WinLineType.None });
                }
            }
        }

        // Проверяем вертикали
        for (int c = 0; c < 3; c++)
        {
            if (buttons[0, c].IsFillCell && buttons[1, c].IsFillCell && buttons[2, c].IsFillCell)
            {
                int p = buttons[0, c].IndexPlayer;
                if (buttons[1, c].IndexPlayer == p && buttons[2, c].IndexPlayer == p)
                {
                    winLines.Add(c switch { 0 => WinLineType.LeftColumn, 1 => WinLineType.MiddleColumn, 2 => WinLineType.RightColumn, _ => WinLineType.None });
                }
            }
        }

        // Главная диагональ
        if (buttons[0, 0].IsFillCell && buttons[1, 1].IsFillCell && buttons[2, 2].IsFillCell)
        {
            int p = buttons[0, 0].IndexPlayer;
            if (buttons[1, 1].IndexPlayer == p && buttons[2, 2].IndexPlayer == p)
            {
                winLines.Add(WinLineType.DiagonalMain);
            }
        }

        // Побочная диагональ
        if (buttons[0, 2].IsFillCell && buttons[1, 1].IsFillCell && buttons[2, 0].IsFillCell)
        {
            int p = buttons[0, 2].IndexPlayer;
            if (buttons[1, 1].IndexPlayer == p && buttons[2, 0].IndexPlayer == p)
            {
                winLines.Add(WinLineType.DiagonalAnti);
            }
        }

        return winLines;
    }
    public int GetWinnerFromLineType(WinLineType type)
    {
        switch (type)
        {
            case WinLineType.TopRow:
            case WinLineType.MiddleRow:
            case WinLineType.BottomRow:
                int row = type == WinLineType.TopRow ? 0 : (type == WinLineType.MiddleRow ? 1 : 2);
                return buttons[row, 0].IsFillCell ? buttons[row, 0].IndexPlayer : -1;

            case WinLineType.LeftColumn:
            case WinLineType.MiddleColumn:
            case WinLineType.RightColumn:
                int col = type == WinLineType.LeftColumn ? 0 : (type == WinLineType.MiddleColumn ? 1 : 2);
                return buttons[0, col].IsFillCell ? buttons[0, col].IndexPlayer : -1;

            case WinLineType.DiagonalMain:
                return buttons[0, 0].IsFillCell ? buttons[0, 0].IndexPlayer : -1;

            case WinLineType.DiagonalAnti:
                return buttons[0, 2].IsFillCell ? buttons[0, 2].IndexPlayer : -1;

            default:
                return -1;
        }
    }

    public bool IsGameDraw()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (!buttons[i, j].IsFillCell)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void ClearAndUnbloackCells()
    {
        foreach (var item in buttons)
        {
            item.Clear();
            item.Unblock();
        }
    }

    public void ShowBoard()
    {
        board.SetActive(true);
    }
    public void HideBoard()
    {
        board.SetActive(false);
    }

    public bool TryGetEmptyCell(out Cell cell)
    {
        List<Cell> emptyCells = new List<Cell>();

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (!buttons[i, j].IsFillCell)
                {
                    emptyCells.Add(buttons[i, j]);
                }
            }
        }

        if (emptyCells.Count > 0)
        {
            cell = emptyCells[Random.Range(0, emptyCells.Count)];
            return true;
        }
        else
        {
            cell = null;
            return false;
        }
    }

    public int[,] GetBoardState()
    {
        int size = 3;
        int[,] board = new int[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Cell cell = buttons[i, j];
                if (cell.IsFillCell)
                {
                    board[i, j] = cell.IndexPlayer;
                }
                else
                {
                    board[i, j] = -1; // Свободная клетка
                }
            }
        }

        return board;
    }

    public void ApplyGravity()
    {
        // 1. Вычисляем, КАКИЕ фишки и КУДА переместятся
        bool changes = false;
        Dictionary<Cell, Cell> cellRemap = new Dictionary<Cell, Cell>();

        for (int j = 0; j < 3; j++)
        {
            List<Cell> columnCells = new List<Cell>();
            for (int i = 0; i < 3; i++)
            {
                if (buttons[i, j].IsFillCell)
                {
                    columnCells.Add(buttons[i, j]);
                }
            }

            if (columnCells.Count == 0) continue;

            bool needToMove = false;
            int expectedRow = 2;
            for (int k = columnCells.Count - 1; k >= 0; k--)
            {
                if (columnCells[k].row != expectedRow)
                {
                    needToMove = true;
                    break;
                }
                expectedRow--;
            }

            if (!needToMove) continue;

            int fillRow = 2;
            for (int k = columnCells.Count - 1; k >= 0; k--)
            {
                Cell oldCell = columnCells[k];
                Cell targetCell = buttons[fillRow, j];

                if (oldCell.row != fillRow)
                {
                    cellRemap[oldCell] = targetCell;
                    changes = true;
                }
                fillRow--;
            }
        }

        if (!changes)
        {
            Debug.Log("Гравитация: всё на месте.");
            return;
        }

        Debug.Log("Гравитация: фишки упали. Запускаем анимации...");
        GameManager.Singletone.BeginAnimation();

        // 2. Скрываем оригинальные фишки перед анимацией
        foreach (var (oldCell, newCell) in cellRemap)
        {
            oldCell.HideAll();
        }

        // 3. Запускаем анимации
        List<Coroutine> runningAnimations = new List<Coroutine>();
        foreach (var (oldCell, newCell) in cellRemap)
        {
            GameObject chipGO = oldCell.CreateChipVisualCopy();
            if (chipGO != null)
            {
                Coroutine animCoroutine = StartCoroutine(AnimateMoveChip(chipGO, oldCell, newCell));
                runningAnimations.Add(animCoroutine);
            }
        }

        // 4. Ждём завершения всех анимаций
        StartCoroutine(WaitForAllAnimationsAndThen(runningAnimations, cellRemap));
    }

    // Анимация дубликата фишки
    private IEnumerator AnimateMoveChip(GameObject chipGO, Cell fromCell, Cell toCell, float duration = 0.3f)
    {
        // Получаем начальную и конечную позиции в координатах Canvas
        Vector2 localStartPos = GetCellScreenPosition(fromCell.row, fromCell.coll);
        Vector2 localEndPos = GetCellScreenPosition(toCell.row, toCell.coll);

        // Переводим в мировые координаты
        Vector3 startPos = Canvas.transform.TransformPoint(new Vector3(localStartPos.x, localStartPos.y, 0));
        Vector3 endPos = Canvas.transform.TransformPoint(new Vector3(localEndPos.x, localEndPos.y, 0));

        // Анимация
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            chipGO.transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        chipGO.transform.position = endPos;

        // Удаляем дубликат
        Destroy(chipGO);
    }

    private IEnumerator WaitForAllAnimationsAndThen(List<Coroutine> animations, Dictionary<Cell, Cell> cellRemap)
    {
        foreach (var anim in animations)
        {
            if (anim != null)
            {
                yield return anim;
            }
        }

        Debug.Log("Все анимации завершены. Обновляем логическое состояние доски.");

        // 4. Обновляем логическое состояние с сохранением времени жизни
        for (int j = 0; j < 3; j++)
        {
            // Собираем НЕПУСТЫЕ ячейки сверху вниз (сохраняем порядок)
            List<(Cell oldCell, int playerID, int setTurn, int lifetime)> columnData = new List<(Cell, int, int, int)>();
            for (int i = 0; i < 3; i++)
            {
                if (buttons[i, j].IsFillCell)
                {
                    Cell cell = buttons[i, j];
                    columnData.Add((cell, cell.IndexPlayer, cell.cellSetAtTurn, cell.CellLifeTime));
                }
            }

            if (columnData.Count == 0) continue;

            // Очищаем столбец
            for (int i = 0; i < 3; i++)
            {
                buttons[i, j].Clear();
            }

            // Заполняем СНИЗУ, но в том же порядке: первая фишка — самая верхняя, поэтому она должна быть выше остальных
            // То есть: размещаем их, начиная с row = 3 - columnData.Count
            int startRow = 3 - columnData.Count;
            for (int idx = 0; idx < columnData.Count; idx++)
            {
                var (oldCell, playerID, setTurn, lifetime) = columnData[idx];
                Cell targetCell = buttons[startRow + idx, j];
                targetCell.Fill(playerID);
                targetCell.SetCell(setTurn);
                targetCell.SetCellLifetime(lifetime);
            }
        }

        // 5. Обновляем CellHistory
        var cellHistory = GameManager.Singletone.cellHistoryManager.CellHistory;
        foreach (var playerEntry in cellHistory)
        {
            for (int i = 0; i < playerEntry.Value.Count; i++)
            {
                if (playerEntry.Value[i] != null && cellRemap.TryGetValue(playerEntry.Value[i], out Cell newCell))
                {
                    playerEntry.Value[i] = newCell;
                }
            }
        }

        // 6. Используем АКТУАЛЬНЫЙ TurnIndex при проверке
        GameManager.Singletone.cellHistoryManager.CheckCellHistory(GameManager.Singletone.TurnIndex);

        // 7. Проверка побед и урона
        List<int> victims = new List<int>();
        HashSet<int> winners = new HashSet<int>();
        foreach (var (oldCell, newCell) in cellRemap)
        {
            if (IsRow(newCell.row, newCell.coll))
            {
                int ownerOfWinningRow = newCell.IndexPlayer;
                if (!winners.Contains(ownerOfWinningRow))
                {
                    winners.Add(ownerOfWinningRow);
                    victims.Add(1 - ownerOfWinningRow);
                    Debug.Log($"Игрок {ownerOfWinningRow} собрал ряд после гравитации! Игроку {1 - ownerOfWinningRow} будет нанесён урон!");
                }
            }
        }

        if (victims.Count > 0)
        {
            if (NetworkPlayer.Singletone.IsMultiplayer())
            {
                if (NetworkPlayer.Singletone.IsServer)
                {
                    NetworkPlayer.Singletone.TriggerMultipleDamageRpc(victims.ToArray());
                }
            }
            else
            {
                GameManager.Singletone.ApplyMultipleDamages(victims);
            }
        }
        else
        {
            Debug.Log("После гравитации ни одна линия не собрана. Продолжаем игру.");
        }

        GameManager.Singletone.EndAnimation();
    }

    public void ShuffleAllCells()
    {
        // 1. Собираем все заполненные фишки (сохраняем владельца)
        List<(int playerID, Cell originalCell)> filledData = new List<(int, Cell)>();
        List<Cell> allCells = new List<Cell>();

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Cell cell = buttons[i, j];
                allCells.Add(cell);
                if (cell.IsFillCell)
                {
                    filledData.Add((cell.IndexPlayer, cell));
                }
            }
        }

        int filledCount = filledData.Count;
        if (filledCount == 0)
        {
            Debug.Log("[Shuffle] Нет фишек для перемешивания.");
            return;
        }

        // 2. Создаём копию списка всех ячеек и перемешиваем её
        List<Cell> shuffledCells = new List<Cell>(allCells);
        int seed = GameManager.Singletone.TurnIndex;
        System.Random rng = new System.Random(seed);

        // Fisher-Yates shuffle
        for (int i = shuffledCells.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (shuffledCells[i], shuffledCells[j]) = (shuffledCells[j], shuffledCells[i]);
        }

        // 3. Берём первые `filledCount` ячеек как цели
        List<Cell> targetCells = shuffledCells.GetRange(0, filledCount);

        // 4. Создаём отображение: старая ячейка -> новая ячейка
        Dictionary<Cell, Cell> cellRemap = new Dictionary<Cell, Cell>();
        for (int i = 0; i < filledCount; i++)
        {
            var (playerID, oldCell) = filledData[i];
            Cell newCell = targetCells[i];
            cellRemap[oldCell] = newCell;
        }

        GameManager.Singletone.BeginAnimation();

        // 5. Скрываем оригинальные фишки перед анимацией
        foreach (var (oldCell, newCell) in cellRemap)
        {
            oldCell.HideAll();
        }

        // 6. Запускаем анимации перемещения копий фишек
        List<Coroutine> runningAnimations = new List<Coroutine>();
        foreach (var (oldCell, newCell) in cellRemap)
        {
            GameObject chipGO = oldCell.CreateChipVisualCopy();
            if (chipGO != null)
            {
                Coroutine animCoroutine = StartCoroutine(AnimateMoveChip(chipGO, oldCell, newCell));
                runningAnimations.Add(animCoroutine);
            }
        }

        // 7. Ждём завершения всех анимаций и обновляем логику
        StartCoroutine(WaitForAllAnimationsAndThenShuffle(runningAnimations, cellRemap, filledData, targetCells));
    }

    // Новый метод для ожидания анимаций и обновления логики после перемешивания
    private IEnumerator WaitForAllAnimationsAndThenShuffle(List<Coroutine> animations, Dictionary<Cell, Cell> cellRemap, List<(int playerID, Cell originalCell)> filledData, List<Cell> targetCells)
    {
        foreach (var anim in animations)
        {
            if (anim != null)
            {
                yield return anim;
            }
        }

        Debug.Log("Все анимации перемешивания завершены. Обновляем логическое состояние доски.");

        // 8. Очищаем ВСЁ поле
        List<Cell> allCells = new List<Cell>();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Cell cell = buttons[i, j];
                allCells.Add(cell);
                cell.Clear();
            }
        }

        // 9. Заполняем новые ячейки (логически)
        foreach (var (playerID, oldCell) in filledData)
        {
            if (cellRemap.TryGetValue(oldCell, out Cell newCell))
            {
                newCell.Fill(playerID);
            }
        }

        // 10. Обновляем CellHistory
        var cellHistory = GameManager.Singletone.cellHistoryManager.CellHistory;
        foreach (var playerEntry in cellHistory)
        {
            for (int i = 0; i < playerEntry.Value.Count; i++)
            {
                if (playerEntry.Value[i] != null && cellRemap.TryGetValue(playerEntry.Value[i], out Cell newCell))
                {
                    playerEntry.Value[i] = newCell;
                }
            }
        }

        // 11. *Только после обновления ссылок* вызываем CheckCellHistory
        GameManager.Singletone.cellHistoryManager.CheckCellHistory(GameManager.Singletone.TurnIndex);

        // 12. Проверка рядов и урон
        List<int> victims = new List<int>();
        HashSet<int> winners = new HashSet<int>();

        foreach (Cell newCell in targetCells)
        {
            if (IsRow(newCell.row, newCell.coll))
            {
                int winner = newCell.IndexPlayer;
                if (!winners.Contains(winner))
                {
                    winners.Add(winner);
                    victims.Add(1 - winner);
                    Debug.Log($"[Shuffle] Ряд собран игроком {winner} в ({newCell.row},{newCell.coll})!");
                }
            }
        }

        if (victims.Count > 0)
        {
            if (NetworkPlayer.Singletone.IsMultiplayer())
            {
                if (NetworkPlayer.Singletone.IsServer)
                {
                    NetworkPlayer.Singletone.TriggerMultipleDamageRpc(victims.ToArray());
                }
            }
            else
            {
                GameManager.Singletone.ApplyMultipleDamages(victims);
            }
        }
        else
        {
            Debug.Log("[Shuffle] Ни одного ряда после перемешивания.");
        }

        GameManager.Singletone.EndAnimation();
    }
}