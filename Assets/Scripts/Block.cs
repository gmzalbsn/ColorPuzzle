using System;
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
    public Vector3 originalPosition;
    private bool isDragging = false;
    public float orginalZOffset=0.005f;

    private List<GridCell> occupiedCells = new List<GridCell>();
    private void Start()
    {
        originalPosition=transform.localPosition;
    }

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
        Vector3 pos = transform.position;
        pos.z -= orginalZOffset;
        transform.position = pos;
        gridManager.RegisterBlockParts(blockColor, blockParts.Count);
        if (isFixed)
        {
            UpdateOccupiedCells();
        }
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
        center.z -= orginalZOffset; 
    
        transform.position = center;
        foreach (GameObject part in blockParts)
        {
            part.transform.position -= center;
            part.transform.parent = transform;
        }
    }
    public string GetColor()
    {
        return blockColor;
    }
    public bool IsFixed()
    {
        return isFixed;
    }
    public void SetFixed(bool fixState)
    {
        isFixed = fixState;
        foreach (GameObject part in blockParts)
        {
            MeshRenderer renderer = part.GetComponent<BlockPart>()?.meshRenderer;
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = fixState ? 0.7f : 1.0f;
                renderer.material.color = color;
            }
        }
    }
    public void StartDrag()
    {
        if (isFixed) return;
        GridManager sourceGridManager = null;
        if (occupiedCells.Count > 0 && occupiedCells[0] != null)
        {
            sourceGridManager = occupiedCells[0].GetComponentInParent<GridManager>();
            if (sourceGridManager != null)
            {
                sourceGridManager.RegisterBlockParts(blockColor, -blockParts.Count);
            }
        }
        ClearOccupiedCells();
    
        isDragging = true;
    }
    public void OnDrag(Vector3 position)
    {
        if (!isDragging || isFixed) return;
        
        transform.position = position;
    }
    
    public void EndDragSimple(Vector3 finalPosition)
    {
        if (!isDragging || isFixed)
        {
            return;
        }

        isDragging = false;
        finalPosition.z = originalPosition.z - orginalZOffset;
        transform.position = finalPosition;
        UpdateOccupiedCells();
    }

    public int GetBlockPartCount()
    {
        return blockParts.Count;
    }
    public void HighlightGridCells(bool highlighted)
    {
        if (!highlighted)
        {
            Collider[] allGridCells = Physics.OverlapSphere(transform.position, 30f);
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
    private void UpdateOccupiedCells()
    {
        ClearOccupiedCells();
        foreach (GameObject part in blockParts)
        {
            Vector3 partWorldPos = transform.position + part.transform.localPosition;
            Collider[] hitColliders = Physics.OverlapSphere(partWorldPos, 0.5f);
            
            foreach (Collider col in hitColliders)
            {
                GridCell cell = col.GetComponent<GridCell>();
                if (cell != null)
                {
                    cell.SetOccupied(true, blockColor, this);
                    occupiedCells.Add(cell);
                }
            }
        }
    }
    private void ClearOccupiedCells()
    {
        foreach (GridCell cell in occupiedCells)
        {
            if (cell != null)
            {
                cell.SetOccupied(false, "", null);
            }
        }
        occupiedCells.Clear();
    }
    
    private void OnDestroy()
    {
        ClearOccupiedCells();
    }
}