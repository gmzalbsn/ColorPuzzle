using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private int currentLevel = 1;
    private int totalStars = 0;
    private float levelTimer = 0;
    private bool isTimerRunning = false;
    private bool isSoundOn = true;
    
    private LevelLoader levelLoader;
    private UIManager uiManager;
    public delegate void BoardsUpdatedHandler(int completed);
    public event BoardsUpdatedHandler OnBoardsUpdated;
    
    public delegate void TimerUpdatedHandler(float time);
    public event TimerUpdatedHandler OnTimerUpdated;

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
        }
    }

    private void LoadSavedData()
    {
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
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

    public void LoadCurrentLevel()
    {
        if (levelLoader != null)
        {
            levelLoader.levelNumber = currentLevel;
            levelLoader.LoadLevelFromFile();
            if (levelLoader.currentLevelData != null)
            {
                levelTimer = levelLoader.currentLevelData.timeLimit;
            }
            else
            {
                levelTimer = 120f; 
            }
            LevelLoader.completedBoardsAmount = 0;
            if (uiManager != null)
            {
                uiManager.UpdateLevelNumber(currentLevel);
            }
            
            OnBoardsUpdated?.Invoke(LevelLoader.completedBoardsAmount);
            OnTimerUpdated?.Invoke(levelTimer);
            isTimerRunning = true;
        }
    }
    public void OnBoardCompleted()
    {
        OnBoardsUpdated?.Invoke(LevelLoader.completedBoardsAmount);
        if (LevelLoader.completedBoardsAmount >= LevelLoader.requiredCompletedBoards)
        {
            totalStars++;
            if (uiManager != null)
            {
                uiManager.UpdateTotalStars(totalStars);
                uiManager.ShowLevelCompletePanel();
            }
            isTimerRunning = false;
            SaveData();
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
        LoadCurrentLevel();
    }

    public void LoadNextLevel()
    {
        currentLevel++;
        SaveData();
        LoadCurrentLevel();
    }

    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        AudioListener.volume = isSoundOn ? 1f : 0f;
        if (uiManager != null)
        {
            uiManager.UpdateSoundButtons(isSoundOn);
        }
        SaveData();
    }
    public int GetCurrentLevel() { return currentLevel; }
    public int GetTotalStars() { return totalStars; }
    public bool GetSoundState() { return isSoundOn; }
}