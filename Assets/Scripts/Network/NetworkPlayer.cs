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
        GameManager.Singltone.StartTimer();
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
    private void UpdateCurrentPlayerIDRpc(int clientID)
    {
        GameManager.Singltone.UpdateCurrentPlayerID(clientID);
        GameManager.Singltone.StartTimer();
    }

    [Rpc(SendTo.Everyone)]
    public void RestartGameRpc()
    {
        GameManager.Singltone.RestartGame();
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateOffSetRpc(int clientID)
    {
        GameManager.Singltone.UpdateOffSet(clientID);
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateUIRpc()
    {
        GameManager.Singltone.UpdateUI();
    }
}
