using UnityEngine;

public enum CellType
{
    Full = 0,
    TopRight = 1,
    TopLeft = 2,
    BottomRight = 3,
    BottomLeft = 4
}

public class GridCell : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color highlightColor = Color.yellow;
    #endregion
    
    #region Private Fields
    private Vector2Int coordinates;
    private CellType cellType;
    private bool isHighlighted = false;
    #endregion
    
    #region Public Properties
    public bool isOccupied { get; private set; } = false;
    public string occupiedByColor { get; private set; } = "";
    public Block occupiedByBlock { get; private set; } = null;
    #endregion
    
    #region Initialization
    public void Initialize(Vector2Int coords, CellType type = CellType.Full)
    {
        coordinates = coords;
        cellType = type;
    
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();
    }
    #endregion
    
    #region Public Methods
    public CellType GetCellType()
    {
        return cellType;
    }
    
    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }
    public void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;

        if (meshRenderer != null)
        {
            meshRenderer.material.color = highlighted ? highlightColor : normalColor;
        }
    }

    public bool IsHighlighted()
    {
        return isHighlighted;
    }
    public void SetOccupied(bool occupied, string color, Block block)
    {
        bool wasOccupied = isOccupied;
        if (isOccupied != occupied || occupiedByColor != color || occupiedByBlock != block)
        {
            if (isOccupied && !string.IsNullOrEmpty(occupiedByColor))
            {
                NotifyGridManagerOfCellChange(occupiedByColor, false);
            }
            isOccupied = occupied;
            occupiedByColor = occupied ? color : "";
            occupiedByBlock = occupied ? block : null;
            if (occupied && !string.IsNullOrEmpty(color))
            {
                NotifyGridManagerOfCellChange(color, true);
            }
            if (wasOccupied && !occupied)
            {
                SetHighlighted(false);
            }
        }
    }
    #endregion
    
    #region Private Methods
    private void NotifyGridManagerOfCellChange(string color, bool isOccupied)
    {
        GridManager gridManager = GetComponentInParent<GridManager>();
        if (gridManager != null)
        {
            gridManager.UpdateOccupiedCell(color, coordinates, isOccupied);
        }
    }
    #endregion
}