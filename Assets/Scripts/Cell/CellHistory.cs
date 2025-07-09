using System.Collections.Generic;
using UnityEngine;

public class CellHistoryManager
{
    public Dictionary<int, List<Cell>> CellHistory = new Dictionary<int, List<Cell>>();

    public void Add(Cell cell, int playerID)
    {
        if (!CellHistory.ContainsKey(playerID))
        {
            CellHistory.Add(playerID, new List<Cell>());
        }
        CellHistory[playerID].Insert(0, cell);
        CheckCellHistory();
    }

    public void CheckCellHistory()
    {
        foreach (var item in CellHistory)
        {
            if (item.Value.Count < 3)
            {
                return;
            }

            item.Value[2].PreDestroy();

            if (item.Value.Count == 4)
            {
                item.Value[3].Clear();
                item.Value[3].Unblock();
                item.Value.RemoveAt(3);
            }
        }
    }

    public void Clear()
    {
        CellHistory = new Dictionary<int, List<Cell>>();
    }

}