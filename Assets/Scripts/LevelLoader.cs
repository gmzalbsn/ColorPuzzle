using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

[System.Serializable]
public class LevelData
{
    public bool hasMultipleStages = false;
    public int currentStage = 1;
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

public class LevelLoader : MonoBehaviour
{
    [SerializeField] private GameObject boardPrefab;
    [SerializeField] private float horizontalBoardSpacing = 4f;
    [SerializeField] private float verticalBoardSpacing = 4f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BlockManager blockManager;
    private string levelFolderPath = "Assets/Scripts/LevelJson";
    private List<GridManager> boardManagers = new List<GridManager>();
    public LevelData currentLevelData;
    public static int requiredCompletedBoards;
    public static int completedBoardsAmount = 0;
    public int levelNumber = 1;

    [SerializeField] private float stageTransitionDelay = 1.0f;
    [SerializeField] private float stageTransitionDuration = 0.5f;
    [SerializeField] private Ease stageTransitionEase = Ease.OutQuint;
    [SerializeField] private float cameraAdjustDelay = 0.2f;

    private Transform oldStageContainer;
    private Transform newStageContainer;

    private void Start()
    {
        completedBoardsAmount = 0;
        LoadLevelFromFile();
    }

    public void LoadLevelFromFile()
    {
        int currentStage = 1;
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            currentStage = gameManager.GetCurrentStage();
        }

        string resourcePath;
        int totalStages = 1;

        if (gameManager != null)
        {
            totalStages = gameManager.GetTotalStagesForLevel(levelNumber);
        }

        if (totalStages == 1)
        {
            resourcePath = "Levels/level" + levelNumber;
        }
        else
        {
            resourcePath = "Levels/level" + levelNumber + "_" + currentStage;
        }

        TextAsset levelTextAsset = Resources.Load<TextAsset>(resourcePath);

        if (levelTextAsset != null)
        {
            string jsonText = levelTextAsset.text;
            currentLevelData = JsonUtility.FromJson<LevelData>(jsonText);
            requiredCompletedBoards = currentLevelData.requiredCompletedBoards;
            bool isRestarting = (gameManager != null && gameManager.isRestarting);

            if (currentStage > 1 && transform.childCount > 0 && !isRestarting)
            {
                PrepareStageTransition();
            }
            else
            {
                ClearAllExistingBoards();
                CreateBoards();
                if (!isRestarting)
                {
                    ApplyCameraSettingsFromLevelData();
                }

                if (isRestarting && gameManager != null)
                {
                    gameManager.isRestarting = false;
                }
            }
        }
        else
        {
            Debug.LogError($"Didn't find json file: {resourcePath}");
        }
    }

    private void PrepareStageTransition()
    {
        DOTween.Kill(transform);
        oldStageContainer = new GameObject("OldStageContainer").transform;
        oldStageContainer.SetParent(transform.parent);
        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            child.SetParent(oldStageContainer);
        }

        newStageContainer = new GameObject("NewStageContainer").transform;
        newStageContainer.SetParent(transform);

        float screenWidth = Screen.width / 100f;
        newStageContainer.position = new Vector3(screenWidth * 1.2f, 0, 0);

