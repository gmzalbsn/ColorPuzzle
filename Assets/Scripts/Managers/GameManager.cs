using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadSavedData();
    }
    #endregion
    
    #region Events
    public delegate void BoardsUpdatedHandler(int completed);
    public event BoardsUpdatedHandler OnBoardsUpdated;

    public delegate void TimerUpdatedHandler(float time);
    public event TimerUpdatedHandler OnTimerUpdated;

    public delegate void StageProgressHandler(float progress, bool hasMultipleStages);
    public event StageProgressHandler OnStageProgressUpdated;
    #endregion
    
    #region Serialized Fields
    [SerializeField] private Dictionary<int, int> levelStages = new Dictionary<int, int>()
    {
        { 1, 1 },
        { 2, 2 },
        { 3, 1 },
    };
    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] public UIManager uiManager;
    #endregion
    
    #region Private Fields
    private int currentLevel = 1;
    private int currentStageStars = 0;
    private int totalStars = 0;
    private float levelTimer = 0;
    private bool isTimerRunning = false;
    private bool isSoundOn = true;
    private int currentStage = 1;
    private int totalStagesInLevel = 1;
    private float initialLevelTimer = 0;
    
    #endregion
    
    #region Public Properties
    public bool isRestarting { get; set; } = false;
    #endregion
    
    #region Unity Lifecycle Methods
    private void Start()
    {
        UpdateUI();
        LoadCurrentLevel();
    }

    private void Update()
    {
        UpdateTimer();
    }
    #endregion
    
    #region Timer Management
    private void UpdateTimer()
    {
        if (isTimerRunning && levelTimer > 0)
        {
            levelTimer -= Time.deltaTime;
            OnTimerUpdated?.Invoke(levelTimer);

            if (levelTimer <= 0)
            {
                HandleTimerExpired();
            }
        }
    }
    
    private void HandleTimerExpired()
    {
        levelTimer = 0;
        isTimerRunning = false;

        if (uiManager != null)
        {
            uiManager.ShowFailPanel();
        }
    }
    
    public void PauseGame()
    {
        isTimerRunning = false;
    }

    public void ResumeGame()
    {
        isTimerRunning = true;
    }
    
    private IEnumerator ResumeTimerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isTimerRunning = true;
    }
    #endregion
    
    #region Level & Stage Management
    public void LoadCurrentLevel()
    {
        if (levelLoader == null) return;
        
        levelLoader.levelNumber = currentLevel;
        levelLoader.LoadLevelFromFile();
        
        if (levelLoader.currentLevelData != null)
        {
            InitializeLevelData();
            UpdateUI();
            NotifyUIComponents();
            isTimerRunning = true;
        }
    }
    
    private void InitializeLevelData()
    {
        if (currentStage == 1)
        {
            initialLevelTimer = levelLoader.currentLevelData.timeLimit;
            levelTimer = initialLevelTimer;
            currentStageStars = 0;
            LevelLoader.completedBoardsAmount = 0;
        }

        totalStagesInLevel = levelLoader.currentLevelData.totalStages;
    }
    
    private void NotifyUIComponents()
    {
        OnBoardsUpdated?.Invoke(LevelLoader.completedBoardsAmount);
        OnTimerUpdated?.Invoke(levelTimer);
        UpdateStageProgress();
    }
    
    public void OnBoardCompleted()
    {
        OnBoardsUpdated?.Invoke(LevelLoader.completedBoardsAmount);
        
        if (LevelLoader.completedBoardsAmount >= LevelLoader.requiredCompletedBoards)
        {
            isTimerRunning = false;
            CompleteLevelStage();
        }
    }
    
    private void CompleteLevelStage()
    {
        int totalStages = GetTotalStagesForLevel(currentLevel);
        currentStageStars = LevelLoader.completedBoardsAmount;

        if (currentStage < totalStages)
        {
            ProcessNextStage();
        }
        else
        {
            FinishLevel();
        }
    }
    
    private void ProcessNextStage()
    {
        isTimerRunning = false;
        currentStage++;
        UpdateStageProgress();
        LoadCurrentLevel();
        StartCoroutine(ResumeTimerAfterDelay(1.5f));
    }
    
    private void FinishLevel()
    {
        if (uiManager == null) return;
        
        totalStars += currentStageStars;
        uiManager.UpdateTotalStars(totalStars);
        if (currentLevel >= levelStages.Count)
        {
            uiManager.ShowAllLevelsCompletePanel();
        }
        else
        {
            uiManager.ShowLevelCompletePanel();
        }

        SaveData();
    }
    
    public void RestartLevel()
    {
        levelTimer = initialLevelTimer;
        isRestarting = true;
        currentStageStars = 0;
        currentStage = 1;
        LevelLoader.completedBoardsAmount = 0;
        LoadCurrentLevel();
    }

    public void LoadNextLevel()
    {
        currentLevel++;
        currentStage = 1;
        SaveData();
        LoadCurrentLevel();
    }
    #endregion
    
    #region UI Interaction
    private void UpdateUI()
    {
        if (uiManager == null) return;
        
        uiManager.UpdateLevelNumber(currentLevel);
        uiManager.UpdateTotalStars(totalStars);
        uiManager.UpdateSoundButtons(isSoundOn);
        UpdateStageProgress();
    }
    
    private void UpdateStageProgress()
    {
        float progress = 0f;
        bool hasMultipleStages = (totalStagesInLevel > 1);

        if (hasMultipleStages)
        {
            progress = (float)(currentStage - 1) / totalStagesInLevel;
        }

        OnStageProgressUpdated?.Invoke(progress, hasMultipleStages);
    }

    public void GetStarMove(Vector3 effectPosition)
    {
        uiManager.SpawnStarAtBoardPosition(effectPosition, currentStageStars, totalStars);
    }
    #endregion
    
    #region Data Management
    private void LoadSavedData()
    {
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        currentStage = 1;
        totalStars = PlayerPrefs.GetInt("TotalStars", 0);
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("TotalStars", totalStars);
        PlayerPrefs.SetInt("SoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    public void ResetAllData()
    {
        PlayerPrefs.DeleteAll();
        currentLevel = 1;
        currentStage = 1;
        totalStagesInLevel = 1;
        totalStars = 0;

        isSoundOn = true;
        SaveData();
        LoadCurrentLevel();
        UpdateUI();
    }
    #endregion
    
    #region Sound Management
    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        AudioListener.volume = isSoundOn ? 1f : 0f;
        
        if (uiManager != null)
        {
            AudioManager.Instance.ToggleSound();
            uiManager.UpdateSoundButtons(isSoundOn);
        }

        SaveData();
    }
    #endregion
    
    #region Public Getters
    public int GetTotalStagesForLevel(int level)
    {
        string resourcePath = "Levels/level" + level + "_1";
        TextAsset levelTextAsset = Resources.Load<TextAsset>(resourcePath);

        if (levelTextAsset != null)
        {
            LevelData levelData = JsonUtility.FromJson<LevelData>(levelTextAsset.text);
            return levelData.totalStages;
        }
        resourcePath = "Levels/level" + level;
        levelTextAsset = Resources.Load<TextAsset>(resourcePath);

        if (levelTextAsset != null)
        {
            LevelData levelData = JsonUtility.FromJson<LevelData>(levelTextAsset.text);
            return levelData.totalStages;
        }

        return 1;
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public int GetCurrentStage()
    {
        return currentStage;
    }

    public int GetTotalStagesInLevel()
    {
        return totalStagesInLevel;
    }

    public int GetTotalStars()
    {
        return totalStars;
    }

    public bool GetSoundState()
    {
        return isSoundOn;
    }
    #endregion
}