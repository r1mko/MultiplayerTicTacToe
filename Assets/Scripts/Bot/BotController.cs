using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BotController : MonoBehaviour
{
    private Coroutine botTurnCoroutine;

    void Update()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer())
            return;
        if (!GameManager.Singletone.IsPlaying)
            return;
        if (!GameManager.Singletone.IsBotTurn())
            return;
        if (botTurnCoroutine != null)
            return;

        botTurnCoroutine = StartCoroutine(DelayedBotTurn());
    }

    private IEnumerator DelayedBotTurn()
    {
        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(BotTurn());
    }


    //region Бот: Основной ход
    private IEnumerator BotTurn()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.2f));

        var board = BoardManager.Singltone.GetBoardState();
        int playerID = GameManager.Singletone.CurrentPlayerTurnID;
        int opponentID = 1 - playerID;

        LogGameState(playerID, opponentID, board);

        Cell bestCell = null;
        string reason = "";

        // === 1. МОЖЕМ ЛИ ПОБЕДИТЬ СЕЙЧАС? (и линия устойчива) ===
        bestCell = FindWinningMove(board, playerID, out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === 2. МОЖЕТ ЛИ ПРОТИВНИК ПОБЕДИТЬ СЕЙЧАС? (и угроза реальна) ===
        bestCell = FindWinningMove(board, opponentID, out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === 3. ОПАСНОСТЬ: ПОСЛЕ ИСЧЕЗНОВЕНИЯ НАШЕЙ КЛЕТКИ ПРОТИВНИК ПОБЕДИТ? ===
        bestCell = FindThreatAfterOurRemoval(playerID, opponentID, out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === 4. МОЖЕМ ЛИ ПОБЕДИТЬ, КОГДА ИСЧЕЗНЕТ НАША КЛЕТКА? ===
        bestCell = FindFutureWinAfterOurRemoval(playerID, opponentID, out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === 5. МОЖЕМ ЛИ ПОБЕДИТЬ, КОГДА ИСЧЕЗНЕТ ЕГО КЛЕТКА? ===
        bestCell = FindFutureWinAfterOpponentRemoval(playerID, opponentID, out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === 6. РАННЯЯ ЗАЩИТА: Игрок атакует нашу слабую клетку (index=1) ===
        bestCell = FindEarlyDefenseAgainstWeakAttack(playerID, opponentID, out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === 7. АТАКА: Атакуем его слабую клетку (index=1) ===
        bestCell = FindAttackOnWeakCell(playerID, opponentID, out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === 8. ЦЕНТР ===
        var center = BoardManager.Singltone.GetCell(1, 1);
        if (center != null && center.IsEmpty())
        {
            reason = "[Бот] Центр (1,1): стратегически важная позиция.";
            Debug.Log(reason);
            MakeMove(center);
            botTurnCoroutine = null;
            yield break;
        }

        // === 9. УГЛЫ (предпочтительно с угрозой) ===
        bestCell = GetBestThreateningCorner(playerID, out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === 10. СТОРОНЫ ===
        bestCell = GetBestAvailableCorner(out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === РЕЗЕРВ ===
        if (BoardManager.Singltone.TryGetEmptyCell(out Cell fallback))
        {
            reason = $"[Бот] Резерв: ставим в ({fallback.row},{fallback.coll}) — больше нет вариантов.";
            Debug.Log(reason);
            MakeMove(fallback);
        }
        else
        {
            Debug.Log("[Бот] Нет ходов — игра должна была закончиться.");
        }

        botTurnCoroutine = null;
    }

    private void MakeMove(Cell cell)
    {
        GameManager.Singletone.OnClick(cell.row, cell.coll);
    }

    // Бот: Логи и вспомогательные
    private void LogGameState(int playerID, int opponentID, int[,] board)
    {
        Debug.Log("[Бот] СОСТОЯНИЕ ДОСКИ:");
        Debug.Log($"[Бот]   {board[0, 0]} | {board[0, 1]} | {board[0, 2]}");
        Debug.Log("[Бот]  -----------");
        Debug.Log($"[Бот]   {board[1, 0]} | {board[1, 1]} | {board[1, 2]}");
        Debug.Log("[Бот]  -----------");
        Debug.Log($"[Бот]   {board[2, 0]} | {board[2, 1]} | {board[2, 2]}");

        string botCells = "Бот (ID=" + playerID + "): ";
        string playerCells = "Игрок (ID=" + opponentID + "): ";
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (board[r, c] == playerID)
                    botCells += $"({r},{c}) ";
                else if (board[r, c] == opponentID)
                    playerCells += $"({r},{c}) ";
            }
        }
        Debug.Log($"[Бот] {botCells}");
        Debug.Log($"[Бот] {playerCells}");

        Cell botOld = GetOldestCell(playerID);
        Cell playerOld = GetOldestCell(opponentID);
        if (botOld != null) Debug.Log($"[Бот] Наша старая клетка (исчезнет): ({botOld.row},{botOld.coll})");
        if (playerOld != null) Debug.Log($"[Бот] Его старая клетка (исчезнет): ({playerOld.row},{playerOld.coll})");
    }

    private Cell GetOldestCell(int playerID)
    {
        if (GameManager.Singletone.CellHistoryManager.CellHistory.TryGetValue(playerID, out List<Cell> history))
        {
            if (history.Count >= 3) return history[2];
        }
        return null;
    }

    private bool IsCellMarkedForRemoval(Cell cell, int playerID)
    {
        if (!GameManager.Singletone.CellHistoryManager.CellHistory.TryGetValue(playerID, out List<Cell> history))
            return false;
        return history.Count >= 3 && history[2] == cell;
    }

    private void MarkCellsForRemovalInSimulation(int[,] simBoard, int playerID)
    {
        Cell cellToRemove = GetOldestCell(playerID);
        if (cellToRemove != null)
        {
            simBoard[cellToRemove.row, cellToRemove.coll] = -1;
        }
    }

    private List<(Cell[], string)> GetLinesThrough(int row, int col)
    {
        var lines = new List<(Cell[], string)>();

        // Строка
        Cell[] rowCells = {
            BoardManager.Singltone.GetCell(row, 0),
            BoardManager.Singltone.GetCell(row, 1),
            BoardManager.Singltone.GetCell(row, 2)
        };
        lines.Add((rowCells, row == 0 ? "верхней строке" : row == 1 ? "средней строке" : "нижней строке"));

        // Столбец
        Cell[] colCells = {
            BoardManager.Singltone.GetCell(0, col),
            BoardManager.Singltone.GetCell(1, col),
            BoardManager.Singltone.GetCell(2, col)
        };
        lines.Add((colCells, col == 0 ? "левом столбце" : col == 1 ? "среднем столбце" : "правом столбце"));

        // Главная диагональ
        if (row == col)
        {
            Cell[] diag1 = {
                BoardManager.Singltone.GetCell(0,0),
                BoardManager.Singltone.GetCell(1,1),
                BoardManager.Singltone.GetCell(2,2)
            };
            lines.Add((diag1, "главной диагонали"));
        }

        // Побочная диагональ
        if (row + col == 2)
        {
            Cell[] diag2 = {
                BoardManager.Singltone.GetCell(0,2),
                BoardManager.Singltone.GetCell(1,1),
                BoardManager.Singltone.GetCell(2,0)
            };
            lines.Add((diag2, "побочной диагонали"));
        }

        return lines;
    }

    //Бот: Проверки победы и угроз

    private Cell FindWinningMove(int[,] board, int playerID, out string reason)
    {
        reason = "";
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (board[r, c] == -1)
                {
                    board[r, c] = playerID;
                    if (IsWinningOnBoard(board, r, c, playerID))
                    {
                        board[r, c] = -1;
                        Cell cell = BoardManager.Singltone.GetCell(r, c);
                        if (cell.IsEmpty())
                        {
                            if (IsWinStable(r, c, playerID))
                            {
                                string line = GetLineType(r, c);
                                reason = $"Побеждаем сейчас: ставим в ({r},{c}) — победа по {line}.";
                                return cell;
                            }
                            else
                            {
                                reason = $"[Бот] Победа в ({r},{c}) НЕ устойчива — одна из наших клеток исчезнет. Игнорируем.";
                                Debug.Log(reason);
                            }
                        }
                    }
                    board[r, c] = -1;
                }
            }
        }
        return null;
    }

    private bool IsWinningOnBoard(int[,] board, int row, int col, int playerID)
    {
        return IsWinningRow(board, row, playerID) ||
               IsWinningCol(board, col, playerID) ||
               (row == col && IsWinningDiag1(board, playerID)) ||
               (row + col == 2 && IsWinningDiag2(board, playerID));
    }

    private bool IsWinningRow(int[,] board, int row, int playerID)
    {
        return board[row, 0] == playerID && board[row, 1] == playerID && board[row, 2] == playerID;
    }

    private bool IsWinningCol(int[,] board, int col, int playerID)
    {
        return board[0, col] == playerID && board[1, col] == playerID && board[2, col] == playerID;
    }

    private bool IsWinningDiag1(int[,] board, int playerID)
    {
        return board[0, 0] == playerID && board[1, 1] == playerID && board[2, 2] == playerID;
    }

    private bool IsWinningDiag2(int[,] board, int playerID)
    {
        return board[0, 2] == playerID && board[1, 1] == playerID && board[2, 0] == playerID;
    }

    private string GetLineType(int row, int col)
    {
        if (row == 0) return "верхней строке";
        if (row == 1) return "средней строке";
        if (row == 2) return "нижней строке";
        if (col == 0) return "левом столбце";
        if (col == 1) return "среднем столбце";
        if (col == 2) return "правом столбце";
        if (row == col) return "главной диагонали";
        if (row + col == 2) return "побочной диагонали";
        return "ряду";
    }

    private Cell FindFutureWinAfterOurRemoval(int playerID, int opponentID, out string reason)
    {
        reason = "";
        var board = BoardManager.Singltone.GetBoardState();
        var futureBoard = (int[,])board.Clone();
        MarkCellsForRemovalInSimulation(futureBoard, playerID); // Удаляем нашу старую клетку

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (futureBoard[r, c] == -1)
                {
                    futureBoard[r, c] = playerID;
                    if (IsWinningOnBoard(futureBoard, r, c, playerID))
                    {
                        futureBoard[r, c] = -1;
                        Cell cell = BoardManager.Singltone.GetCell(r, c);
                        if (cell.IsEmpty())
                        {
                            // Убедимся, что после удаления этой линии не разрушится
                            if (IsWinStable(r, c, playerID))
                            {
                                Cell ourOld = GetOldestCell(playerID);
                                string line = GetLineType(r, c);
                                reason = $"ВЫИГРЫШ ПОСЛЕ НАШЕГО ИСЧЕЗНОВЕНИЯ: после исчезновения ({ourOld?.row},{ourOld?.coll}) победим по {line}. Ставим в ({r},{c}).";
                                return cell;
                            }
                        }
                    }
                    futureBoard[r, c] = -1;
                }
            }
        }
        return null;
    }

    //Бот: Сложные стратегии — устойчивость, атака, защита

    private bool IsWinStable(int row, int col, int playerID)
    {
        var board = BoardManager.Singltone.GetBoardState();
        board[row, col] = playerID;

        if (IsWinningRow(board, row, playerID))
        {
            for (int c = 0; c < 3; c++)
            {
                if (c == col) continue;
                Cell cell = BoardManager.Singltone.GetCell(row, c);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == playerID && IsCellMarkedForRemoval(cell, playerID))
                    return false;
            }
        }
        else if (IsWinningCol(board, col, playerID))
        {
            for (int r = 0; r < 3; r++)
            {
                if (r == row) continue;
                Cell cell = BoardManager.Singltone.GetCell(r, col);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == playerID && IsCellMarkedForRemoval(cell, playerID))
                    return false;
            }
        }
        else if (row == col && IsWinningDiag1(board, playerID))
        {
            for (int i = 0; i < 3; i++)
            {
                if (i == row) continue;
                Cell cell = BoardManager.Singltone.GetCell(i, i);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == playerID && IsCellMarkedForRemoval(cell, playerID))
                    return false;
            }
        }
        else if (row + col == 2 && IsWinningDiag2(board, playerID))
        {
            int[] diagR = { 0, 1, 2 }, diagC = { 2, 1, 0 };
            for (int i = 0; i < 3; i++)
            {
                if (diagR[i] == row && diagC[i] == col) continue;
                Cell cell = BoardManager.Singltone.GetCell(diagR[i], diagC[i]);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == playerID && IsCellMarkedForRemoval(cell, playerID))
                    return false;
            }
        }

        return true;
    }

    private bool IsThreatReal(int row, int col, int opponentID)
    {
        var board = BoardManager.Singltone.GetBoardState();
        board[row, col] = opponentID;

        if (IsWinningRow(board, row, opponentID))
        {
            for (int c = 0; c < 3; c++)
            {
                if (c == col) continue;
                Cell cell = BoardManager.Singltone.GetCell(row, c);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == opponentID && IsCellMarkedForRemoval(cell, opponentID))
                    return false;
            }
        }
        else if (IsWinningCol(board, col, opponentID))
        {
            for (int r = 0; r < 3; r++)
            {
                if (r == row) continue;
                Cell cell = BoardManager.Singltone.GetCell(r, col);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == opponentID && IsCellMarkedForRemoval(cell, opponentID))
                    return false;
            }
        }
        else if (row == col && IsWinningDiag1(board, opponentID))
        {
            for (int i = 0; i < 3; i++)
            {
                if (i == row) continue;
                Cell cell = BoardManager.Singltone.GetCell(i, i);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == opponentID && IsCellMarkedForRemoval(cell, opponentID))
                    return false;
            }
        }
        else if (row + col == 2 && IsWinningDiag2(board, opponentID))
        {
            int[] diagR = { 0, 1, 2 }, diagC = { 2, 1, 0 };
            for (int i = 0; i < 3; i++)
            {
                if (diagR[i] == row && diagC[i] == col) continue;
                Cell cell = BoardManager.Singltone.GetCell(diagR[i], diagC[i]);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == opponentID && IsCellMarkedForRemoval(cell, opponentID))
                    return false;
            }
        }

        return true;
    }

    private Cell FindFutureWinAfterOpponentRemoval(int playerID, int opponentID, out string reason)
    {
        reason = "";
        var board = BoardManager.Singltone.GetBoardState();
        var futureBoard = (int[,])board.Clone();
        MarkCellsForRemovalInSimulation(futureBoard, opponentID);

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (futureBoard[r, c] == -1)
                {
                    futureBoard[r, c] = playerID;
                    if (IsWinningOnBoard(futureBoard, r, c, playerID))
                    {
                        futureBoard[r, c] = -1;
                        Cell cell = BoardManager.Singltone.GetCell(r, c);
                        if (cell.IsEmpty() && IsWinStable(r, c, playerID))
                        {
                            Cell hisOld = GetOldestCell(opponentID);
                            string line = GetLineType(r, c);
                            reason = $"ВЫИГРЫШ В БУДУЩЕМ: после исчезновения ({hisOld?.row},{hisOld?.coll}) победим по {line}. Ставим в ({r},{c}).";
                            return cell;
                        }
                    }
                    futureBoard[r, c] = -1;
                }
            }
        }
        return null;
    }

    private Cell FindThreatAfterOurRemoval(int playerID, int opponentID, out string reason)
    {
        reason = "";
        var board = BoardManager.Singltone.GetBoardState();
        var weakenedBoard = (int[,])board.Clone();
        MarkCellsForRemovalInSimulation(weakenedBoard, playerID);

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (weakenedBoard[r, c] == -1)
                {
                    weakenedBoard[r, c] = opponentID;
                    if (IsWinningOnBoard(weakenedBoard, r, c, opponentID))
                    {
                        weakenedBoard[r, c] = -1;
                        Cell cell = BoardManager.Singltone.GetCell(r, c);
                        if (cell.IsEmpty() && IsThreatReal(r, c, opponentID))
                        {
                            Cell ourOld = GetOldestCell(playerID);
                            string line = GetLineType(r, c);
                            reason = $"ОПАСНОСТЬ: после исчезновения нашей клетки ({ourOld?.row},{ourOld?.coll}) противник победит по {line}. Блокируем ({r},{c}).";
                            return cell;
                        }
                    }
                    weakenedBoard[r, c] = -1;
                }
            }
        }
        return null;
    }

    private Cell FindAttackOnWeakCell(int playerID, int opponentID, out string reason)
    {
        reason = "";
        var history = GameManager.Singletone.CellHistoryManager.CellHistory;
        if (!history.ContainsKey(opponentID)) return null;

        foreach (Cell weakCell in history[opponentID])
        {
            if (weakCell == null) continue;
            var lines = GetLinesThrough(weakCell.row, weakCell.coll);
            foreach (var (cells, lineType) in lines)
            {
                int ourCount = 0;
                Cell emptyCell = null;
                bool hasEnemyOtherThanWeak = false;

                foreach (Cell cell in cells)
                {
                    if (cell == weakCell) continue;
                    if (cell.IsFillCell)
                    {
                        if (cell.IndexPlayer == playerID) ourCount++;
                        else hasEnemyOtherThanWeak = true;
                    }
                    else emptyCell = cell;
                }

                if (ourCount == 2 && emptyCell != null && !hasEnemyOtherThanWeak && IsWinStable(weakCell.row, weakCell.coll, playerID))
                {
                    reason = $"АТАКА: атакуем слабую клетку ({weakCell.row},{weakCell.coll}) — после исчезновения победим по {lineType}. Ставим в ({emptyCell.row},{emptyCell.coll}).";
                    return emptyCell;
                }
            }
        }
        return null;
    }

    private Cell FindEarlyDefenseAgainstWeakAttack(int playerID, int opponentID, out string reason)
    {
        reason = "";
        var history = GameManager.Singletone.CellHistoryManager.CellHistory;

        // === 1. Ищем НАШУ клетку, поставленную 1 ход назад (index=1) ===
        if (!history.ContainsKey(playerID) || history[playerID].Count < 2) return null;
        Cell weakOurCell = history[playerID][1]; // index=1 → исчезнет на следующем ходу
        if (weakOurCell == null || !weakOurCell.IsFillCell) return null;

        Debug.Log($"[Бот] Проверяем раннюю защиту: наша слабая клетка — ({weakOurCell.row},{weakOurCell.coll}) (index=1)");

        var lines = GetLinesThrough(weakOurCell.row, weakOurCell.coll);
        foreach (var (cells, lineType) in lines)
        {
            List<Cell> opponentCells = new List<Cell>();
            List<Cell> emptyCells = new List<Cell>();

            foreach (Cell cell in cells)
            {
                if (cell == weakOurCell) continue;

                if (cell.IsFillCell)
                {
                    if (cell.IndexPlayer == opponentID)
                    {
                        opponentCells.Add(cell);
                    }
                }
                else
                {
                    emptyCells.Add(cell);
                }
            }

            // === 2. Должна быть ровно ОДНА клетка противника в линии ===
            if (opponentCells.Count != 1) continue;
            Cell oppCell = opponentCells[0];

            // === 3. Эта клетка должна быть поставлена ТОЛЬКО ЧТО (index=0) ===
            if (!IsCellAtIndex(opponentID, oppCell, 0))
            {
                Debug.Log($"[Бот] Пропускаем: его клетка ({oppCell.row},{oppCell.coll}) не index=0 (возможно, исчезнет)");
                continue;
            }

            // === 4. Должна быть одна пустая клетка (куда он поставит на следующем ходу) ===
            if (emptyCells.Count != 1) continue;
            Cell targetEmpty = emptyCells[0];

            // === 5. Проверим, что его клетка НЕ исчезнет до нужного момента ===
            if (IsCellMarkedForRemoval(oppCell, opponentID))
            {
                Debug.Log($"[Бот] Пропускаем: его атакующая клетка ({oppCell.row},{oppCell.coll}) исчезнет до нужного хода");
                continue;
            }

            // === УГРОЗА РЕАЛЬНА! ===
            // Попробуем атакующую защиту
            Cell aggressiveMove = FindAggressiveCounterMove(playerID, opponentID, weakOurCell, cells, lineType, new List<Cell> { targetEmpty }, out string attackReason);
            if (aggressiveMove != null)
            {
                reason = attackReason;
                return aggressiveMove;
            }

            // Или просто заблокируем
            reason = $"РЕАЛЬНАЯ УГРОЗА: игрок атакует нашу слабую клетку ({weakOurCell.row},{weakOurCell.coll}) по {lineType}. Блокируем ({targetEmpty.row},{targetEmpty.coll}) чтобы сорвать план.";
            return targetEmpty;
        }

        return null;
    }
    private Cell FindAggressiveCounterMove(int playerID, int opponentID, Cell weakCell, Cell[] lineCells, string lineType, List<Cell> emptyCells, out string reason)
    {
        reason = "";

        // Попробуем поставить в линию, но так, чтобы **сами начать угрожать**
        foreach (Cell cell in emptyCells)
        {
            // Проверим, если мы поставим сюда — можем ли мы **создать угрозу**?
            var board = BoardManager.Singltone.GetBoardState();
            board[cell.row, cell.coll] = playerID;

            if (IsWinningOnBoard(board, cell.row, cell.coll, playerID))
            {
                // Угроза победы — отлично!
                reason = $"АТАКУЮЩАЯ ЗАЩИТА: игрок атакует ({weakCell.row},{weakCell.coll}), но мы ставим в ({cell.row},{cell.coll}) и сами угрожаем победой по {GetLineType(cell.row, cell.coll)}.";
                return cell;
            }

            // Или хотя бы начать строить линию к его слабой клетке
            Cell hisWeak = GetOldestCell(opponentID); // Его исчезающая клетка
            if (hisWeak != null)
            {
                var hisLines = GetLinesThrough(hisWeak.row, hisWeak.coll);
                foreach (var (cells, hisLineType) in hisLines)
                {
                    int ourCount = 0;
                    Cell emptyInHisLine = null;
                    bool hasEnemyOther = false;

                    foreach (Cell c in cells)
                    {
                        if (c == hisWeak) continue;
                        if (c.IsFillCell)
                        {
                            if (c.IndexPlayer == playerID) ourCount++;
                            else if (c.IndexPlayer == opponentID) hasEnemyOther = true;
                        }
                        else if (c == cell)
                        {
                            emptyInHisLine = c;
                        }
                    }

                    if (ourCount == 1 && emptyInHisLine != null && !hasEnemyOther)
                    {
                        reason = $"АТАКУЮЩАЯ ЗАЩИТА: отвечаем на атаку на ({weakCell.row},{weakCell.coll}) ходом в ({cell.row},{cell.coll}), начиная атаку на его слабую клетку ({hisWeak.row},{hisWeak.coll}).";
                        return cell;
                    }
                }
            }
        }

        return null;
    }

    private bool IsCellAtIndex(int playerID, Cell cell, int index)
    {
        if (!GameManager.Singletone.CellHistoryManager.CellHistory.TryGetValue(playerID, out List<Cell> history))
            return false;
        if (history.Count <= index) return false;
        return history[index] == cell;
    }


    //Бот: Выбор позиции (углы, стороны)
    private Cell GetBestAvailableCorner(out string reason)
    {
        List<(int r, int c)> corners = new List<(int, int)> { (0, 0), (0, 2), (2, 0), (2, 2) };
        for (int i = corners.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (corners[i], corners[j]) = (corners[j], corners[i]);
        }

        foreach (var (r, c) in corners)
        {
            Cell cell = BoardManager.Singltone.GetCell(r, c);
            if (cell != null && cell.IsEmpty())
            {
                reason = $"Угол: ставим в ({r},{c}) — сильная позиция.";
                return cell;
            }
        }
        reason = "";
        return null;
    }

    private Cell GetBestThreateningCorner(int playerID, out string reason)
    {
        reason = "";
        int[,] corners = { { 0, 0 }, { 0, 2 }, { 2, 0 }, { 2, 2 } };
        List<(int r, int c)> threateningCorners = new List<(int, int)>();
        List<(int r, int c)> safeCorners = new List<(int, int)>();

        for (int i = 0; i < 4; i++)
        {
            int r = corners[i, 0];
            int c = corners[i, 1];
            Cell cell = BoardManager.Singltone.GetCell(r, c);
            if (cell == null || !cell.IsEmpty()) continue;

            // Проверим, создаёт ли ход в этот угол угрозу победы
            var board = BoardManager.Singltone.GetBoardState();
            board[r, c] = playerID;

            if (IsWinningOnBoard(board, r, c, playerID))
            {
                threateningCorners.Add((r, c));
            }
            else
            {
                safeCorners.Add((r, c));
            }
        }

        // Сначала — угрожающие углы
        if (threateningCorners.Count > 0)
        {
            // Перемешиваем, чтобы не всегда выбирать первый
            ShuffleList(threateningCorners);
            var (r, c) = threateningCorners[0];
            reason = $"УГРОЗА ИЗ УГЛА: ставим в ({r},{c}) — создаём немедленную угрозу победы.";
            return BoardManager.Singltone.GetCell(r, c);
        }

        // Иначе — рандомный безопасный угол
        if (safeCorners.Count > 0)
        {
            ShuffleList(safeCorners);
            var (r, c) = safeCorners[0];
            reason = $"Угол: ставим в ({r},{c}) — сильная позиция.";
            return BoardManager.Singltone.GetCell(r, c);
        }

        reason = "";
        return null;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}