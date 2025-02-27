using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class LevelData
{
    public int levelId;
    public int timeLimit;
    public int requiredCompletedBoards;
    public List<BoardData> boards;
}

[System.Serializable]
public class BoardData
{
    public string id;
    public string position;
    public int rows;
    public int columns;
    public List<BlockData> blocks;
    
    public bool hasCustomShape = false;
    public List<CellPosition> customCells;
}

[System.Serializable]
public class CellPosition
{
    public int x;
    public int y;
}

[System.Serializable]
public class BlockData
{
    public string id;           
    public string color;        
    public bool isFixed;       
    public List<BlockPartPosition> parts; 
}

[System.Serializable]
public class BlockPartPosition
{
    public int gridX;  
    public int gridY; 
}

public class LevelLoader : MonoBehaviour
{
    [SerializeField] private GameObject boardPrefab;
    [SerializeField] private int levelNumber=1;
    [SerializeField] private float boardSpacing = 4f;
    [SerializeField] private float cameraOffset = 4f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BlockManager blockManager;
    private string levelFolderPath = "Assets/Scripts/LevelJson";
    private List<GridManager> boardManagers = new List<GridManager>();
    private LevelData currentLevelData;
    public static int requiredCompletedBoards;
    public static int completedBoardsAmount = 0;
    private void Start()
    {
        completedBoardsAmount = 0;
        LoadLevelFromFile();
    }
    
    public void LoadLevelFromFile()
    {
        string resourcePath = "Levels/" + Path.GetFileNameWithoutExtension("level"+levelNumber);
        TextAsset levelTextAsset = Resources.Load<TextAsset>(resourcePath);
    
        if (levelTextAsset != null)
        {
            string jsonText = levelTextAsset.text;
            currentLevelData = JsonUtility.FromJson<LevelData>(jsonText);
            requiredCompletedBoards=currentLevelData.requiredCompletedBoards;
            CreateBoards();
        }
        else
        {
            Debug.LogError($"JSON dosyası bulunamadı: {resourcePath}");
        }
    }
    
   private void CreateBoards()
{
    if (currentLevelData == null || currentLevelData.boards == null)
    {
        return;
    }
    foreach (Transform child in transform)
    {
        Destroy(child.gameObject);
    }
    boardManagers.Clear();
    Dictionary<string, List<BoardData>> boardsByRow = new Dictionary<string, List<BoardData>>();
    foreach (BoardData board in currentLevelData.boards)
    {
        string rowKey = board.position.Contains("top") ? "top" : 
                       board.position.Contains("middle") ? "middle" : 
                       board.position.Contains("bottom") ? "bottom" : "middle";
                       
        if (!boardsByRow.ContainsKey(rowKey))
            boardsByRow[rowKey] = new List<BoardData>();
            
        boardsByRow[rowKey].Add(board);
    }
    float totalHeight = (boardsByRow.Count - 1) * boardSpacing;
    float currentY = totalHeight / 2;
    
    foreach (var rowPair in boardsByRow)
    {
        List<BoardData> rowBoards = rowPair.Value;
        float totalRowWidth = (rowBoards.Count - 1) * boardSpacing;
        float currentX = -totalRowWidth / 2;
        foreach (BoardData boardData in rowBoards)
        {
            GameObject boardObj = Instantiate(boardPrefab, Vector3.zero, Quaternion.identity, transform);
            boardObj.name = boardData.id;
            GridManager gridManager = boardObj.GetComponentInChildren<GridManager>();
            gridManager.SetBoardId(boardData.id);
            if (boardData.hasCustomShape && boardData.customCells != null && boardData.customCells.Count > 0)
            {
                gridManager.CreateCustomGrid(boardData.rows, boardData.columns, boardData.customCells);
            }
            else
            {
                gridManager.CreateGrid(boardData.rows, boardData.columns);
            }
            boardManagers.Add(gridManager);
            Vector3 boardPosition = new Vector3(currentX, currentY, 0);
            boardObj.transform.position = boardPosition;
            boardObj.transform.rotation = Quaternion.Euler(-90, 0, 0);
            if (blockManager != null && boardData.blocks != null && boardData.blocks.Count > 0)
            {
                blockManager.CreateBlocksForBoard(gridManager, boardData);
            }
            
            currentX += boardSpacing;
        }
        currentY -= boardSpacing;
    }
    AdjustCameraToFitAllBoards();
}
    
    private void AdjustCameraToFitAllBoards()
    {
        if (mainCamera == null || transform.childCount == 0)
            return;
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;
    
        foreach (Transform child in transform)
        {
            foreach (Transform gridCell in child)
            {
                if (!boundsInitialized)
                {
                    bounds = new Bounds(gridCell.position, Vector3.zero);
                    boundsInitialized = true;
                }
                else
                {
                    bounds.Encapsulate(gridCell.position);
                }
            }
        }
        bounds.Expand(cameraOffset);
        float screenRatio = (float)Screen.width / Screen.height;
        float boundsSize = Mathf.Max(bounds.size.x / screenRatio, bounds.size.z);
        mainCamera.orthographicSize = boundsSize / 2;
        Vector3 cameraPos = mainCamera.transform.position;
        cameraPos.x = bounds.center.x;
        mainCamera.transform.position = cameraPos;
    }
}