using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] private GameObject levelCompletedPanel; 
    [SerializeField] private Text levelCompletedText; 
    private int requiredCompletedBoards = 0;
    private List<GridManager> boardManagers = new List<GridManager>();
    private void Start()
    {
        if (levelCompletedPanel != null)
        {
            levelCompletedPanel.SetActive(false);
        }
        StartCoroutine(InitializeBoardsAfterDelay());
    }
    private IEnumerator InitializeBoardsAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        boardManagers.Clear();
        GridManager[] managers = FindObjectsOfType<GridManager>();
        foreach (GridManager manager in managers)
        {
            boardManagers.Add(manager);
        }
    }
}