using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements - Texts")] [SerializeField]
    private TextMeshProUGUI levelNumberText;

    [SerializeField] private TextMeshProUGUI completedBoardsText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI totalStarsText;

    [Header("UI Elements - Panels")] [SerializeField]
    private GameObject pausePanel;

    [SerializeField] private GameObject failPanel;
    [SerializeField] private GameObject levelCompletePanel;

    [Header("UI Elements - Buttons")] [SerializeField]
    private Button pauseButton;

    [SerializeField] private Button continueButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button tryAgainButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button resetDataButton;

    [Header("Sound Buttons")] [SerializeField]
    private GameObject soundOnImage;

    [SerializeField] private GameObject soundOffImage;

    [Header("Stage Progress")] [SerializeField]
    private GameObject stageProgressContainer;

    [SerializeField] private Image stageProgressFill;

    private float progressLerpSpeed = 3f;
    private float currentProgress = 0.5f;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            return;
        }

        InitializeButtons();
        gameManager.OnBoardsUpdated += UpdateBoardsText;
        gameManager.OnTimerUpdated += UpdateTimer;
        gameManager.OnStageProgressUpdated += UpdateStageProgress;
        pausePanel.SetActive(false);
        failPanel.SetActive(false);
        levelCompletePanel.SetActive(false);
        UpdateAllUI();
    }

    private void InitializeButtons()
    {
        pauseButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayUIButtonSound();
            gameManager.PauseGame();
            pausePanel.SetActive(true);
        });

        continueButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayUIButtonSound();
            gameManager.ResumeGame();
            pausePanel.SetActive(false);
        });

        replayButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayUIButtonSound();
            gameManager.RestartLevel();
            HideAllPanels();
        });

        tryAgainButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayUIButtonSound();
            gameManager.RestartLevel();
            HideAllPanels();
        });

        nextLevelButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayUIButtonSound();
            gameManager.LoadNextLevel();
            HideAllPanels();
        });

        if (soundOnImage != null && soundOffImage != null)
        {
            Button soundOnButton = soundOnImage.GetComponent<Button>();
            Button soundOffButton = soundOffImage.GetComponent<Button>();

            if (soundOnButton != null)
                soundOnButton.onClick.AddListener(gameManager.ToggleSound);

            if (soundOffButton != null)
                soundOffButton.onClick.AddListener(gameManager.ToggleSound);
        }

        if (resetDataButton != null)
        {
            resetDataButton.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayUIButtonSound();
                if (gameManager != null)
                {
                    gameManager.ResetAllData();
                }
            });
        }
    }

    private void UpdateAllUI()
    {
        if (gameManager != null)
        {
            UpdateLevelNumber(gameManager.GetCurrentLevel());
            UpdateTotalStars(gameManager.GetTotalStars());
            UpdateSoundButtons(gameManager.GetSoundState());

            bool hasMultipleStages = gameManager.GetTotalStagesInLevel() > 1;
            float progress = 0.5f;
            if (hasMultipleStages)
            {
                progress = (float)(gameManager.GetCurrentStage() - 1) / gameManager.GetTotalStagesInLevel();
            }

            UpdateStageProgress(progress, hasMultipleStages);
        }
    }

    public void UpdateStageProgress(float progress, bool hasMultipleStages)
    {
        if (stageProgressContainer != null)
        {
            stageProgressContainer.SetActive(hasMultipleStages);
        }

        if (hasMultipleStages)
        {
            currentProgress = progress == 0 ? 0.5f : 1f;
        }
    }

    public void UpdateLevelNumber(int level)
    {
        if (levelNumberText != null)
        {
            levelNumberText.text = level.ToString();
        }
    }

    public void UpdateBoardsText(int completed)
    {
        if (completedBoardsText != null)
        {
            completedBoardsText.text = completed.ToString();
        }
    }

    public void UpdateTimer(float timeRemaining)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void Update()
    {
        if (stageProgressFill != null)
        {
            stageProgressFill.fillAmount = Mathf.Lerp(stageProgressFill.fillAmount, currentProgress,
                Time.deltaTime * progressLerpSpeed);
        }
    }

    public void UpdateTotalStars(int stars)
    {
        if (totalStarsText != null)
        {
            totalStarsText.text = stars.ToString();
        }
    }

    public void UpdateSoundButtons(bool isSoundOn)
    {
        if (soundOnImage != null && soundOffImage != null)
        {
            soundOnImage.SetActive(isSoundOn);
            soundOffImage.SetActive(!isSoundOn);
        }
    }

    public void ShowLevelCompletePanel()
    {
        if (levelCompletePanel != null)
        {
            DOVirtual.DelayedCall(1f,()=>levelCompletePanel.SetActive(true));
            
            AudioManager.Instance.PlayLevelWinSound();
        }
    }

    public void ShowFailPanel()
    {
        if (failPanel != null)
        {
            failPanel.SetActive(true);
            AudioManager.Instance.PlayLevelFailSound();
        }
    }

    private void HideAllPanels()
    {
        pausePanel.SetActive(false);
        failPanel.SetActive(false);
        levelCompletePanel.SetActive(false);
    }
}