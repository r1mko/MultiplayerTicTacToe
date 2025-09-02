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
        Dictionary<Cell, Cell> cellRemap = new Dictionary<Cell, Cell>();

        for (int j = 0; j < 3; j++)
        {
            List<Cell> columnCells = new List<Cell>();

            // Собираем заполненные клетки сверху вниз (по возрастанию row)
            for (int i = 0; i < 3; i++)
            {
                if (buttons[i, j].IsFillCell)
                {
                    columnCells.Add(buttons[i, j]);
                }
            }

            if (columnCells.Count == 0) continue;

            // Проверяем, нужно ли двигать
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

            // Очищаем столбец
            for (int i = 0; i < 3; i++)
            {
                buttons[i, j].Clear();
            }

            // Заполняем снизу вверх, начиная с самой нижней фишки
            int fillRow = 2;
            for (int k = columnCells.Count - 1; k >= 0; k--)
            {
                Cell cell = columnCells[k];
                Cell targetCell = buttons[fillRow, j];
                targetCell.Fill(cell.IndexPlayer); // ← сохраняем оригинального владельца

                if (cell.row != fillRow)
                {
                    cellRemap[cell] = targetCell;
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

        Debug.Log("Гравитация: фишки упали. Обновляем историю...");

        // Обновляем CellHistory
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

        GameManager.Singletone.cellHistoryManager.CheckCellHistory();

        // Проверка победы
        foreach (var (oldCell, newCell) in cellRemap)
        {
            if (IsRow(newCell.row, newCell.coll))
            {
                Debug.Log($"Игрок {newCell.IndexPlayer} победил после гравитации!");
            }
        }
    }
}