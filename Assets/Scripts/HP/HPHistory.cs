using System.Collections.Generic;
using UnityEngine;

public class HPHistoryManager
{
    public Dictionary<int, int> HPHistory = new Dictionary<int, int>();
    private int defaultHP = 15;
    private int defaultDamage = 3;

    public void Damage(int playerID)
    {
        if (HPHistory.ContainsKey(playerID))
        {
            HPHistory[playerID] -= defaultDamage;
            Debug.Log($"У игрока с айди {playerID} осталось хп: {HPHistory[playerID]}");
        }
    }
    public int GetHP(int playerID)
    {
        return HPHistory.TryGetValue(playerID, out int hp) ? hp : defaultHP;
    }

    public bool LosePlayer(int playerID)
    {
        if (HPHistory.ContainsKey(playerID))
        {
            if (HPHistory[playerID] <= 0)
            {
                return true;
            }
        }
        return false;
    }

    public void ResetPlayersHP()
    {
        HPHistory = new Dictionary<int, int>();
        HPHistory.Add(0, defaultHP);
        HPHistory.Add(1, defaultHP);
    }
}
