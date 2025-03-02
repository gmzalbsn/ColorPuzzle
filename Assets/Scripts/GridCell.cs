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
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color highlightColor = Color.yellow;
    private bool isHighlighted = false;
    private Vector2Int coordinates;
    public bool isOccupied = false;
    public string occupiedByColor = "";
    public Block occupiedByBlock = null;
    private CellType cellType;
    private bool isPartOfFullSquare = false;
    public void Initialize(Vector2Int coords, CellType type = CellType.Full)
    {
        coordinates = coords;
        cellType = type;
    
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

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
                GridManager gridManager = GetComponentInParent<GridManager>();
                if (gridManager != null)
                {
                    gridManager.UpdateOccupiedCell(occupiedByColor, coordinates, false);
                }
            }

            isOccupied = occupied;
            occupiedByColor = occupied ? color : "";
            occupiedByBlock = occupied ? block : null;

            if (occupied && !string.IsNullOrEmpty(color))
            {
                GridManager gridManager = GetComponentInParent<GridManager>();
                if (gridManager != null)
                {
                    gridManager.UpdateOccupiedCell(color, coordinates, true);
                }
            }

            if (wasOccupied && !occupied)
            {
                SetHighlighted(false);
            }
        }
    }
}