using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private int currentLevel = 1;
    private int currentStageStars = 0;
    private int totalStars = 0;
    private float levelTimer = 0;
    private bool isTimerRunning = false;
    private bool isSoundOn = true;
    private int currentStage = 1;
    private int totalStagesInLevel = 1;
    private float initialLevelTimer = 0; // İlk timer değerini sakla
    private LevelLoader levelLoader;
    private UIManager uiManager;

    public delegate void BoardsUpdatedHandler(int completed);

    public event BoardsUpdatedHandler OnBoardsUpdated;

    public delegate void TimerUpdatedHandler(float time);

    public event TimerUpdatedHandler OnTimerUpdated;

    public delegate void StageProgressHandler(float progress, bool hasMultipleStages);

    public event StageProgressHandler OnStageProgressUpdated;
    public bool isRestarting = false;

    private Dictionary<int, int> levelStages = new Dictionary<int, int>()
    {
        { 1, 1 },
        { 2, 2 },
        { 3, 1 },
    };

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

    private void Start()
    {
        levelLoader = FindObjectOfType<LevelLoader>();
        if (levelLoader == null)
        {
            return;
        }

        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            return;
        }

        UpdateUI();
        LoadCurrentLevel();
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

    private void Update()
    {
        if (isTimerRunning && levelTimer > 0)
        {
            levelTimer -= Time.deltaTime;
            OnTimerUpdated?.Invoke(levelTimer);

            if (levelTimer <= 0)
            {
                levelTimer = 0;
                isTimerRunning = false;

                if (uiManager != null)
                {
                    uiManager.ShowFailPanel();
                }
            }
        }
    }

    private void UpdateUI()
    {
        if (uiManager != null)
        {
            uiManager.UpdateLevelNumber(currentLevel);
            uiManager.UpdateTotalStars(totalStars);
            uiManager.UpdateSoundButtons(isSoundOn);
            UpdateStageProgress();
        }
    }

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

    private void LoadSavedData()
    {
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        currentStage = 1;
        totalStars = PlayerPrefs.GetInt("TotalStars", 0);
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        Debug.Log($"Loaded data: Level={currentLevel}, Stage=1 (reset to beginning)");
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("TotalStars", totalStars);
        PlayerPrefs.SetInt("SoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadCurrentLevel()
    {
        if (levelLoader != null)
        {
            levelLoader.levelNumber = currentLevel;
            levelLoader.LoadLevelFromFile();
            if (levelLoader.currentLevelData != null)
            {
                if (currentStage == 1)
                {
                    initialLevelTimer = levelLoader.currentLevelData.timeLimit;
                    levelTimer = initialLevelTimer;
                    currentStageStars = 0;
                    LevelLoader.completedBoardsAmount = 0;
                }

                totalStagesInLevel = levelLoader.currentLevelData.totalStages;

                UpdateStageProgress();
                UpdateUI();
                OnBoardsUpdated?.Invoke(LevelLoader.completedBoardsAmount);
                OnTimerUpdated?.Invoke(levelTimer);
                isTimerRunning = true;
            }
        }
    }

    private void CompleteLevelStage()
    {
        int totalStages = GetTotalStagesForLevel(currentLevel);
        currentStageStars = LevelLoader.completedBoardsAmount;
        if (currentStage < totalStages)
        {
            isTimerRunning = false;
            currentStage++;
            float progress = (float)(currentStage - 1) / totalStages;
            OnStageProgressUpdated?.Invoke(progress, totalStages > 1);
            LoadCurrentLevel();
            StartCoroutine(ResumeTimerAfterDelay(1.5f));
        }
        else
        {
            if (uiManager != null)
            {
                totalStars += currentStageStars;
                uiManager.UpdateTotalStars(totalStars);
                uiManager.ShowLevelCompletePanel();
            }

            SaveData();
        }
    }

    private IEnumerator ResumeTimerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isTimerRunning = true;
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

    public void PauseGame()
    {
        isTimerRunning = false;
    }

    public void ResumeGame()
    {
        isTimerRunning = true;
    }

    public void RestartLevel()
    {
        levelTimer = initialLevelTimer;
        isRestarting = true;
        currentStageStars = 0;
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
}