using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Singltone;


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
        return NetworkPlayer.Singletone.CurrentPlayerTurnID == NetworkPlayer.Singletone.NetworkManager.LocalClientId;
    }
}
