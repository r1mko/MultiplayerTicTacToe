using System.Collections.Generic;
using UnityEngine;

public class CellHistoryManager
{
    public Dictionary<int, List<Cell>> CellHistory = new Dictionary<int, List<Cell>>();

    public void AddMove(Cell cell, int playerID, int turnIndex)
    {
        if (!CellHistory.ContainsKey(playerID))
        {
            CellHistory.Add(playerID, new List<Cell>());
        }
        CellHistory[playerID].Insert(0, cell);
        if (cell != null)
        {
            cell.SetCell(turnIndex);
        }
        CheckCellHistory(turnIndex);
    }

    public void CheckCellHistory(int turnIndex)
    {
        foreach (var kvp in CellHistory)
        {
            var playerCells = kvp.Value;

            var cellsToRemove = new List<Cell>();

            foreach (var cell in playerCells)
            {
                if (cell != null)
                {
                    cell.CheckPreDestroyState(turnIndex);

                    if (cell.CheckDestroyCell(turnIndex))
                    {
                        cellsToRemove.Add(cell);
                    }
                }

            }

            foreach (var cell in cellsToRemove)
            {
                if (cell != null)
                {
                    cell.Clear();
                    cell.Unblock();
                }
                playerCells.Remove(cell);
            }
        }
    }

    public void Clear()
    {
        CellHistory = new Dictionary<int, List<Cell>>();
    }

    public void RemoveMoveFromPlayer(Cell cell, int playerID)
    {
        if (CellHistory.ContainsKey(playerID) && CellHistory[playerID].Contains(cell))
        {
            CellHistory[playerID].Remove(cell);
        }
        else
        {
            Debug.LogWarning($"[CellHistory] Ячейка ({cell.row}, {cell.coll}) не найдена в истории игрока {playerID} для удаления.");
        }
    }

    public void ReplaceCellWithNull(Cell cell, int playerID)
    {
        if (!CellHistory.ContainsKey(playerID)) return;

        var list = CellHistory[playerID];
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == cell)
            {
                list[i] = null;
                return;
            }
        }
    }
}