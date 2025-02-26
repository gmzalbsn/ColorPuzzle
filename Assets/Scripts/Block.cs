using UnityEngine;
using System.Collections.Generic;

public class Block : MonoBehaviour
{
    [SerializeField] private GameObject blockPartPrefab; 
    [SerializeField] private GameObject fixedBlockPartPrefab; 
    
    private string blockId;
    private string blockColor;
    public bool isFixed;
    private List<GameObject> blockParts = new List<GameObject>();
    private List<Vector2Int> gridPositions = new List<Vector2Int>();
    private Vector3 dragOffset;
    private Vector3 originalPosition;
    private bool isDragging = false;
    public void Initialize(BlockData data, GridManager gridManager, Material colorMaterial)
    {
        blockId = data.id;
        blockColor = data.color;
        isFixed = data.isFixed;
        foreach (BlockPartPosition partPos in data.parts)
        {
            GridCell cell = gridManager.GetCell(partPos.gridX, partPos.gridY);
            if (cell == null) continue;
            gridPositions.Add(new Vector2Int(partPos.gridX, partPos.gridY));
            GameObject prefabToUse = isFixed ? fixedBlockPartPrefab : blockPartPrefab;
            GameObject part = Instantiate(prefabToUse, cell.GetWorldPosition(), Quaternion.identity, transform);
            part.name = $"Part_{partPos.gridX}_{partPos.gridY}";
            MeshRenderer renderer = part.GetComponent<BlockPart>().meshRenderer;
            if (renderer != null && colorMaterial != null)
            {
                renderer.material = colorMaterial;
            }
            blockParts.Add(part);
            part.transform.rotation = Quaternion.Euler(-90, 0, 0);
        }
        RecalculatePosition();
    }
    private void RecalculatePosition()
    {
        if (blockParts.Count == 0) return;
        Vector3 center = Vector3.zero;
        foreach (GameObject part in blockParts)
        {
            center += part.transform.position;
        }
        center /= blockParts.Count;
        transform.position = center;
        foreach (GameObject part in blockParts)
        {
            part.transform.position -= center;
            part.transform.parent = transform;
        }
        originalPosition = transform.position;
    }
    public string GetColor()
    {
        return blockColor;
    }
    public bool IsFixed()
    {
        return isFixed;
    }
    public void StartDrag()
    {
        if (isFixed) return;
        
        isDragging = true;
    }
    public void OnDrag(Vector3 position)
    {
        if (!isDragging || isFixed) return;
        
        transform.position = position;
    }
    
    public void EndDragSimple(Vector3 finalPosition)
    {
        if (!isDragging || isFixed) return;
    
        isDragging = false;
        transform.position = finalPosition;
        RecalculatePosition();
    }
    public List<Vector2Int> GetOccupiedGridPositions()
    {
        return new List<Vector2Int>(gridPositions);
    }
    public void HighlightGridCells(bool highlighted)
    {
        if (!highlighted)
        {
            Collider[] allGridCells = Physics.OverlapSphere(transform.position, 10f);
            foreach (Collider col in allGridCells)
            {
                GridCell cell = col.GetComponent<GridCell>();
                if (cell != null)
                {
                    cell.SetHighlighted(false);
                }
            }
        }
        if (highlighted)
        {
            foreach (GameObject part in blockParts)
            {
                Vector3 partWorldPos = transform.position + part.transform.localPosition;
                Collider[] hitColliders = Physics.OverlapSphere(partWorldPos, 0.5f);
                foreach (Collider col in hitColliders)
                {
                    GridCell cell = col.GetComponent<GridCell>();
                    if (cell != null)
                    {
                        cell.SetHighlighted(true);
                    }
                }
            }
        }
    }
}