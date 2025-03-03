using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

#region Data Classes
[System.Serializable]
public class LevelData
{
    public int totalStages = 1;
    public int timeLimit;
    public int requiredCompletedBoards;
    public float horizontalBoardSpacing = 4.0f;
    public float verticalBoardSpacing = 4.0f;
    public float cameraOffset = 4.0f;
    public float cameraXOffset = 0f;
    public float cameraYOffset = 0f;
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
    public List<int> cellTypes; 
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
    public int cornerTypeValue;
    
    [System.NonSerialized] 
    private BlockCornerType _cornerType = BlockCornerType.Full; 
    
    public BlockCornerType cornerType
    {
        get
        {
            if (_cornerType == BlockCornerType.Full) 
            {
                if (cornerTypeValue >= 0 && cornerTypeValue <= 4)
                {
                    _cornerType = (BlockCornerType)cornerTypeValue;
                }
                else
                {
                    _cornerType = BlockCornerType.Full;
                }
            }

            return _cornerType;
        }
    }

    public List<BlockPartPosition> parts;

    public override string ToString()
    {
        return $"BlockData[id={id}, color={color}, cornerTypeValue={cornerTypeValue}, cornerType={cornerType}]";
    }
}

[System.Serializable]
public class BlockPartPosition
{
    public int gridX;
    public int gridY;
}
#endregion

