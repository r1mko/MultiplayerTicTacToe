using System;
using System.Collections;
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

        // Ходим только если это ход бота
        if (!GameManager.Singletone.IsBotTurn())
            return;

        if (botTurnCoroutine != null)
            return;

        botTurnCoroutine = StartCoroutine(BotTurn());
    }

    private IEnumerator BotTurn()
    {
        // Имитация "размышления" бота
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        var board = BoardManager.Singltone.GetBoardState();
        int playerID = GameManager.Singletone.CurrentPlayerTurnID;
        int opponentID = 1 - playerID; // Предполагаем, что игроки 0 и 1

        Cell bestCell = null;
        string reason = "";

        // 1. Проверяем: можем ли мы победить?
        bestCell = FindWinningMove(board, playerID);
        if (bestCell != null)
        {
            reason = $"Побеждаем: ставим в ({bestCell.row}, {bestCell.coll}) чтобы выиграть.";
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // 2. Можем ли мы проиграть? Нужно блокировать!
        bestCell = FindWinningMove(board, opponentID);
        if (bestCell != null)
        {
            reason = $"Блокируем: ставим в ({bestCell.row}, {bestCell.coll}) чтобы не проиграть.";
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
            botTurnCoroutine = null;
            yield break;
        }

        // 3. Центр свободен?
        var centerCell = BoardManager.Singltone.GetCell(1, 1);
        if (centerCell.IsEmpty())
        {
            reason = "Центр свободен — стратегически выгодная позиция.";
            Debug.Log($"[Бот] {reason}");
            MakeMove(centerCell);
            botTurnCoroutine = null;
            yield break;
        }

        // 4. Выбираем лучшее из оставшихся: углы > стороны
        bestCell = GetBestAvailableCornerOrSide();
        if (bestCell != null)
        {
            reason = $"Ставим в ({bestCell.row}, {bestCell.coll}) — лучшее доступное место (угол/сторона).";
            Debug.Log($"[Бот] {reason}");
            MakeMove(bestCell);
        }
        else
        {
            // На всякий случай — случайный ход (если всё занято, хотя это маловероятно)
            if (BoardManager.Singltone.TryGetEmptyCell(out Cell fallbackCell))
            {
                reason = $"Резервный ход: ставим в ({fallbackCell.row}, {fallbackCell.coll})";
                Debug.Log($"[Бот] {reason}");
                MakeMove(fallbackCell);
            }
            else
            {
                Debug.Log("[Бот] Нет свободных клеток — игра должна была закончиться.");
            }
        }

        botTurnCoroutine = null;
    }

    private void MakeMove(Cell cell)
    {
        GameManager.Singletone.OnClick(cell.row, cell.coll);
    }

    // Проверяет: есть ли ход, который приведёт к победе для playerID?
    private Cell FindWinningMove(int[,] board, int playerID)
    {
        int size = board.GetLength(0);

        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (board[row, col] == -1) // Свободная клетка
                {
                    // Пробуем поставить
                    board[row, col] = playerID;

                    // Проверяем, выиграем ли мы?
                    bool isWinning = IsWinningMove(row, col, playerID, board);

                    // Откатываем
                    board[row, col] = -1;

                    if (isWinning)
                    {
                        Cell cell = BoardManager.Singltone.GetCell(row, col);
                        if (cell != null && cell.IsEmpty())
                            return cell;
                    }
                }
            }
        }
        return null;
    }

    // Упрощённая проверка победы по конкретной позиции (аналогично твоей IsWon)
    private bool IsWinningMove(int row, int col, int playerID, int[,] board)
    {
        int size = board.GetLength(0);

        // Проверка строки
        bool rowWin = true;
        for (int x = 0; x < size; x++)
            if (board[row, x] != playerID)
                rowWin = false;

        // Проверка столбца
        bool colWin = true;
        for (int y = 0; y < size; y++)
            if (board[y, col] != playerID)
                colWin = false;

        // Диагональ \
        bool diag1Win = true;
        if (row == col)
        {
            for (int i = 0; i < size; i++)
                if (board[i, i] != playerID)
                    diag1Win = false;
        }
        else
        {
            diag1Win = false;
        }

        // Диагональ /
        bool diag2Win = true;
        if (row + col == size - 1)
        {
            for (int i = 0; i < size; i++)
                if (board[i, size - 1 - i] != playerID)
                    diag2Win = false;
        }
        else
        {
            diag2Win = false;
        }

        return rowWin || colWin || diag1Win || diag2Win;
    }

    // Приоритет: углы (0,0), (0,2), (2,0), (2,2) → потом стороны (0,1), (1,0), (1,2), (2,1)
    private Cell GetBestAvailableCornerOrSide()
    {
        int[,] priorityOrder = new int[,]
        {
            {0, 0}, {0, 2}, {2, 0}, {2, 2},  // углы
            {0, 1}, {1, 0}, {1, 2}, {2, 1}   // стороны
        };

        for (int i = 0; i < 8; i++)
        {
            int r = priorityOrder[i, 0];
            int c = priorityOrder[i, 1];
            Cell cell = BoardManager.Singltone.GetCell(r, c);
            if (cell != null && cell.IsEmpty())
            {
                return cell;
            }
        }

        return null;
    }
}