        CreateBoards(newStageContainer);
        StartCoroutine(DelayedStageTransition());
    }

    private IEnumerator DelayedStageTransition()
    {
        yield return new WaitForSeconds(stageTransitionDelay);
        ClearAllExistingBoards();
        oldStageContainer = new GameObject("OldStageContainer").transform;
        oldStageContainer.SetParent(transform.parent);

        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            Vector3 worldPos = child.position;
            child.SetParent(oldStageContainer);
            child.position = worldPos;
        }

        newStageContainer = new GameObject("NewStageContainer").transform;
        newStageContainer.SetParent(transform);

        float screenWidth = Screen.width / 100f;
        newStageContainer.position = new Vector3(screenWidth * 1.2f, 0, 0);

        CreateBoards(newStageContainer);

        yield return new WaitForSeconds(0.1f);

        AnimateStageTransition();
        ApplyCameraSettingsFromLevelData();
    }

    private void ClearAllExistingBoards()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        GameObject oldContainer = GameObject.Find("OldStageContainer");
        if (oldContainer != null)
        {
            Destroy(oldContainer);
        }

        GameObject newContainer = GameObject.Find("NewStageContainer");
        if (newContainer != null)
        {
            Destroy(newContainer);
        }

        boardManagers.Clear();
    }

    private void AnimateStageTransition()
    {
        float screenWidth = Screen.width / 100f;
        float targetX = 0f;
        Sequence transitionSequence = DOTween.Sequence();
        transitionSequence.Append(oldStageContainer.DOMoveX(-screenWidth * 1.2f, stageTransitionDuration)
            .SetEase(stageTransitionEase));

        transitionSequence.Join(newStageContainer.DOMoveX(targetX, stageTransitionDuration)
            .SetEase(stageTransitionEase));

        transitionSequence.OnComplete(() =>
        {
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

            Destroy(oldStageContainer.gameObject);
            Destroy(newStageContainer.gameObject);
            StartCoroutine(UpdateAllBlocksPositionsDelayed());
            StartCoroutine(DelayedCameraAdjust());
        });
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
    }

    private List<CellType> ConvertToCellTypes(List<int> cellTypeIds)
    {
        if (cellTypeIds == null || cellTypeIds.Count == 0)
            return null;

        List<CellType> cellTypes = new List<CellType>();
        foreach (int typeId in cellTypeIds)
        {
            CellType type = CellType.Full; 

            switch (typeId)
            {
                case 0:
                    type = CellType.Full;
                    break;
                case 1:
                    type = CellType.TopRight;
                    break;
                case 2:
                    type = CellType.TopLeft;
                    break;
                case 3:
                    type = CellType.BottomRight;
                    break;
                case 4:
                    type = CellType.BottomLeft;
                    break;
            }
            cellTypes.Add(type);
        }

        return cellTypes;
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

        float hSpacing = currentLevelData.horizontalBoardSpacing > 0 
            ? currentLevelData.horizontalBoardSpacing 
            : horizontalBoardSpacing;
        float vSpacing = currentLevelData.verticalBoardSpacing > 0 
            ? currentLevelData.verticalBoardSpacing 
            : verticalBoardSpacing;
        
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

        float totalHeight = (boardsByRow.Count - 1) * vSpacing;
        float currentY = totalHeight / 2;

        foreach (var rowPair in boardsByRow)
        {
            List<BoardData> rowBoards = rowPair.Value;
            
            float totalRowWidth = (rowBoards.Count - 1) * hSpacing;
            float currentX = -totalRowWidth / 2;

            foreach (BoardData boardData in rowBoards)
            {
                GameObject boardObj = Instantiate(boardPrefab, Vector3.zero, Quaternion.identity, targetParent);
                boardObj.name = boardData.id;
                GridManager gridManager = boardObj.GetComponentInChildren<GridManager>();

                if (gridManager == null)
                {
                    continue;
                }

                gridManager.SetBoardId(boardData.id);
                if (boardData.hasCustomShape && boardData.customCells != null && boardData.customCells.Count > 0)
                {
                    List<CellType> cellTypeList = ConvertToCellTypes(boardData.cellTypes);
                    gridManager.CreateCustomGrid(boardData.rows, boardData.columns, boardData.customCells,
                        cellTypeList);
                }
                else
                {
                    gridManager.CreateGrid(boardData.rows, boardData.columns);
                }

                boardManagers.Add(gridManager);
                Vector3 boardPosition = new Vector3(currentX, currentY, 0);
                boardObj.transform.localPosition = boardPosition;
                boardObj.transform.rotation = Quaternion.Euler(-90, 0, 0);
                if (blockManager != null && boardData.blocks != null && boardData.blocks.Count > 0)
                {
                    blockManager.CreateBlocksForBoard(gridManager, boardData);
                }

                // Increment horizontal position using horizontal spacing
                currentX += hSpacing;
            }

            // Decrement vertical position using vertical spacing
            currentY -= vSpacing;
        }
    }

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
}