public class LevelLoader : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private GameObject boardPrefab;
    [SerializeField] private float horizontalBoardSpacing = 4f;
    [SerializeField] private float verticalBoardSpacing = 4f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private float stageTransitionDelay = 1.0f;
    [SerializeField] private float stageTransitionDuration = 0.5f;
    [SerializeField] private Ease stageTransitionEase = Ease.OutQuint;
    [SerializeField] private float cameraAdjustDelay = 0.2f;
    #endregion
    
    #region Private Fields
    private List<GridManager> boardManagers = new List<GridManager>();
    private Transform oldStageContainer;
    private Transform newStageContainer;
    #endregion
    
    #region Public Properties
    public LevelData currentLevelData;
    public static int requiredCompletedBoards;
    public static int completedBoardsAmount = 0;
    public int levelNumber = 1;
    #endregion
    
    #region Unity Lifecycle Methods
    private void Start()
    {
        completedBoardsAmount = 0;
        LoadLevelFromFile();
    }
    #endregion
    
    #region Level Loading Methods
    public void LoadLevelFromFile()
    {
        int currentStage = GetCurrentStage();
        int totalStages = GetTotalStages();
        string resourcePath = GetLevelResourcePath(currentStage, totalStages);
        TextAsset levelTextAsset = Resources.Load<TextAsset>(resourcePath);

        if (levelTextAsset != null)
        {
            ProcessLevelData(levelTextAsset, currentStage);
        }
        else
        {
            Debug.LogError($"Didn't find json file: {resourcePath}");
        }
    }
    
    private int GetCurrentStage()
    {
        GameManager gameManager = GameManager.Instance;
        return gameManager != null ? gameManager.GetCurrentStage() : 1;
    }
    
    private int GetTotalStages()
    {
        GameManager gameManager = GameManager.Instance;
        return gameManager != null ? gameManager.GetTotalStagesForLevel(levelNumber) : 1;
    }
    
    private string GetLevelResourcePath(int currentStage, int totalStages)
    {
        return totalStages == 1 
            ? "Levels/level" + levelNumber 
            : "Levels/level" + levelNumber + "_" + currentStage;
    }
    
    private void ProcessLevelData(TextAsset levelTextAsset, int currentStage)
    {
        string jsonText = levelTextAsset.text;
        currentLevelData = JsonUtility.FromJson<LevelData>(jsonText);
        requiredCompletedBoards = currentLevelData.requiredCompletedBoards;
        
        GameManager gameManager = GameManager.Instance;
        bool isRestarting = (gameManager != null && gameManager.isRestarting);

        if (currentStage > 1 && transform.childCount > 0 && !isRestarting)
        {
            PrepareStageTransition();
        }
        else
        {
            SetupInitialStage(isRestarting);
        }
    }
    
    private void SetupInitialStage(bool isRestarting)
    {
        ClearAllExistingBoards();
        CreateBoards();
        
        if (!isRestarting)
        {
            ApplyCameraSettingsFromLevelData();
        }

        if (isRestarting && GameManager.Instance != null)
        {
            GameManager.Instance.isRestarting = false;
        }
    }
    #endregion
    
    #region Stage Transition Methods
    private void PrepareStageTransition()
    {
        DOTween.Kill(transform);
        CreateStageContainers();
        
        float screenWidth = Screen.width / 100f;
        newStageContainer.position = new Vector3(screenWidth * 1.2f, 0, 0);

        CreateBoards(newStageContainer);
        StartCoroutine(DelayedStageTransition());
    }

    private void CreateStageContainers()
    {
        // Create old stage container and move existing boards to it
        oldStageContainer = new GameObject("OldStageContainer").transform;
        oldStageContainer.SetParent(transform.parent);
        
        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            child.SetParent(oldStageContainer);
        }

        // Create new stage container for incoming boards
        newStageContainer = new GameObject("NewStageContainer").transform;
        newStageContainer.SetParent(transform);
    }

    private IEnumerator DelayedStageTransition()
    {
        yield return new WaitForSeconds(stageTransitionDelay);
        
        ClearAllExistingBoards();
        CreateStageContainers();

        float screenWidth = Screen.width / 100f;
        newStageContainer.position = new Vector3(screenWidth * 1.2f, 0, 0);

        CreateBoards(newStageContainer);

        yield return new WaitForSeconds(0.1f);

        AnimateStageTransition();
        ApplyCameraSettingsFromLevelData();
    }

    private void AnimateStageTransition()
    {
        float screenWidth = Screen.width / 100f;
        
        Sequence transitionSequence = DOTween.Sequence();
        
        // Animate old stage moving left
        transitionSequence.Append(oldStageContainer.DOMoveX(-screenWidth * 1.2f, stageTransitionDuration)
            .SetEase(stageTransitionEase));

        // Animate new stage coming in from right
        transitionSequence.Join(newStageContainer.DOMoveX(0f, stageTransitionDuration)
            .SetEase(stageTransitionEase));

        transitionSequence.OnComplete(() => FinalizeStageTransition());
    }

    private void FinalizeStageTransition()
    {
        // Move all children from new container to main transform
        List<Transform> childrenToMove = new List<Transform>();
        for (int i = 0; i < newStageContainer.childCount; i++)
        {
            childrenToMove.Add(newStageContainer.GetChild(i));
        }

        foreach (Transform child in childrenToMove)
        {
            Vector3 worldPos = child.position;
            child.SetParent(transform);
            child.position = worldPos;
        }

        // Clean up containers
        Destroy(oldStageContainer.gameObject);
        Destroy(newStageContainer.gameObject);
        
        // Update blocks and camera
        StartCoroutine(UpdateAllBlocksPositionsDelayed());
        StartCoroutine(DelayedCameraAdjust());
    }

    private IEnumerator UpdateAllBlocksPositionsDelayed()
    {
        yield return new WaitForSeconds(0.3f);
        
        foreach (Transform boardObj in transform)
        {
            Block[] blocks = boardObj.GetComponentsInChildren<Block>();
            foreach (Block block in blocks)
            {
                if (block != null)
                {
                    block.UpdateOriginalPosition();
                }
            }
        }
    }

    private IEnumerator DelayedCameraAdjust()
    {
        yield return new WaitForSeconds(cameraAdjustDelay);
        // Implement any additional camera adjustment here if needed
    }
    #endregion
    
    #region Board Creation Methods
    private void ClearAllExistingBoards()
    {
        // Clear child objects
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Clean up any existing containers
        DestroyContainerIfExists("OldStageContainer");
        DestroyContainerIfExists("NewStageContainer");

        boardManagers.Clear();
    }
    
    private void DestroyContainerIfExists(string containerName)
    {
        GameObject container = GameObject.Find(containerName);
        if (container != null)
        {
            Destroy(container);
        }
    }

    private void CreateBoards(Transform parent = null)
    {
        if (currentLevelData == null || currentLevelData.boards == null)
        {
            return;
        }

        Transform targetParent = parent != null ? parent : transform;
        
        if (parent == null)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            boardManagers.Clear();
        }

        // Get spacing values from level data or defaults
        float hSpacing = currentLevelData.horizontalBoardSpacing > 0 
            ? currentLevelData.horizontalBoardSpacing 
            : horizontalBoardSpacing;
            
        float vSpacing = currentLevelData.verticalBoardSpacing > 0 
            ? currentLevelData.verticalBoardSpacing 
            : verticalBoardSpacing;
        
        // Group boards by row position
        Dictionary<string, List<BoardData>> boardsByRow = GroupBoardsByRow();
        
        // Calculate vertical positioning
        float totalHeight = (boardsByRow.Count - 1) * vSpacing;
        float currentY = totalHeight / 2;

        // Create boards row by row
        foreach (var rowPair in boardsByRow)
        {
            CreateBoardRow(rowPair.Value, currentY, hSpacing, targetParent);
            currentY -= vSpacing; // Move down to next row
        }
    }
    
    private Dictionary<string, List<BoardData>> GroupBoardsByRow()
    {
        Dictionary<string, List<BoardData>> boardsByRow = new Dictionary<string, List<BoardData>>();
        
        foreach (BoardData board in currentLevelData.boards)
        {
            string rowKey = GetRowKeyFromPosition(board.position);

            if (!boardsByRow.ContainsKey(rowKey))
                boardsByRow[rowKey] = new List<BoardData>();

            boardsByRow[rowKey].Add(board);
        }
        
        return boardsByRow;
    }
    
    private string GetRowKeyFromPosition(string position)
    {
        if (position.Contains("top")) return "top";
        if (position.Contains("middle")) return "middle";
        if (position.Contains("bottom")) return "bottom";
        return "middle"; // Default
    }
    
    private void CreateBoardRow(List<BoardData> rowBoards, float currentY, float hSpacing, Transform targetParent)
    {
        float totalRowWidth = (rowBoards.Count - 1) * hSpacing;
        float currentX = -totalRowWidth / 2;

        foreach (BoardData boardData in rowBoards)
        {
            CreateSingleBoard(boardData, new Vector3(currentX, currentY, 0), targetParent);
            currentX += hSpacing; // Move right to next position
        }
    }
    
    private void CreateSingleBoard(BoardData boardData, Vector3 position, Transform targetParent)
    {
        GameObject boardObj = Instantiate(boardPrefab, Vector3.zero, Quaternion.identity, targetParent);
        boardObj.name = boardData.id;
        
        GridManager gridManager = boardObj.GetComponentInChildren<GridManager>();
        if (gridManager == null)
        {
            Debug.LogWarning($"GridManager component not found on board prefab for board {boardData.id}");
            return;
        }

        // Initialize grid manager
        gridManager.SetBoardId(boardData.id);
        
        // Create grid based on board data
        if (boardData.hasCustomShape && boardData.customCells != null && boardData.customCells.Count > 0)
        {
            List<CellType> cellTypeList = ConvertToCellTypes(boardData.cellTypes);
            gridManager.CreateCustomGrid(boardData.rows, boardData.columns, boardData.customCells, cellTypeList);
        }
        else
        {
            gridManager.CreateGrid(boardData.rows, boardData.columns);
        }

        // Add to managers list and position the board
        boardManagers.Add(gridManager);
        boardObj.transform.localPosition = position;
        boardObj.transform.rotation = Quaternion.Euler(-90, 0, 0);
        
        // Create blocks for this board
        if (blockManager != null && boardData.blocks != null && boardData.blocks.Count > 0)
        {
            blockManager.CreateBlocksForBoard(gridManager, boardData);
        }
    }

    private List<CellType> ConvertToCellTypes(List<int> cellTypeIds)
    {
        if (cellTypeIds == null || cellTypeIds.Count == 0)
            return null;

        List<CellType> cellTypes = new List<CellType>();
        foreach (int typeId in cellTypeIds)
        {
            CellType type = MapIntToCellType(typeId);
            cellTypes.Add(type);
        }

        return cellTypes;
    }
    
    private CellType MapIntToCellType(int typeId)
    {
        switch (typeId)
        {
            case 0: return CellType.Full;
            case 1: return CellType.TopRight;
            case 2: return CellType.TopLeft;
            case 3: return CellType.BottomRight;
            case 4: return CellType.BottomLeft;
            default: return CellType.Full;
        }
    }
    #endregion
    
    #region Camera Methods
    private void ApplyCameraSettingsFromLevelData()
    {
        if (mainCamera == null || currentLevelData == null)
            return;
            
        Vector3 cameraPos = mainCamera.transform.position;
        cameraPos.x = currentLevelData.cameraXOffset;
        cameraPos.y = currentLevelData.cameraYOffset;

        mainCamera.transform.position = cameraPos;
        mainCamera.orthographicSize = currentLevelData.cameraOffset;
    }
    #endregion
}