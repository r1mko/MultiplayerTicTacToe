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
        foreach (var kvp in CellHistory)
        {
            List<Cell> history = kvp.Value;

            if (history.Count >= 3)
            {
                Cell cellToMark = history[2];
                if (cellToMark != null && !cellToMark.IsMarkedForDestruction)
                {
                    cellToMark.MarkForDestruction(true);
                }
            }

            if (history.Count >= 4)
            {
                Cell cellToRemove = history[3];
                if (cellToRemove != null)
                {
                    cellToRemove.MarkForDestruction(false);
                    cellToRemove.Clear();
                    cellToRemove.Unblock();
                }
                history.RemoveAt(3);
            }
        }
    }

    public void SkipTurn(int playerID)
    {
        Add(null, playerID); 
    }

    public void Clear()
    {
        CellHistory = new Dictionary<int, List<Cell>>();
    }

}