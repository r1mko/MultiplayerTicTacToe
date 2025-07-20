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

    private void Awake()
    {
        Singltone = this;
        ShowBoard();
    }

    Cell[,] buttons = new Cell[3, 3];
    private void Start()
    {
        Debug.Log($"Count of buttons.length is {buttons.Length}");

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
        //HideBoard(); //позже добавим
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
        Debug.Log($"Вызвали onClickCell row: {row} coll: {coll} is our turn? {GameManager.Singltone.IsOurTurn()}");
        if (!GameManager.Singltone.IsOurTurn())
        {
            return;
        }

        if (NetworkPlayer.Singletone.IsMultiplayer())
        {
            NetworkPlayer.Singletone.OnClickRpc(row, coll);
        }
        else
        {
            GameManager.Singltone.OnClick(row, coll);
        }

    }

    public void FillCell(int row, int col, int currentPlayerIndex)
    {
        buttons[row, col].Fill(currentPlayerIndex);
    }

    public bool IsWon(int row, int column)
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
                    Debug.Log("Проверяем у нас ничья? Нет");
                    return false;
                }
            }
        }
        Debug.Log("Проверяем у нас ничья? Да");
        return true;
    }

    public void ClearAndUnbloackCells()
    {
        Debug.Log("[BoardManager] Вызвали ClearAndUnbloackCells");
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
}