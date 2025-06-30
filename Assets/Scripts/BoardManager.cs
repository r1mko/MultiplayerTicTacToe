using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
public class BoardManager : MonoBehaviour
{
    public static BoardManager Singltone;

    private void Awake()
    {
        Singltone = this;
    }

    Cell[,] buttons = new Cell[3, 3];
    private void Start() //метод срабатывает как будет создан объект. типа start, но синхронизирован, а не локален
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
    }

    public void BlockAllButtons()
    {
        foreach (var item in buttons)
        {
            item.Block();
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

    public void ClearAndBloackCells()
    {
        foreach (var item in buttons)
        {
            item.Clear();
            item.Unblock();
        }
    }
}