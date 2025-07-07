using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Singletone;
    public TMP_Text sessionInfoText;

    [SerializeField] private TMP_Text currentPlayerTextID;
    [SerializeField] private GameObject navigationPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        Singletone = this;
        restartButton.onClick.AddListener(OnRestart);
        hostButton.onClick.AddListener(OnHost);
        clientButton.onClick.AddListener(OnClient);
        HideRestartButton();
        ShowNavigationPanel();
        ShowActiveSessionInfo();
        HideMoveInfo();
    }

    private void OnRestart()
    {
        NetworkPlayer.Singletone.RestartGameRpc();
    }

    private void OnHost()
    {
        HideNavigationPanel();
        SessionManager.Instance.StartSessionAsHostButton();
    }

    private void OnClient()
    {
        HideNavigationPanel();
        SessionManager.Instance.FindAndJoinSessionButton();
    }

    public void UpdateCurrentPlayerText()
    {
        if (GameManager.Singltone.IsOurTurn())
        {
            currentPlayerTextID.text = "Ваш ход";
        }
        else
        {
            currentPlayerTextID.text = "Ход противника";
        }

    }

    public void SetWinText()
    {
        if (GameManager.Singltone.IsOurTurn())
        {
            currentPlayerTextID.text = "Вы победили!";
        }
        else
        {
            currentPlayerTextID.text = "Вы проиграли!";
        }
    }

    public void SetDrawText(string text)
    {
        currentPlayerTextID.text = text;
    }
    public void ShowRestartButton()
    {
        restartButton.gameObject.SetActive(true);
    }

    public void HideRestartButton()
    {
        restartButton.gameObject.SetActive(false);
    }

    public void ShowNavigationPanel()
    {
        navigationPanel.SetActive(true);
    }

    public void HideNavigationPanel()
    {
        navigationPanel.SetActive(false);
    }

    public void ShowActiveSessionInfo()
    {
        sessionInfoText.gameObject.SetActive(true);
    }

    public void HideActiveSessionInfo()
    {
        sessionInfoText.gameObject.SetActive(false);
    }

    public void SetSessionInfoText(string info)
    {
        sessionInfoText.text = info;
    }

    public void ShowMoveInfo()
    {
        currentPlayerTextID.gameObject.SetActive(true);
    }

    public void HideMoveInfo()
    {
        currentPlayerTextID.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        restartButton.onClick.RemoveAllListeners();
        hostButton.onClick.RemoveAllListeners();
        clientButton.onClick.RemoveAllListeners();
    }
}
