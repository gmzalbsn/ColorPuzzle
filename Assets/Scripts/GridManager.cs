using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject gridCellPrefab;
    [SerializeField] private float cellSpacing = 1.1f; 
    
    private Dictionary<Vector2Int, GridCell> gridCells = new Dictionary<Vector2Int, GridCell>();
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
}