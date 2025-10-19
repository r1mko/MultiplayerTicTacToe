using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Singletone;

    // === UI References ===
    [SerializeField] private Slider playerHPSlider;
    [SerializeField] private Slider opponentHPSlider;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button slideButton;
    [SerializeField] private Button shotButton;
    [SerializeField] private Button shuffleButton;
    [SerializeField] private GameObject navigationPanel;
    [SerializeField] private GameObject smileScreen;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text sessionInfoText;
    [SerializeField] private TMP_Text winXText;
    [SerializeField] private TMP_Text winOText;
    [SerializeField] private TMP_Text currentPlayerTextID;
    [SerializeField] private TMP_Text slideButtonText;
    [SerializeField] private TMP_Text shotButtonText;
    [SerializeField] private TMP_Text shuffleButtonText;

    // ======================
    // === Инициализация ===
    // ======================

    private void Awake()
    {
        Singletone = this;

        // Подписка на события кнопок
        restartButton.onClick.AddListener(OnRestart);
        hostButton.onClick.AddListener(OnHost);
        singlePlayerButton.onClick.AddListener(OnSingle);
        clientButton.onClick.AddListener(OnClient);
        slideButton.onClick.AddListener(Slide);
        shotButton.onClick.AddListener(Shot);
        shuffleButton.onClick.AddListener(Shuffle);

        // Начальное состояние UI
        HideRestartButton();
        HideHPBar();
        HideMoveInfo();
        HideWinLoseCountInfo();
        HideSmileScreen();
        HideTimerText();
        HideSlideButton();
        HideShotButton();
        HideShuffleButton();
        ShowActiveSessionInfo();
        ShowNavigationPanel();
    }

    // ======================
    // === Навигация ===
    // ======================

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

    public void ShowNavigationPanel()
    {
        navigationPanel.SetActive(true);
    }

    public void HideNavigationPanel()
    {
        navigationPanel.SetActive(false);
    }

    // ==============================
    // === Игровая информация ===
    // ==============================

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

    // ======================
    // === Таймер ===
    // ======================

    public void SetTimerText(double time)
    {
        timerText.text = time.ToString();
    }

    public void HideTimerText()
    {
        timerText.gameObject.SetActive(false);
    }

    public void ShowTimerText()
    {
        timerText.gameObject.SetActive(true);
    }

    // ======================
    // === HP-бары ===
    // ======================

    public void SetPlayersHP(int player, int opponent)
    {
        if (playerHPSlider != null)
            playerHPSlider.value = player;

        if (opponentHPSlider != null)
            opponentHPSlider.value = opponent;
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

    // ======================
    // === Механика Slide ===
    // ======================

    public void ShowSlideButton()
    {
        slideButton.gameObject.SetActive(true);
    }

    public void HideSlideButton()
    {
        slideButton.gameObject.SetActive(false);
    }

    private void Slide()
    {
        GameManager.Singletone.ApplySlideGravity();
    }

    public void SetCooldownText(string text)
    {
        slideButtonText.text = text;
    }

    public void BlockSlideButton()
    {
        slideButton.interactable = false;
    }

    public void UnblockSlideButton()
    {
        slideButton.interactable = true;
    }

    // ======================
    // === Механика Shot ===
    // ======================

    public void ShowShotButton()
    {
        shotButton.gameObject.SetActive(true);
    }

    public void HideShotButton()
    {
        shotButton.gameObject.SetActive(false);
    }

    private void Shot()
    {
        GameManager.Singletone.ApplyShot();
    }

    public void SetShotCooldownText(string text)
    {
        shotButtonText.text = text;
    }

    public void BlockShotButton()
    {
        shotButton.interactable = false;
    }

    public void UnblockShotButton()
    {
        shotButton.interactable = true;
    }

    // =========================
    // === Механика Shuffle ===
    // =========================

    public void ShowShuffleButton()
    {
        shuffleButton.gameObject.SetActive(true);
    }

    public void HideShuffleButton()
    {
        shuffleButton.gameObject.SetActive(false);
    }

    private void Shuffle()
    {
        GameManager.Singletone.ApplyShuffle();
    }

    public void SetShuffleCooldownText(string text)
    {
        shuffleButtonText.text = text;
    }

    public void BlockShuffleButton()
    {
        shuffleButton.interactable = false;
    }

    public void UnblockShuffleButton()
    {
        shuffleButton.interactable = true;
    }

    // ======================
    // === Сессия ===
    // ======================

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

    // ======================
    // === Кнопка Restart ===
    // ======================

    public void ShowRestartButton()
    {
        restartButton.gameObject.SetActive(true);
    }

    public void HideRestartButton()
    {
        restartButton.gameObject.SetActive(false);
    }

    // ======================
    // === Очистка ===
    // ======================

    private void OnDestroy()
    {
        restartButton.onClick.RemoveAllListeners();
        hostButton.onClick.RemoveAllListeners();
        clientButton.onClick.RemoveAllListeners();
        singlePlayerButton.onClick.RemoveAllListeners();
        slideButton.onClick.RemoveAllListeners();
        shotButton.onClick.RemoveAllListeners();
        shuffleButton.onClick.RemoveAllListeners();
    }
}