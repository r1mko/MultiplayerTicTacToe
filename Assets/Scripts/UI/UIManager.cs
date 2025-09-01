using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Singletone;
    [SerializeField] private Slider playerHPSlider;
    [SerializeField] private Slider opponentHPSlider;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text sessionInfoText;
    [SerializeField] private TMP_Text winXText;
    [SerializeField] private TMP_Text winOText;
    [SerializeField] private TMP_Text currentPlayerTextID;
    [SerializeField] private GameObject navigationPanel;
    [SerializeField] private GameObject smileScreen;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button slideButton;

    private void Awake()
    {
        Singletone = this;
        restartButton.onClick.AddListener(OnRestart);
        hostButton.onClick.AddListener(OnHost);
        singlePlayerButton.onClick.AddListener(OnSingle);
        clientButton.onClick.AddListener(OnClient);
        slideButton.onClick.AddListener(Slide);
        HideRestartButton();
        HideHPBar();
        HideMoveInfo();
        HideWinLoseCountInfo();
        HideSmileScreen();
        HideTimerText();
        ShowActiveSessionInfo();
        ShowNavigationPanel();
    }

    private void OnSingle()
    {
        HideNavigationPanel();
        GameManager.Singletone.StartGame();
    }

    private void OnRestart()
    {
        GameManager.Singletone.RestartGame();
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
        if (GameManager.Singletone.IsOurTurn())
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
        if (GameManager.Singletone.IsOurTurn())
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

    public void ShowWinLoseCountInfo()
    {
        winXText.gameObject.SetActive(true);
        winOText.gameObject.SetActive(true);
    }

    public void HideWinLoseCountInfo()
    {
        winOText.gameObject.SetActive(false);
    }

    public void SetWinLoseCountText(int[] winArray)
    {
        winXText.text = $"X: {winArray[0]}";
        winOText.text = $"O: {winArray[1]}";
    }

    public void ShowSmileScreen()
    {
        smileScreen.SetActive(true);
    }

    public void HideSmileScreen()
    {
        smileScreen.SetActive(false);
    }

    public void SetTimerText(double time)
    {
        timerText.text = time.ToString();
    }

    public void HideTimerText()
    {
        timerText.gameObject.SetActive(false);
       // Debug.Log("Вызвали скрытие таймера");
    }

    public void ShowTimerText()
    {
        timerText.gameObject.SetActive(true);
        //Debug.Log("Вызвали отображение таймера");
    }

    public void SetPlayersHP(int player, int opponent)
    {
        if (playerHPSlider != null)
            playerHPSlider.value = player;

        if (opponentHPSlider != null)
            opponentHPSlider.value = opponent;

        // Если ты хочешь, чтобы максимальное значение слайдера устанавливалось автоматически,
        // раскомментируй следующие строки:
        /*
        if (playerHPSlider != null)
            playerHPSlider.maxValue = player; // или задай максимальное HP отдельно
        if (opponentHPSlider != null)
            opponentHPSlider.maxValue = opponent;
        */
    }

    public void ShowHPBar()
    {
        playerHPSlider.gameObject.SetActive(true);
        opponentHPSlider.gameObject.SetActive(true);
    }
    public void HideHPBar()
    {
        playerHPSlider.gameObject.SetActive(false);
        opponentHPSlider.gameObject.SetActive(false);
    }

    private void Slide()
    {
        BoardManager.Singltone.ApplyGravity();
    }
    private void OnDestroy()
    {
        restartButton.onClick.RemoveAllListeners();
        hostButton.onClick.RemoveAllListeners();
        clientButton.onClick.RemoveAllListeners();
        singlePlayerButton.onClick.RemoveAllListeners();
    }
}
