using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityUtils;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer Singletone;

    public int CurrentPlayerTurnID;

    private int startOffSet;



    private void Awake()
    {
        Singletone = this;
        NetworkManager.OnClientConnectedCallback += ClientConnected;
    }

    private void ClientConnected(ulong clientID)
    {
        Debug.Log($"[NetworkPlayer] Вызвали метод ClientConnected. ID: {clientID}. Сервер ли у нас {IsServer}, Хост ли у нас {IsHost}");
        CheckTwoPlayers();
    }

    private void UpdateCurrentPlayerID(int clientID)
    {
        CurrentPlayerTurnID = (int)NetworkManager.ConnectedClientsIds[clientID];
        UIManager.Singletone.UpdateCurrentPlayer(clientID);
    }

    private void CheckTwoPlayers()
    {
        if (!IsServer)
        {
            return;
        }

        if (NetworkManager.ConnectedClientsIds.Count == 2)
        {
            PrepareGame();
        }
    }

    private void PrepareGame()
    {
        var randomIndex = Random.Range(0, NetworkManager.ConnectedClientsIds.Count);
        UpdateOffSetRpc(randomIndex);
        Debug.Log($"[NetworkPlayer] Вызвали PrepareGame. randomIndex равен {randomIndex}");
    }

    [Rpc(SendTo.Everyone)]
    public void OnClickRpc(int row, int col)
    {
        Debug.Log($"[NetworkPlayer] Row {row}, col {col}");
        BoardManager.Singltone.FillCell(row, col, CurrentPlayerTurnID);
        GameManager.Singltone.NextTurn();

        if (BoardManager.Singltone.IsWon(row,col))
        {
            GameOver();
            UIManager.Singletone.SetWinText(CurrentPlayerTurnID.ToString());
            return;
        }

        if (BoardManager.Singltone.IsGameDraw())
        {
            GameOver();
            UIManager.Singletone.SetDrawText("It's a draw!");
            return;
        }

        if (IsServer)
        {
            var playersCounts = NetworkManager.ConnectedClientsIds.Count;
            var currentPlayer = (GameManager.Singltone.TurnIndex + startOffSet) % playersCounts;
            UpdateCurrentPlayerIDRpc(currentPlayer);
            Debug.Log($"[NetworkPlayer] CurrentPlayer {currentPlayer}");
        }
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateCurrentPlayerIDRpc(int clientID)
    {
        UpdateCurrentPlayerID(clientID);
    }

    private void GameOver()
    {
        BoardManager.Singltone.BlockAllButtons();
        UIManager.Singletone.ShowRestartButton();
    }

    public void RestartGame()
    {
        BoardManager.Singltone.ClearAndBloackCells();
        GameManager.Singltone.Restart();
        if (IsServer)
        {
            PrepareGame();
        }
    }

    [Rpc(SendTo.Everyone)]
    public void RestartGameRpc()
    {
        RestartGame();
    }

    [Rpc(SendTo.Everyone)]
    private void UpdateOffSetRpc(int clientID)
    {
        startOffSet = clientID;
        UpdateCurrentPlayerID(clientID);
        UIManager.Singletone.HideRestartButton();
    }
}
