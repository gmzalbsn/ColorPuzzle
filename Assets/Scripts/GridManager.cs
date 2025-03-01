using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject gridCellPrefab;
    [SerializeField] private float cellSpacing = 1.1f; 
    private Dictionary<Vector2Int, GridCell> gridCells = new Dictionary<Vector2Int, GridCell>();
    
    [SerializeField] private string boardId;
    private int totalCellCount;
    private Dictionary<string, int> blockPartsByColor = new Dictionary<string, int>();
    private Dictionary<string, int> occupiedCellsByColor = new Dictionary<string, int>();
    private bool isCompleted = false;
    public string GetBoardId() { return boardId; }
    public void SetBoardId(string id) { boardId = id; }
    public int GetTotalCellCount() { return totalCellCount; }
    public bool IsCompleted() { return isCompleted; }
    
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
    public void CreateCustomGrid(int rows, int columns, List<CellPosition> customCells)
    {
        ClearGrid();
        
        if (customCells != null && customCells.Count > 0)
        {
            foreach (CellPosition cell in customCells)
            {
                CreateCell(cell.x, cell.y);
            }
        }
        else
        {
            CreateGrid(rows, columns);
        }
        totalCellCount = gridCells.Count;
    }
    
    private void CreateCell(int col, int row)
    {
        Vector3 position = new Vector3(col, 0, row);
        GameObject cellObj = Instantiate(gridCellPrefab, position, Quaternion.identity, transform);
        cellObj.name = $"GridCell_{row}_{col}";
        
        GridCell cell = cellObj.GetComponent<GridCell>();
        if (cell == null)
            cell = cellObj.AddComponent<GridCell>();
            
        cell.Initialize(new Vector2Int(col, row));
        
        gridCells.Add(new Vector2Int(col, row), cell);
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
    Debug.Log($"GridManager {boardId}: Resetting all cells...");
    
    // Tüm işgal edilen hücreleri temizle ANCAK bundan önce registeredBlocks'u güncelle
    // yoksa sayılar birbirini tutmaz
    HashSet<Block> uniqueBlocks = new HashSet<Block>();
    
    foreach (var cellEntry in gridCells)
    {
        GridCell cell = cellEntry.Value;
        
        // Sadece işgal edilmiş hücreleri kontrol et
        if (cell.isOccupied && !string.IsNullOrEmpty(cell.occupiedByColor) && cell.occupiedByBlock != null)
        {
            // İşgal eden bloku kaydet
            uniqueBlocks.Add(cell.occupiedByBlock);
            
            // İşgal durumunu temizle
            cell.SetOccupied(false, "", null);
        }
        
        // Highlight durumunu temizle
        cell.SetHighlighted(false);
    }
    
    // Her blok için tekrar hücrelerin işgal durumunu güncelle
    foreach (Block block in uniqueBlocks)
    {
        // Blokun orijinal pozisyonunu korumasını sağla
        Vector3 originalPos = block.originalPosition;
        
        // Bloğun işgal ettiği hücreleri güncelle
        block.UpdateOccupiedCells();
        
        // Orijinal pozisyonu koru (UpdateOccupiedCells bunu değiştirmiş olabilir)
        block.originalPosition = originalPos;
    }
    
    // blockPartsByColor ve occupiedCellsByColor dictionary'lerini yeniden hesapla
    RecalculateBlocksAndCells();
    
    Debug.Log($"GridManager {boardId}: All cells reset completed.");
}

// GridManager sınıfına yeni bir yardımcı metod ekle
private void RecalculateBlocksAndCells()
{
    // Dictionary'leri temizle
    blockPartsByColor.Clear();
    occupiedCellsByColor.Clear();
    
    // Tüm hücreleri dolaş ve işgal durumunu kaydet
    foreach (var cellEntry in gridCells)
    {
        GridCell cell = cellEntry.Value;
        if (cell.isOccupied && !string.IsNullOrEmpty(cell.occupiedByColor))
        {
            // Bu renk için bir giriş yoksa oluştur
            if (!occupiedCellsByColor.ContainsKey(cell.occupiedByColor))
            {
                occupiedCellsByColor[cell.occupiedByColor] = 0;
            }
            
            // İşgal edilen hücre sayısını artır
            occupiedCellsByColor[cell.occupiedByColor]++;
        }
    }
    
    // Bloklardan gelen parça bilgilerini kaydet
    HashSet<Block> uniqueBlocks = new HashSet<Block>();
    foreach (var cellEntry in gridCells)
    {
        GridCell cell = cellEntry.Value;
        if (cell.isOccupied && cell.occupiedByBlock != null)
        {
            uniqueBlocks.Add(cell.occupiedByBlock);
        }
    }
    
    // Her blok için parça sayısını kaydet
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
    
    Debug.Log($"GridManager {boardId}: Recalculated blocks and cells - Block parts: {blockPartsByColor.Count} colors, Occupied cells: {occupiedCellsByColor.Count} colors");
    
    // Board tamamlanma durumunu kontrol et
    CheckBoardCompletion();
}
}