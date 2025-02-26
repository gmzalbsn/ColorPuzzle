using UnityEngine;

public class GridCell : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color highlightColor = Color.yellow;
    private bool isHighlighted = false;
    private Vector2Int coordinates;
    private bool isOccupied = false;
    
    public void Initialize(Vector2Int coords)
    {
        coordinates = coords;
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();
        SetHighlighted(false);
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
}