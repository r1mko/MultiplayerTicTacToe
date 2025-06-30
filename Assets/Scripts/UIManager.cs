using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Singletone;
    [SerializeField] private TMP_Text currentPlayerTextID;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        Singletone = this;
        restartButton.onClick.AddListener(OnRestart);
    }

    private void OnRestart()
    {
        NetworkPlayer.Singletone.RestartGameRpc();
    }

    public void UpdateCurrentPlayer(int id)
    {
        currentPlayerTextID.text = $"Turn Index: {id}";
    }

    public void SetWinText(string text)
    {
        currentPlayerTextID.text = $"Won: {text}";
    }

    public void ShowRestartButton()
    {
        restartButton.gameObject.SetActive(true);
    }

    public void HideRestartButton()
    {
        restartButton.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        restartButton.onClick.RemoveAllListeners();
    }
}
