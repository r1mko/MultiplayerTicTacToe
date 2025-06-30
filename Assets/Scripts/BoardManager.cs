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

    public void FillCell(int row, int col)
    {
        buttons[row, col].Fill();
    }

    public bool IsWon(int row, int column)
    {
        TextMeshProUGUI clickedButtonText = buttons[row, column].GetComponentInChildren<TextMeshProUGUI>();

        //проверяем столбцы
        if (buttons[0, column].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text &&
            buttons[1, column].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text &&
            buttons[2, column].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text)
        {
            return true;
        }

        //проверяем ряды
        else if (buttons[row, 0].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text &&
                 buttons[row, 1].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text &&
                 buttons[row, 2].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text)
        {
            return true;
        }

        //проверяем первую диагональ
        else if (buttons[0, 0].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text &&
                 buttons[1, 1].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text &&
                 buttons[2, 2].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text)
        {
            return true;
        }

        //проверяем вторую диагональ
        else if (buttons[0, 2].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text &&
                 buttons[1, 1].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text &&
                 buttons[2, 0].GetComponentInChildren<TextMeshProUGUI>().text == clickedButtonText.text)
        {
            return true;
        }

        return false;
    }

    private bool IsGameDraw()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (buttons[i, j].GetComponentInChildren<TextMeshProUGUI>().text != "X" &&
                    buttons[i, j].GetComponentInChildren<TextMeshProUGUI>().text != "O")
                {
                    Debug.Log("Проверяем у нас ничья? Нет");
                    return false;
                }
            }
        }
        Debug.Log("Проверяем у нас ничья? Да");
        return true;
    }


}