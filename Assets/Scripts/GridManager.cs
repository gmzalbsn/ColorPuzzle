using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject gridCellPrefab;
    [Header("Half Cell Prefabs")]
    [SerializeField] private GameObject topRightPrefab;
    [SerializeField] private GameObject topLeftPrefab;
    [SerializeField] private GameObject bottomRightPrefab;
    [SerializeField] private GameObject bottomLeftPrefab;
    
    [SerializeField] private float cellSpacing = 1.1f;
    private Dictionary<Vector2Int, GridCell> gridCells = new Dictionary<Vector2Int, GridCell>();

    [SerializeField] private string boardId;
    private int totalCellCount;
    private Dictionary<string, int> blockPartsByColor = new Dictionary<string, int>();
    private Dictionary<string, int> occupiedCellsByColor = new Dictionary<string, int>();
    private bool isCompleted = false;

    public string GetBoardId()
    {
        return boardId;
    }

    public void SetBoardId(string id)
    {
        boardId = id;
    }

    public int GetTotalCellCount()
    {
        return totalCellCount;
    }

    public bool IsCompleted()
    {
        return isCompleted;
    }

    [SerializeField] private GameObject completionEffectPrefab;

    public Dictionary<Vector2Int, GridCell> GetAllCells()
    {
        return gridCells;
    }

    public void CreateGrid(int rows, int columns)
    {
        ClearGrid();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                CreateCell(col, row);
            }
        }

        totalCellCount = gridCells.Count;
    }

    public void CreateCustomGrid(int rows, int columns, List<CellPosition> customCells, List<CellType> cellTypes = null)
    {
        ClearGrid();

        if (customCells != null && customCells.Count > 0)
        {
            for (int i = 0; i < customCells.Count; i++)
            {
                CellPosition cell = customCells[i];
                CellType cellType = CellType.Full;
                
                if (cellTypes != null && i < cellTypes.Count)
                {
                    cellType = cellTypes[i];
                }

                CreateCell(cell.x, cell.y, cellType);
            }
        }
        else
        {
            CreateGrid(rows, columns);
        }

        totalCellCount = gridCells.Count;
    }

    private void CreateCell(int col, int row, CellType cellType = CellType.Full)
    {
        Vector3 position = new Vector3(col * cellSpacing, 0, row * cellSpacing);
        GameObject prefabToUse = GetCellPrefab(cellType);
        GameObject cellObj = Instantiate(prefabToUse, position, Quaternion.identity, transform);
        cellObj.name = $"GridCell_{row}_{col}_{cellType}";
        GridCell cell = cellObj.GetComponent<GridCell>();
        if (cell == null)
            cell = cellObj.AddComponent<GridCell>();
        cell.Initialize(new Vector2Int(col, row), cellType);
        gridCells.Add(new Vector2Int(col, row), cell);
    }
    private GameObject GetCellPrefab(CellType cellType)
    {
        switch (cellType)
        {
            case CellType.Full:
                return gridCellPrefab;
            case CellType.TopRight:
                return topRightPrefab != null ? topRightPrefab : gridCellPrefab;
            case CellType.TopLeft:
                return topLeftPrefab != null ? topLeftPrefab : gridCellPrefab;
            case CellType.BottomRight:
                return bottomRightPrefab != null ? bottomRightPrefab : gridCellPrefab;
            case CellType.BottomLeft:
                return bottomLeftPrefab != null ? bottomLeftPrefab : gridCellPrefab;
            default:
                return gridCellPrefab;
        }
    }

    private void ClearGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        gridCells.Clear();
    }

    public GridCell GetCell(int x, int y)
    {
        Vector2Int key = new Vector2Int(x, y);
        if (gridCells.ContainsKey(key))
            return gridCells[key];
        return null;
    }

    public void RegisterBlockParts(string color, int partCount)
    {
        if (string.IsNullOrEmpty(color))
        {
            return;
        }

        if (!blockPartsByColor.ContainsKey(color))
        {
            blockPartsByColor[color] = 0;
        }

        blockPartsByColor[color] += partCount;
        if (blockPartsByColor[color] <= 0)
        {
            blockPartsByColor.Remove(color);
        }

        CheckBoardCompletion();
    }

    public void UpdateOccupiedCell(string color, Vector2Int cellPosition, bool isOccupied)
    {
        if (!occupiedCellsByColor.ContainsKey(color))
        {
            occupiedCellsByColor[color] = 0;
        }

        if (isOccupied)
        {
            occupiedCellsByColor[color]++;
        }
        else
        {
            occupiedCellsByColor[color]--;
            if (occupiedCellsByColor[color] < 0)
                occupiedCellsByColor[color] = 0;
        }

        CheckBoardCompletion();
    }

    private void CheckBoardCompletion()
    {
        if (totalCellCount == 0)
        {
            isCompleted = false;
            return;
        }

        int totalPartCount = 0;
        foreach (var entry in blockPartsByColor)
        {
            totalPartCount += entry.Value;
        }

        if (totalPartCount == totalCellCount)
        {
            if (blockPartsByColor.Count == 1)
            {
                if (!isCompleted)
                {
                    isCompleted = true;
                    LevelLoader.completedBoardsAmount += 1;
                    SpawnCompletionEffect();
                    AudioManager.Instance.PlayBlockCompletedSound();

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnBoardCompleted();
                    }

                    FixAllBlocksOnBoard();
                }

                return;
            }
        }

        if (isCompleted)
        {
            isCompleted = false;
            LevelLoader.completedBoardsAmount -= 1;
            if (LevelLoader.completedBoardsAmount < 0)
                LevelLoader.completedBoardsAmount = 0;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBoardCompleted();
            }
        }
    }

    private void SpawnCompletionEffect()
    {
        if (completionEffectPrefab != null)
        {
            Vector3 boardCenter = CalculateBoardCenter();
            Vector3 effectPosition = new Vector3(boardCenter.x, boardCenter.y + 0.5f, boardCenter.z);

            GameObject effectInstance = Instantiate(completionEffectPrefab, effectPosition, Quaternion.identity);
            Destroy(effectInstance, 3f);
        }
    }

    private Vector3 CalculateBoardCenter()
    {
        if (gridCells.Count == 0)
            return transform.position;
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var cellEntry in gridCells)
        {
            GridCell cell = cellEntry.Value;
            if (cell != null)
            {
                Vector3 cellPos = cell.transform.position;

                min.x = Mathf.Min(min.x, cellPos.x);
                min.y = Mathf.Min(min.y, cellPos.y);
                min.z = Mathf.Min(min.z, cellPos.z);

                max.x = Mathf.Max(max.x, cellPos.x);
                max.y = Mathf.Max(max.y, cellPos.y);
                max.z = Mathf.Max(max.z, cellPos.z);
            }
        }

        Vector3 center = (min + max) * 0.5f;
        return center;
    }

    public void PrintBoardStatus()
    {
        int totalPartCount = 0;

        if (blockPartsByColor.Count == 0)
        {
        }
        else
        {
            foreach (var entry in blockPartsByColor)
            {
                string color = entry.Key;
                int partCount = entry.Value;
                totalPartCount += partCount;
            }
        }
    }

    public void FixAllBlocksOnBoard()
    {
        HashSet<Block> uniqueBlocks = new HashSet<Block>();
        foreach (var cellEntry in gridCells)
        {
            GridCell cell = cellEntry.Value;
            if (cell.isOccupied)
            {
                Block block = cell.occupiedByBlock;
                if (block != null && !uniqueBlocks.Contains(block))
                {
                    uniqueBlocks.Add(block);
                }
            }
        }

        foreach (Block block in uniqueBlocks)
        {
            if (!block.IsFixed())
            {
                block.SetFixed(true);
            }
        }
    }

    public void ResetAllCells()
    {
        HashSet<Block> uniqueBlocks = new HashSet<Block>();

        foreach (var cellEntry in gridCells)
        {
            GridCell cell = cellEntry.Value;

            if (cell.isOccupied && !string.IsNullOrEmpty(cell.occupiedByColor) && cell.occupiedByBlock != null)
            {
                uniqueBlocks.Add(cell.occupiedByBlock);

                cell.SetOccupied(false, "", null);
            }

            cell.SetHighlighted(false);
        }

        foreach (Block block in uniqueBlocks)
        {
            Vector3 originalPos = block.originalPosition;
            block.UpdateOccupiedCells();
            block.originalPosition = originalPos;
        }

        RecalculateBlocksAndCells();
    }

    private void RecalculateBlocksAndCells()
    {
        blockPartsByColor.Clear();
        occupiedCellsByColor.Clear();

        foreach (var cellEntry in gridCells)
        {
            GridCell cell = cellEntry.Value;
            if (cell.isOccupied && !string.IsNullOrEmpty(cell.occupiedByColor))
            {
                if (!occupiedCellsByColor.ContainsKey(cell.occupiedByColor))
                {
                    occupiedCellsByColor[cell.occupiedByColor] = 0;
                }

                occupiedCellsByColor[cell.occupiedByColor]++;
            }
        }

        HashSet<Block> uniqueBlocks = new HashSet<Block>();
        foreach (var cellEntry in gridCells)
        {
            GridCell cell = cellEntry.Value;
            if (cell.isOccupied && cell.occupiedByBlock != null)
            {
                uniqueBlocks.Add(cell.occupiedByBlock);
            }
        }

        foreach (Block block in uniqueBlocks)
        {
            string blockColor = block.GetColor();
            int blockPartCount = block.GetBlockPartCount();

            if (!blockPartsByColor.ContainsKey(blockColor))
            {
                blockPartsByColor[blockColor] = 0;
            }

            blockPartsByColor[blockColor] += blockPartCount;
        }

        CheckBoardCompletion();
    }
}