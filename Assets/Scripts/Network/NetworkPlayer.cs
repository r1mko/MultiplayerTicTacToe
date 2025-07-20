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
        Debug.Log($"[NetworkPlayer] Вызвали метод ClientConnected. ID: {clientID}. Сервер ли у нас {IsServer}, Хост ли у нас {IsHost}");
        if (CheckTwoPlayers())
        {
            GameManager.Singltone.StartGame();
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
        GameManager.Singltone.OnClick(row, col);
    }

    [Rpc(SendTo.Everyone)]
    public void MoveToNextPlayerRpc()
    {
       GameManager.Singltone.MoveToNextPlayer();
    }

    [Rpc(SendTo.Everyone)]
    public void StartGameRpc()
    {
        Debug.Log("[NetworkPlayer] Вызвали метод StartGameRpc");
        GameManager.Singltone.StartTimer();
        GameManager.Singltone.UpdateUI(); //вызывается 2 раза у хоста
    }

    [Rpc(SendTo.Everyone)]
    public void SendSmileClientRpc(int smileID, ulong senderId)
    {
        if (senderId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"[ClientRpc] Это мой собственный смайл — пропускаем.");
            return;
        }

        EventManager.Trigger(new EnemySmileReceivedEvent(smileID));
    }

    [Rpc(SendTo.Everyone)]
    public void UpdateCurrentPlayerIDRpc(int clientID)
    {
        GameManager.Singltone.UpdateCurrentPlayerID(clientID);
        GameManager.Singltone.StartTimer();
    }

    [Rpc(SendTo.Everyone)]
    public void RestartGameRpc()
    {
        if (IsServer)
        {
            var randomIndex = Random.Range(0, NetworkManager.ConnectedClientsIds.Count);
            PrepareGameRpc(randomIndex);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void PrepareGameRpc(int offSet)
    {
        if (IsServer)
        {
            ulong clientId = NetworkManager.ConnectedClientsIds[offSet];
            UpdateCurrentPlayerIDRpc((int)clientId);
        }

        GameManager.Singltone.PrepareGame(offSet);
    }

    [Rpc(SendTo.Everyone)]
    public void UpdateOffSetRpc(int clientID)
    {
        GameManager.Singltone.UpdateOffSet(clientID);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateUIRpc()
    {
        GameManager.Singltone.UpdateUI();
    }
}
