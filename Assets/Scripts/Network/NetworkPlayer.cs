using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityUtils;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer Singletone;

    private void Awake()
    {
        Singletone = this;
        NetworkManager.OnClientConnectedCallback += ClientConnected;
    }

    public bool IsMultiplayer()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private void ClientConnected(ulong clientID)
    {
        if (CheckTwoPlayers())
        {
            GameManager.Singletone.StartGame();
        }
    }


    private bool CheckTwoPlayers()
    {
        if (!IsServer)
        {
            return false;
        }

        if (NetworkManager.ConnectedClientsIds.Count == 2)
        {
            return true;
        }
        return false;
    }


    //rpc region
    [Rpc(SendTo.Everyone)]
    public void OnClickRpc(int row, int col)
    {
        GameManager.Singletone.OnClick(row, col);
    }

    [Rpc(SendTo.Everyone)]
    public void MoveToNextPlayerRpc()
    {
        Debug.Log("[NetworkPlayer] Вызвали метод MoveToNextPlayerRpc");
        GameManager.Singletone.HandleSkipTurn();
        GameManager.Singletone.PassMoveToNextPlayer();
    }

    [Rpc(SendTo.Everyone)]
    public void SendSmileClientRpc(int smileID, ulong senderId)
    {
        if (senderId == NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        EventManager.Trigger(new EnemySmileReceivedEvent(smileID));
    }

    [Rpc(SendTo.Everyone)]
    public void StartGameRpc()
    {
        Debug.Log("[NetworkPlayer] Вызвали метод StartGameRpc");
        GameManager.Singletone.UpdateUI();
        if (IsServer)
        {
            PrepareGameRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    public void RestartGameRpc()
    {
        Debug.Log("[NetworkPlayer] Вызвали метод RestartGameRpc");

        if (IsServer)
        {
            PrepareGameRpc();
            GameManager.Singletone.Restart();
        }
    }

    [Rpc(SendTo.Everyone)]
    public void PrepareGameRpc()
    {
        GameManager.Singletone.PrepareGame();
        BoardManager.Singltone.ClearAndUnbloackCells();
    }

    [Rpc(SendTo.Everyone)]
    public void UpdateOffSetRpc(int clientID)
    {
        GameManager.Singletone.UpdateOffSet(clientID);
    }
}
