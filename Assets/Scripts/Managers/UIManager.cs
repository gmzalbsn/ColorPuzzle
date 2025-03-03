using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    #region Serialized Fields - Text Elements
    [Header("UI Elements - Texts")]
    [SerializeField] private TextMeshProUGUI levelNumberText;
    [SerializeField] private TextMeshProUGUI completedBoardsText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI totalStarsText;
    #endregion
    
    #region Serialized Fields - Panels
    [Header("UI Elements - Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject failPanel;
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject allLevelsCompletePanel;
    #endregion
    
    #region Serialized Fields - Buttons
    [Header("UI Elements - Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button tryAgainButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button resetDataButton;
    [SerializeField] private Button playAgainButton;
    #endregion
    
    #region Serialized Fields - Sound UI
    [Header("Sound Buttons")]
    [SerializeField] private GameObject soundOnImage;
    [SerializeField] private GameObject soundOffImage;
    #endregion
    
    #region Serialized Fields - Progress
    [Header("Stage Progress")]
    [SerializeField] private GameObject stageProgressContainer;
    [SerializeField] private Image stageProgressFill;
    #endregion
    
    #region Serialized Fields - Star Animation
    [Header("Star Animation")]
    [SerializeField] public RectTransform starUITarget;
    [SerializeField] public GameObject starPrefab;
    #endregion
    
    #region Private Fields
    private float progressLerpSpeed = 3f;
    private float currentProgress = 0.5f;
    private GameManager gameManager;
    #endregion

    #region Unity Lifecycle Methods
    private void Start()
    {
        InitializeGameManager();
        InitializeUI();
    }
    
    private void Update()
    {
        UpdateProgressFill();
    }
    #endregion
    
    #region Initialization
    private void InitializeGameManager()
    {
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager not found in the scene");
            return;
        }
        gameManager.OnBoardsUpdated += UpdateBoardsText;
        gameManager.OnTimerUpdated += UpdateTimer;
        gameManager.OnStageProgressUpdated += UpdateStageProgress;
    }
    
    private void InitializeUI()
    {
        InitializeButtons();
        HideAllPanels();
        UpdateAllUI();
    }
    
    private void InitializeButtons()
    {
        SetupButton(pauseButton, () => {
            PlayButtonSound();
            PauseGame();
        });
        SetupButton(continueButton, () => {
            PlayButtonSound();
            ResumeGame();
        });
        SetupButton(replayButton, () => {
            PlayButtonSound();
            RestartLevel();
        });
        SetupButton(tryAgainButton, () => {
            PlayButtonSound();
            RestartLevel();
        });
        SetupButton(nextLevelButton, () => {
            PlayButtonSound();
            LoadNextLevel();
        });
        SetupButton(playAgainButton, () => {
            PlayButtonSound();
            ResetAllData();
        });

        SetupSoundButtons();

        SetupButton(resetDataButton, () => {
            PlayButtonSound();
            ResetAllData();
        });
    }
    
    private void SetupButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
    
    private void SetupSoundButtons()
    {
        if (soundOnImage != null && soundOffImage != null)
        {
            Button soundOnButton = soundOnImage.GetComponent<Button>();
            Button soundOffButton = soundOffImage.GetComponent<Button>();

            if (soundOnButton != null)
                soundOnButton.onClick.AddListener(ToggleSound);

            if (soundOffButton != null)
                soundOffButton.onClick.AddListener(ToggleSound);
        }
    }
    #endregion
    
    #region UI Updates
    private void UpdateAllUI()
    {
        if (gameManager == null) return;
        
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
    
    private void UpdateProgressFill()
    {
        if (stageProgressFill != null)
        {
            stageProgressFill.fillAmount = Mathf.Lerp(stageProgressFill.fillAmount, 
                currentProgress, Time.deltaTime * progressLerpSpeed);
        }
    }
    #endregion
    
    #region Panel Management
    public void ShowPausePanel()
    {
        pausePanel.SetActive(true);
    }
    
    public void ShowLevelCompletePanel()
    {
        if (levelCompletePanel != null)
        {
            DOVirtual.DelayedCall(1f, () => levelCompletePanel.SetActive(true));
            PlayWinSound();
        }
    }
    
    public void ShowAllLevelsCompletePanel()
    {
        if (allLevelsCompletePanel != null)
        {
            allLevelsCompletePanel.SetActive(true);
            PlayWinSound();
        }
    }

    public void ShowFailPanel()
    {
        if (failPanel != null)
        {
            failPanel.SetActive(true);
            PlayFailSound();
        }
    }

    private void HideAllPanels()
    {
        pausePanel.SetActive(false);
        failPanel.SetActive(false);
        levelCompletePanel.SetActive(false);
        allLevelsCompletePanel.SetActive(false);
    }
    #endregion
    
    #region StarUIAnimation
    public void SpawnStarAtBoardPosition(Vector3 boardPosition,int currentStageStars,int totalStars)
    {
        if (starPrefab == null || starUITarget == null)
        {
            Debug.LogError("UIManager or Star components missing!");
            return;
        }
        
        GameObject star =starPrefab;
        star.SetActive(true);
        star.transform.localScale = Vector3.one * 1.5f;
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(boardPosition);
        star.transform.position = screenPos;
        star.transform.SetParent(starUITarget.parent, true);
        
        AnimateStarToTarget(star,currentStageStars,totalStars);
    }
    
    private void AnimateStarToTarget(GameObject star,int currentStageStars,int totalStars)
    {
        Sequence starSequence = DOTween.Sequence();
        
        starSequence.Append(star.transform.DOMove(starUITarget.position, 0.8f).SetEase(Ease.InOutQuad));
        starSequence.Join(star.transform.DOScale(Vector3.one, 0.8f).SetEase(Ease.InOutQuad));

        starSequence.OnComplete(() =>
        {
            currentStageStars++;
            UpdateTotalStars(totalStars + currentStageStars);
            
            star.transform.localScale = Vector3.one * 1.5f;
            star.SetActive(false);
        });
    }
    #endregion
    #region Game Actions
    private void PauseGame()
    {
        if (gameManager == null) return;
        
        gameManager.PauseGame();
        ShowPausePanel();
    }
    
    private void ResumeGame()
    {
        if (gameManager == null) return;
        
        gameManager.ResumeGame();
        pausePanel.SetActive(false);
    }
    
    private void RestartLevel()
    {
        if (gameManager == null) return;
        
        gameManager.RestartLevel();
        HideAllPanels();
    }
    
    private void LoadNextLevel()
    {
        if (gameManager == null) return;
        
        gameManager.LoadNextLevel();
        HideAllPanels();
    }
    
    private void ResetAllData()
    {
        if (gameManager == null) return;
        
        gameManager.ResetAllData();
        HideAllPanels();
    }
    
    private void ToggleSound()
    {
        if (gameManager == null) return;
        
        gameManager.ToggleSound();
    }
    #endregion
    
    #region Audio Helpers
    private void PlayButtonSound()
    {
        AudioManager.Instance?.PlayUIButtonSound();
    }
    
    private void PlayWinSound()
    {
        AudioManager.Instance?.PlayLevelWinSound();
    }
    
    private void PlayFailSound()
    {
        AudioManager.Instance?.PlayLevelFailSound();
    }
    #endregion
}