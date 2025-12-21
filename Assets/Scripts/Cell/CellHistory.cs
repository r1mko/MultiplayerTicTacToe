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
        cell.SetCell(turnIndex);
        CheckCellHistory(turnIndex);
    }

    public void CheckCellHistory(int turnIndex)
    {
        foreach (var item in CellHistory)
        {
            foreach (var cell in item.Value)
            {
                cell.CheckCellState(turnIndex);
            }

            if (item.Value.Count >= 3)
            {
                //if (item.Value[2] != null)
                //{
                //    item.Value[2].PreDestroy();
                //    item.Value[2].MarkForDestruction(true);
                //}

                if (item.Value.Count == 4)
                {
                    if (item.Value[3] != null)
                    {
                        item.Value[3].Clear();
                        item.Value[3].Unblock();
                        //item.Value[3].MarkForDestruction(false);
                    }
                    item.Value.RemoveAt(3);
                }
            }
        }
    }

    //to do
    public void SkipTurn(int playerID)
    {
        AddMove(null, playerID, -1); 
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