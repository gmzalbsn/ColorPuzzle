using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class LevelManager : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] private GameObject levelCompletedPanel; 
    [SerializeField] private Text levelCompletedText;
    #endregion
    
    #region Private Fields
    private int requiredCompletedBoards = 0;
    private List<GridManager> boardManagers = new List<GridManager>();
    #endregion
    
    #region Unity Lifecycle Methods
    private void Start()
    {
        InitializeLevelUI();
        StartCoroutine(InitializeBoardsAfterDelay());
    }
    #endregion
    
    #region Initialization
    private void InitializeLevelUI()
    {
        if (levelCompletedPanel != null)
        {
            levelCompletedPanel.SetActive(false);
        }
    }
    
    private IEnumerator InitializeBoardsAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        RefreshBoardManagersList();
    }
    
    private void RefreshBoardManagersList()
    {
        boardManagers.Clear();
        GridManager[] managers = FindObjectsOfType<GridManager>();
        
        foreach (GridManager manager in managers)
        {
            boardManagers.Add(manager);
        }
    }
    #endregion
    
    #region Level Management
    public void CheckLevelCompletion()
    {
        int completedBoards = 0;
        
        foreach (GridManager manager in boardManagers)
        {
            if (manager.IsCompleted())
            {
                completedBoards++;
            }
        }
        
        if (completedBoards >= requiredCompletedBoards)
        {
            ShowLevelCompleted();
        }
    }
    
    private void ShowLevelCompleted()
    {
        if (levelCompletedPanel != null)
        {
            levelCompletedPanel.SetActive(true);
            
            if (levelCompletedText != null)
            {
                levelCompletedText.text = "Level Completed!";
            }
        }
    }
    #endregion
}