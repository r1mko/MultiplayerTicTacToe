using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityUtils;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer Singletone;

    public ulong CurrentPlayerTurnID;



    private void Awake()
    {
        Singletone = this;
    }

    private void Start()
    {
        StartCoroutine(WaitTwoPlayers());
    }

    IEnumerator WaitTwoPlayers()
    {
        if (IsServer)
        {
            yield return new WaitUntil(() => NetworkManager.ConnectedClientsIds.Count == 2);
            PrepareGame();
        }
    }

    private void PrepareGame()
    {
        var randomIndex = Random.Range(0, NetworkManager.ConnectedClientsIds.Count);
        CurrentPlayerTurnID = NetworkManager.ConnectedClientsIds[randomIndex];
        Debug.Log($"[NetworkPlayer] Вызвали PrepareGame. randomIndex равен {randomIndex}");
    }

    [Rpc(SendTo.Everyone)]
    public void OnClickRpc(int row, int col)
    {
        Debug.Log($"[NetworkPlayer] Row {row}, col {col}");
        BoardManager.Singltone.FillCell(row, col);
        GameManager.Singltone.NextTurn();

        if (IsServer)
        {
            var playersCounts = NetworkManager.ConnectedClientsIds.Count;
            var currentPlayer = GameManager.Singltone.TurnIndex % playersCounts;
            CurrentPlayerTurnID = NetworkManager.ConnectedClientsIds[currentPlayer];
            Debug.Log($"[NetworkPlayer] CurrentPlayer {currentPlayer}");
        }

    }


}
