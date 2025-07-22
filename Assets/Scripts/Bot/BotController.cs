using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        yield return new WaitForEndOfFrame(); // Гарантируем актуальность состояния
        yield return StartCoroutine(BotTurn());
    }

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

        // === 3. МОЖЕМ ЛИ ПОБЕДИТЬ, ЕСЛИ ИСЧЕЗНЕТ ЕГО КЛЕТКА? ===
        bestCell = FindFutureWinAfterOpponentRemoval(playerID, opponentID, out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === 4. ОПАСНОСТЬ: ПОСЛЕ ИСЧЕЗНОВЕНИЯ НАШЕЙ КЛЕТКИ ПРОТИВНИК ПОБЕДИТ? ===
        bestCell = FindThreatAfterOurRemoval(playerID, opponentID, out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === 5. ЦЕНТР ===
        var center = BoardManager.Singltone.GetCell(1, 1);
        if (center != null && center.IsEmpty())
        {
            reason = "[Бот] Центр (1,1): стратегически важная позиция.";
            Debug.Log(reason);
            MakeMove(center);
            botTurnCoroutine = null;
            yield break;
        }

        // === 6. УГЛЫ ===
        bestCell = GetBestAvailableCorner(out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // === 7. СТОРОНЫ ===
        bestCell = GetBestAvailableSide(out reason);
        if (bestCell != null)
        {
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // Резерв
        if (BoardManager.Singltone.TryGetEmptyCell(out Cell fallback))
        {
            reason = $"[Бот] Резерв: ставим в ({fallback.row},{fallback.coll}) — больше нет вариантов.";
            Debug.Log(reason);
            MakeMove(fallback);
        }

        botTurnCoroutine = null;
    }

    private void MakeMove(Cell cell)
    {
        GameManager.Singletone.OnClick(cell.row, cell.coll);
    }

    // === ЛОГИ: ПОКАЗЫВАЕМ ВСЁ ===
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

    // === ОСНОВНОЙ МЕТОД: ИЩЕТ ВЫИГРЫШНЫЙ ХОД (с проверкой устойчивости) ===
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
                            // Проверяем, устойчива ли победа
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

    // === ПРОВЕРКА: ПОБЕДА УСТОЙЧИВА? (нет исчезающих клеток в линии) ===
    private bool IsWinStable(int row, int col, int playerID)
    {
        var board = BoardManager.Singltone.GetBoardState();
        board[row, col] = playerID; // Симулируем наш ход

        // Проверяем все линии через (row, col)
        if (IsWinningRow(board, row, playerID))
        {
            for (int c = 0; c < 3; c++)
            {
                if (c == col) continue;
                Cell cell = BoardManager.Singltone.GetCell(row, c);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == playerID)
                {
                    if (IsCellMarkedForRemoval(cell, playerID))
                        return false;
                }
            }
        }
        else if (IsWinningCol(board, col, playerID))
        {
            for (int r = 0; r < 3; r++)
            {
                if (r == row) continue;
                Cell cell = BoardManager.Singltone.GetCell(r, col);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == playerID)
                {
                    if (IsCellMarkedForRemoval(cell, playerID))
                        return false;
                }
            }
        }
        else if (row == col && IsWinningDiag1(board, playerID))
        {
            for (int i = 0; i < 3; i++)
            {
                if (i == row) continue;
                Cell cell = BoardManager.Singltone.GetCell(i, i);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == playerID)
                {
                    if (IsCellMarkedForRemoval(cell, playerID))
                        return false;
                }
            }
        }
        else if (row + col == 2 && IsWinningDiag2(board, playerID))
        {
            int[] diagR = { 0, 1, 2 }, diagC = { 2, 1, 0 };
            for (int i = 0; i < 3; i++)
            {
                if (diagR[i] == row && diagC[i] == col) continue;
                Cell cell = BoardManager.Singltone.GetCell(diagR[i], diagC[i]);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == playerID)
                {
                    if (IsCellMarkedForRemoval(cell, playerID))
                        return false;
                }
            }
        }

        return true;
    }

    // === ПРОВЕРКА: МОЖЕТ ЛИ КЛЕТКА БЫТЬ УДАЛЕНА ===
    private bool IsCellMarkedForRemoval(Cell cell, int playerID)
    {
        if (!GameManager.Singletone.CellHistoryManager.CellHistory.TryGetValue(playerID, out List<Cell> history))
            return false;
        return history.Count >= 3 && history[2] == cell;
    }

    // === 3. МОЖЕМ ПОБЕДИТЬ ПОСЛЕ УДАЛЕНИЯ ЕГО КЛЕТКИ ===
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
                        if (cell.IsEmpty())
                        {
                            // Дополнительно: устойчива ли эта победа?
                            if (IsWinStable(r, c, playerID))
                            {
                                Cell hisOld = GetOldestCell(opponentID);
                                string line = GetLineType(r, c);
                                reason = $"ВЫИГРЫШ В БУДУЩЕМ: после исчезновения ({hisOld?.row},{hisOld?.coll}) победим по {line}. Ставим в ({r},{c}).";
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

    // === 4. ОПАСНОСТЬ: ПОСЛЕ УДАЛЕНИЯ НАШЕЙ КЛЕТКИ ПРОТИВНИК ПОБЕДИТ? ===
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
                        if (cell.IsEmpty())
                        {
                            // Проверяем, реальна ли угроза
                            if (IsThreatReal(r, c, opponentID))
                            {
                                Cell ourOld = GetOldestCell(playerID);
                                string line = GetLineType(r, c);
                                reason = $"ОПАСНОСТЬ: после исчезновения нашей клетки ({ourOld?.row},{ourOld?.coll}) противник победит по {line}. Блокируем ({r},{c}).";
                                return cell;
                            }
                            else
                            {
                                reason = $"[Бот] Угроза в ({r},{c}) НЕ реальна — одна из его клеток исчезнет. Игнорируем.";
                                Debug.Log(reason);
                            }
                        }
                    }
                    weakenedBoard[r, c] = -1;
                }
            }
        }
        return null;
    }

    // === ПРОВЕРКА: УГРОЗА РЕАЛЬНА? ===
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
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == opponentID)
                {
                    if (IsCellMarkedForRemoval(cell, opponentID))
                        return false;
                }
            }
        }
        else if (IsWinningCol(board, col, opponentID))
        {
            for (int r = 0; r < 3; r++)
            {
                if (r == row) continue;
                Cell cell = BoardManager.Singltone.GetCell(r, col);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == opponentID)
                {
                    if (IsCellMarkedForRemoval(cell, opponentID))
                        return false;
                }
            }
        }
        else if (row == col && IsWinningDiag1(board, opponentID))
        {
            for (int i = 0; i < 3; i++)
            {
                if (i == row) continue;
                Cell cell = BoardManager.Singltone.GetCell(i, i);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == opponentID)
                {
                    if (IsCellMarkedForRemoval(cell, opponentID))
                        return false;
                }
            }
        }
        else if (row + col == 2 && IsWinningDiag2(board, opponentID))
        {
            int[] diagR = { 0, 1, 2 }, diagC = { 2, 1, 0 };
            for (int i = 0; i < 3; i++)
            {
                if (diagR[i] == row && diagC[i] == col) continue;
                Cell cell = BoardManager.Singltone.GetCell(diagR[i], diagC[i]);
                if (cell != null && cell.IsFillCell && cell.IndexPlayer == opponentID)
                {
                    if (IsCellMarkedForRemoval(cell, opponentID))
                        return false;
                }
            }
        }

        return true;
    }

    private void MarkCellsForRemovalInSimulation(int[,] simBoard, int playerID)
    {
        Cell cellToRemove = GetOldestCell(playerID);
        if (cellToRemove != null)
        {
            simBoard[cellToRemove.row, cellToRemove.coll] = -1;
        }
    }

    private Cell GetOldestCell(int playerID)
    {
        if (GameManager.Singletone.CellHistoryManager.CellHistory.TryGetValue(playerID, out List<Cell> history))
        {
            if (history.Count >= 3) return history[2];
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

    private Cell GetBestAvailableCorner(out string reason)
    {
        int[,] corners = { { 0, 0 }, { 0, 2 }, { 2, 0 }, { 2, 2 } };
        for (int i = 0; i < 4; i++)
        {
            int r = corners[i, 0];
            int c = corners[i, 1];
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

    private Cell GetBestAvailableSide(out string reason)
    {
        int[,] sides = { { 0, 1 }, { 1, 0 }, { 1, 2 }, { 2, 1 } };
        for (int i = 0; i < 4; i++)
        {
            int r = sides[i, 0];
            int c = sides[i, 1];
            Cell cell = BoardManager.Singltone.GetCell(r, c);
            if (cell != null && cell.IsEmpty())
            {
                reason = $"Сторона: ставим в ({r},{c}) — последний выбор.";
                return cell;
            }
        }
        reason = "";
        return null;
    }
}