using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Singltone;

    private int[] wins = new int[]{0,0};


    private void Awake()
    {
        Singltone = this;
    }


    public int TurnIndex;

    public void NextTurn()
    {
        TurnIndex++;
    }

    public bool IsOurTurn()
    {
        return NetworkPlayer.Singletone.CurrentPlayerTurnID == (int)NetworkPlayer.Singletone.NetworkManager.LocalClientId;
    }

    public void Restart()
    {
        TurnIndex = 0;
    }

    public void SetWin(int winnerID)
    {
        wins[winnerID]++;
        UIManager.Singletone.SetWinLoseCountText(wins);
    }


}
