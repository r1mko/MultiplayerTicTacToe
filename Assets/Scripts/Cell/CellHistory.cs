using System.Collections.Generic;
using UnityEngine;

public class CellHistoryManager
{
    public Dictionary<int, List<Cell>> CellHistory = new Dictionary<int, List<Cell>>();

    public void EnsurePlayerExists(int playerID)
    {
        if (!CellHistory.ContainsKey(playerID))
        {
            CellHistory[playerID] = new List<Cell>();
        }
    }

    public void AddMove(Cell cell, int playerID)
    {
        if (!CellHistory.ContainsKey(playerID))
        {
            CellHistory.Add(playerID, new List<Cell>());
        }
        CellHistory[playerID].Insert(0, cell);
        CheckCellHistory();
    }

    public void AddMoveToBothPlayers(Cell cell)
    {
        for (int playerId = 0; playerId <= 1; playerId++)
        {
            if (!CellHistory.ContainsKey(playerId))
            {
                CellHistory.Add(playerId, new List<Cell>());
            }
            CellHistory[playerId].Insert(0, cell);
        }

        CheckCellHistory();
    }


    public void CheckCellHistory()
    {
        foreach (var item in CellHistory)
        {
            if (item.Value.Count >= 3)
            {
                if (item.Value[2] != null)
                {
                    item.Value[2].PreDestroy();
                    item.Value[2].MarkForDestruction(true);
                }

                if (item.Value.Count == 4)
                {
                    if (item.Value[3] != null)
                    {
                        item.Value[3].Clear();
                        item.Value[3].Unblock();
                        item.Value[3].MarkForDestruction(false);
                    }
                    item.Value.RemoveAt(3);
                }
            }
        }
    }

    public void SkipTurn(int playerID)
    {
        AddMove(null, playerID); 
    }

    public void Clear()
    {
        CellHistory = new Dictionary<int, List<Cell>>();
    }

    public void RemoveMoveFromAnyPlayer(Cell cell)
    {
        foreach (var list in CellHistory.Values)
        {
            if (list.Contains(cell))
            {
                list.Remove(cell);
                break;
            }
        }
    }

    public void RemoveMoveFromPlayer(Cell cell, int playerID)
    {
        if (CellHistory.ContainsKey(playerID) && CellHistory[playerID].Contains(cell))
        {
            CellHistory[playerID].Remove(cell);
            Debug.Log($"[CellHistory] Удалена ячейка ({cell.row}, {cell.coll}) из истории игрока {playerID}.");
        }
        else
        {
            Debug.LogWarning($"[CellHistory] Ячейка ({cell.row}, {cell.coll}) не найдена в истории игрока {playerID} для удаления.");
        }
    }
}