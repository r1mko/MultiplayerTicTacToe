using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinmaxBot : MonoBehaviour
{
    private Coroutine botTurnCoroutine;
    public int maxDepth = 8;

    void Update()
    {
        if (NetworkPlayer.Singletone.IsMultiplayer()) return;
        if (!GameManager.Singletone.IsPlaying) return;
        if (!GameManager.Singletone.IsBotTurn()) return;
        if (botTurnCoroutine != null) return;

        botTurnCoroutine = StartCoroutine(BotTurn());
    }

    private IEnumerator BotTurn()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.2f));

        Cell bestMove = FindBestMove();
        if (bestMove != null)
        {
            Debug.Log($"[MinimaxBot] Выбираем ход: ({bestMove.row},{bestMove.coll})");
            GameManager.Singletone.OnClick(bestMove.row, bestMove.coll);
        }
        else
        {
            Debug.Log("[MinimaxBot] Нет доступных ходов.");
        }

        botTurnCoroutine = null;
    }

    private Cell FindBestMove()
    {
        int playerID = GameManager.Singletone.CurrentPlayerTurnID;
        int opponentID = 1 - playerID;

        var board = BoardManager.Singltone.GetBoardState();
        var history = CloneHistory(GameManager.Singletone.CellHistoryManager.CellHistory);

        int bestScore = int.MinValue;
        List<Cell> bestMoves = new List<Cell>(); // Список всех лучших ходов

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (board[r, c] == -1)
                {
                    // Симулируем ход
                    var newBoard = (int[,])board.Clone();
                    var newHistory = CloneHistory(history);

                    RemoveOldestCell(newBoard, newHistory, playerID);
                    newBoard[r, c] = playerID;
                    Cell cell = BoardManager.Singltone.GetCell(r, c);
                    newHistory[playerID].Insert(0, cell);

                    // Оцениваем позицию
                    int score = Minimax(newBoard, newHistory, 0, false, playerID, opponentID, int.MinValue, int.MaxValue);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMoves.Clear();
                        bestMoves.Add(cell);
                    }
                    else if (score == bestScore)
                    {
                        bestMoves.Add(cell);
                    }
                }
            }
        }

        // Если есть несколько равных лучших ходов — выбираем случайный
        if (bestMoves.Count > 0)
        {
            Cell chosenMove = bestMoves[UnityEngine.Random.Range(0, bestMoves.Count)];
            Debug.Log($"[MinimaxBot] Выбираем случайный лучший ход из {bestMoves.Count} вариантов: ({chosenMove.row},{chosenMove.coll})");
            return chosenMove;
        }

        Debug.Log("[MinimaxBot] Нет доступных ходов.");
        return null;
    }

    private int Minimax(int[,] board, Dictionary<int, List<Cell>> history, int depth, bool isBotTurn, int botID, int playerID, int alpha, int beta)
    {
        // Базовые случаи
        if (IsWin(board, botID)) return +10 - depth;
        if (IsWin(board, playerID)) return -10 + depth;
        if (IsDraw(board) || depth >= maxDepth) return EvaluatePosition(board, history, botID);

        if (isBotTurn)
        {
            int maxEval = int.MinValue;
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (board[r, c] == -1)
                    {
                        var newBoard = (int[,])board.Clone();
                        var newHistory = CloneHistory(history);
                        int currID = botID;

                        RemoveOldestCell(newBoard, newHistory, currID);
                        newBoard[r, c] = currID;
                        Cell cell = BoardManager.Singltone.GetCell(r, c);
                        newHistory[currID].Insert(0, cell);

                        int eval = Minimax(newBoard, newHistory, depth + 1, false, botID, playerID, alpha, beta);
                        maxEval = Math.Max(maxEval, eval);
                        alpha = Math.Max(alpha, eval);
                        if (beta <= alpha) break;
                    }
                }
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (board[r, c] == -1)
                    {
                        var newBoard = (int[,])board.Clone();
                        var newHistory = CloneHistory(history);
                        int currID = playerID;

                        RemoveOldestCell(newBoard, newHistory, currID);
                        newBoard[r, c] = currID;
                        Cell cell = BoardManager.Singltone.GetCell(r, c);
                        newHistory[currID].Insert(0, cell);

                        int eval = Minimax(newBoard, newHistory, depth + 1, true, botID, playerID, alpha, beta);
                        minEval = Math.Min(minEval, eval);
                        beta = Math.Min(beta, eval);
                        if (beta <= alpha) break;
                    }
                }
            }
            return minEval;
        }
    }

    private bool IsWin(int[,] board, int playerID)
    {
        return
            (board[0, 0] == playerID && board[0, 1] == playerID && board[0, 2] == playerID) ||
            (board[1, 0] == playerID && board[1, 1] == playerID && board[1, 2] == playerID) ||
            (board[2, 0] == playerID && board[2, 1] == playerID && board[2, 2] == playerID) ||
            (board[0, 0] == playerID && board[1, 0] == playerID && board[2, 0] == playerID) ||
            (board[0, 1] == playerID && board[1, 1] == playerID && board[2, 1] == playerID) ||
            (board[0, 2] == playerID && board[1, 2] == playerID && board[2, 2] == playerID) ||
            (board[0, 0] == playerID && board[1, 1] == playerID && board[2, 2] == playerID) ||
            (board[0, 2] == playerID && board[1, 1] == playerID && board[2, 0] == playerID);
    }

    private bool IsDraw(int[,] board)
    {
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                if (board[r, c] == -1) return false;
        return true;
    }

    private int EvaluatePosition(int[,] board, Dictionary<int, List<Cell>> history, int botID)
    {
        int score = 0;
        int playerID = 1 - botID;

        // Центр
        if (board[1, 1] == botID) score += 3;
        else if (board[1, 1] == playerID) score -= 3;

        // Углы
        int[] cornersR = { 0, 0, 2, 2 };
        int[] cornersC = { 0, 2, 0, 2 };
        for (int i = 0; i < 4; i++)
        {
            if (board[cornersR[i], cornersC[i]] == botID) score += 1;
            else if (board[cornersR[i], cornersC[i]] == playerID) score -= 1;
        }

        // Подсчёт угроз
        score += CountThreats(board, botID) * 2;
        score -= CountThreats(board, playerID) * 2;

        return score;
    }

    private int CountThreats(int[,] board, int playerID)
    {
        int count = 0;
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (board[r, c] == -1)
                {
                    board[r, c] = playerID;
                    if (IsWin(board, playerID)) count++;
                    board[r, c] = -1;
                }
            }
        }
        return count;
    }

    private Dictionary<int, List<Cell>> CloneHistory(Dictionary<int, List<Cell>> source)
    {
        var clone = new Dictionary<int, List<Cell>>();

        // Гарантируем, что в клоне есть 0 и 1, НО НЕ МОДИФИЦИРУЕМ source
        if (source.ContainsKey(0))
        {
            clone[0] = new List<Cell>(source[0]);
        }
        else
        {
            clone[0] = new List<Cell>();
        }

        if (source.ContainsKey(1))
        {
            clone[1] = new List<Cell>(source[1]);
        }
        else
        {
            clone[1] = new List<Cell>();
        }

        return clone;
    }

    private void RemoveOldestCell(int[,] board, Dictionary<int, List<Cell>> history, int playerID)
    {
        if (history.TryGetValue(playerID, out List<Cell> list) && list.Count >= 3)
        {
            Cell cellToRemove = list[2];
            if (cellToRemove != null)
            {
                board[cellToRemove.row, cellToRemove.coll] = -1;
            }
            list.RemoveAt(2);
        }
    }
}