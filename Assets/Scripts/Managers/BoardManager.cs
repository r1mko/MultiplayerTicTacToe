using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
public class BoardManager : MonoBehaviour
{
    [SerializeField] private GameObject board;

    public static BoardManager Singltone;

    Cell[,] buttons = new Cell[3, 3];

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
        int indexPlayer = buttons[row, column].IndexPlayer;

        //проверяем столбцы
        if (buttons[0, column].IsSameCell(indexPlayer) &&
            buttons[1, column].IsSameCell(indexPlayer) &&
            buttons[2, column].IsSameCell(indexPlayer))
        {
            return true;
        }

        //проверяем ряды
        else if (buttons[row, 0].IsSameCell(indexPlayer) &&
                 buttons[row, 1].IsSameCell(indexPlayer) &&
                 buttons[row, 2].IsSameCell(indexPlayer))
        {
            return true;
        }

        //проверяем первую диагональ
        else if (buttons[0, 0].IsSameCell(indexPlayer) &&
                 buttons[1, 1].IsSameCell(indexPlayer) &&
                 buttons[2, 2].IsSameCell(indexPlayer))
        {
            return true;
        }

        //проверяем вторую диагональ
        else if (buttons[0, 2].IsSameCell(indexPlayer) &&
                 buttons[1, 1].IsSameCell(indexPlayer) &&
                 buttons[2, 0].IsSameCell(indexPlayer))
        {
            return true;
        }

        return false;
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
        bool changes = false;
        List<(int row, int col, int playerIndex)> filledCells = new List<(int, int, int)>();

        for (int j = 0; j < 3; j++)
        {
            List<int> columnValues = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                if (buttons[i, j].IsFillCell)
                {
                    columnValues.Add(buttons[i, j].IndexPlayer);
                }
            }

            int fillIndex = 2;
            for (int k = columnValues.Count - 1; k >= 0; k--)
            {
                if (fillIndex >= 0)
                {
                    int oldIndex = fillIndex + 1;
                    if (oldIndex <= 2 && buttons[oldIndex, j].IsFillCell && buttons[oldIndex, j].IndexPlayer == columnValues[k])
                    {
                        // Тот же игрок, но на новом месте
                        if (buttons[fillIndex, j].IsFillCell && buttons[fillIndex, j].IndexPlayer != columnValues[k])
                        {
                            changes = true;
                        }
                    }
                    else if (!buttons[fillIndex, j].IsFillCell || buttons[fillIndex, j].IndexPlayer != columnValues[k])
                    {
                        changes = true;
                    }

                    buttons[fillIndex, j].Fill(columnValues[k]);
                    filledCells.Add((fillIndex, j, columnValues[k]));
                    fillIndex--;
                }
            }

            while (fillIndex >= 0)
            {
                if (buttons[fillIndex, j].IsFillCell)
                {
                    buttons[fillIndex, j].Clear();
                    changes = true;
                }
                fillIndex--;
            }
        }

        if (changes)
        {
            Debug.Log("Гравитация применена.");

            // Проверка победы после падения (опционально!)
            foreach (var (row, col, playerIndex) in filledCells)
            {
                if (IsRow(row, col))
                {
                    Debug.Log($"Игрок {playerIndex} победил после гравитации!");
                    // Можно вызвать GameOver, но это зависит от логики
                    // Пока не вызываем — ход передается дальше
                }
            }
        }
    }
}