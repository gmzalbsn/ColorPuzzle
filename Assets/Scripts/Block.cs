using System;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public enum BlockCornerType
{
    Full = 0,
    TopRight = 1,
    TopLeft = 2, 
    BottomRight = 3,
    BottomLeft = 4
}

public class Block : MonoBehaviour
{
    [SerializeField] private GameObject blockPartPrefab;
    [SerializeField] private GameObject fixedBlockPartPrefab;

    [Header("Corner Block Prefabs")] [SerializeField]
    private GameObject topRightBlockPrefab;

    [SerializeField] private GameObject topLeftBlockPrefab;
    [SerializeField] private GameObject bottomRightBlockPrefab;
    [SerializeField] private GameObject bottomLeftBlockPrefab;

    private string blockId;
    private string blockColor;
    public bool isFixed;
    private List<GameObject> blockParts = new List<GameObject>();
    private List<Vector2Int> gridPositions = new List<Vector2Int>();
    private Vector3 dragOffset;
    public Vector3 originalPosition;
    private bool isDragging = false;
    private bool isMoving = false;

    public float orginalZOffset = 0.005f;

    private List<GridCell> occupiedCells = new List<GridCell>();
    private Sequence currentTweenSequence;
    [SerializeField] private float snapDuration = 0.3f;
    [SerializeField] private float returnDuration = 0.25f;
    [SerializeField] private float highlightRadius = 0.4f;

    private BlockCornerType blockCornerType;

    private void Start()
    {
        UpdateOriginalPosition();
    }

    public void UpdateOriginalPosition()
    {
        originalPosition = transform.position;
    }

public void Initialize(BlockData data, GridManager gridManager, Material colorMaterial)
{
    blockId = data.id;
    blockColor = data.color;
    isFixed = data.isFixed;
    blockCornerType = (BlockCornerType)data.cornerTypeValue;
    
    Debug.Log($"Initialize: Block {blockId}, Color: {blockColor}, isFixed: {isFixed}, CornerType: {blockCornerType}");

    foreach (BlockPartPosition partPos in data.parts)
    {
        GridCell cell = gridManager.GetCell(partPos.gridX, partPos.gridY);
        if (cell == null) continue;
        gridPositions.Add(new Vector2Int(partPos.gridX, partPos.gridY));
        GameObject prefabToUse;

        if (blockCornerType == BlockCornerType.Full)
        {
            prefabToUse = isFixed ? fixedBlockPartPrefab : blockPartPrefab;
        }
        else
        {
            prefabToUse = GetPrefabForCornerType(blockCornerType);
        }
        GameObject part = Instantiate(prefabToUse, cell.GetWorldPosition(), Quaternion.identity, transform);
        part.name = $"Part_{partPos.gridX}_{partPos.gridY}_{blockCornerType}";
        BlockPart blockPart = part.GetComponent<BlockPart>();
        if (blockPart != null && blockPart.meshRenderer != null && colorMaterial != null)
        {
            blockPart.meshRenderer.material = colorMaterial;
            if (isFixed)
            {
                Color color = blockPart.meshRenderer.material.color;
                color.a = 0.7f;  
                blockPart.meshRenderer.material.color = color;
            }
        }
        else
        {
            MeshRenderer renderer = part.GetComponentInChildren<MeshRenderer>();
            if (renderer != null && colorMaterial != null)
            {
                renderer.material = colorMaterial;
                if (isFixed)
                {
                    Color color = renderer.material.color;
                    color.a = 0.7f;  
                    renderer.material.color = color;
                }
            }
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

    Invoke("UpdateOriginalPosition", 0.1f);
}
private GameObject GetPrefabForCornerType(BlockCornerType cornerType)
{
    
    switch (cornerType)
    {
        case BlockCornerType.TopRight:
            if (topRightBlockPrefab != null) {
                return topRightBlockPrefab;
            }
            break;
            
        case BlockCornerType.TopLeft:
            if (topLeftBlockPrefab != null) {
                return topLeftBlockPrefab;
            }
            break;
            
        case BlockCornerType.BottomRight:
            if (bottomRightBlockPrefab != null) {
                return bottomRightBlockPrefab;
            }
            break;
            
        case BlockCornerType.BottomLeft:
            if (bottomLeftBlockPrefab != null) {
                return bottomLeftBlockPrefab;
            }
            break;
    }
    return blockPartPrefab;
}
public BlockCornerType GetCornerType()
{
    return blockCornerType;
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

    public bool IsMoving()
    {
        return isMoving;
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
        KillAllTweens();
        ClearOccupiedCells();

        isDragging = true;
        isMoving = true;
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
        isMoving = false;
    }

    public void EndDragWithEffect(Vector3 finalPosition)
    {
        if (!isDragging || isFixed)
        {
            return;
        }

        isDragging = false;
        KillAllTweens();
        Vector3 currentPos = transform.position;
        float targetZ = finalPosition.z;

        Vector2 startXY = new Vector2(currentPos.x, currentPos.y);
        Vector2 targetXY = new Vector2(finalPosition.x, finalPosition.y);

        currentTweenSequence = DOTween.Sequence();
        currentTweenSequence.Append(
            DOTween.To(() => startXY, newPos => { transform.position = new Vector3(newPos.x, newPos.y, currentPos.z); },
                    targetXY, snapDuration * 0.85f)
                .SetEase(Ease.OutBack, 1.2f)
        );
        currentTweenSequence.Append(
            DOTween.To(() => currentPos.z,
                    newZ => { transform.position = new Vector3(finalPosition.x, finalPosition.y, newZ); }, targetZ,
                    snapDuration * 0.15f)
                .SetEase(Ease.Linear)
        );

        currentTweenSequence.OnComplete(() =>
        {
            transform.position = finalPosition;
            AudioManager.Instance.PlayPiecePlacedSound();
            UpdateOccupiedCells();

            currentTweenSequence = null;
            isMoving = false;
        });
    }

    public void ReturnToOriginWithEffect(Vector3 originalPos)
    {
        if (isFixed) return;
        KillAllTweens();
        Vector3 currentPos = transform.position;
        Vector2 startXY = new Vector2(currentPos.x, currentPos.y);
        Vector2 targetXY = new Vector2(originalPos.x, originalPos.y);

        float currentZ = currentPos.z;
        float targetZ = originalPos.z - orginalZOffset;

        currentTweenSequence = DOTween.Sequence();
        currentTweenSequence.Append(
            DOTween.To(() => currentZ, newZ => { transform.position = new Vector3(currentPos.x, currentPos.y, newZ); },
                    currentZ + 0.2f, returnDuration * 0.2f)
                .SetEase(Ease.OutQuad)
        );

        currentTweenSequence.Append(
            DOTween.To(() => startXY, newPos =>
                {
                    float progress = (newPos.x - startXY.x) / (targetXY.x - startXY.x);
                    if (float.IsNaN(progress)) progress = 0;
                    float heightOffset = 0.1f * Mathf.Sin(progress * Mathf.PI);

                    transform.position = new Vector3(
                        newPos.x,
                        newPos.y,
                        currentZ + 0.2f + heightOffset
                    );
                }, targetXY, returnDuration * 0.6f)
                .SetEase(Ease.InOutQuad)
        );
        currentTweenSequence.Append(
            DOTween.To(() => currentZ + 0.2f,
                    newZ => { transform.position = new Vector3(originalPos.x, originalPos.y, newZ); }, targetZ,
                    returnDuration * 0.2f)
                .SetEase(Ease.OutQuad)
        );

        currentTweenSequence.OnComplete(() =>
        {
            transform.position = new Vector3(originalPos.x, originalPos.y, targetZ);
            AudioManager.Instance.PlayPieceReturnSound();
            isDragging = false;
            isMoving = false;

            UpdateOccupiedCells();

            currentTweenSequence = null;
        });
    }

    private void KillAllTweens()
    {
        if (currentTweenSequence != null)
        {
            currentTweenSequence.Kill();
            currentTweenSequence = null;
        }

        DOTween.Kill(transform);
    }

    public int GetBlockPartCount()
    {
        return blockParts.Count;
    }
public void HighlightGridCells(bool highlighted)
{
    GridCell[] allCellsInScene = FindObjectsOfType<GridCell>();
    foreach (GridCell cell in allCellsInScene)
    {
        cell.SetHighlighted(false);
    }

    if (!highlighted || isFixed)
    {
        return;
    }

    float searchRadius = blockParts.Count <= 6 ? 1.5f : 1.1f;

    GridManager nearestGridManager = FindNearestGridManager();
    if (nearestGridManager == null)
    {
        return;
    }

    Dictionary<Vector2Int, GridCell> validCells = nearestGridManager.GetAllCells();
    if (validCells.Count == 0)
    {
        return;
    }

    List<GridCell> cellsToHighlight = new List<GridCell>();

    foreach (GameObject part in blockParts)
    {
        if (part == null) continue;

        Vector3 partWorldPos = transform.position + part.transform.localPosition;
        GridCell bestCell = null;
        float bestDistance = float.MaxValue;

        foreach (var entry in validCells)
        {
            GridCell cell = entry.Value;
            if (cell != null && !cellsToHighlight.Contains(cell))
            {
                if (blockCornerType != BlockCornerType.Full)
                {
                    bool typeMatches = (cell.GetCellType() == CellType.Full) || 
                                       (BlockCornerTypeMatchesCellType(blockCornerType, cell.GetCellType()));
                    
                    if (!typeMatches)
                    {
                        continue;
                    }
                }

                float distance = Vector3.Distance(partWorldPos, cell.transform.position);
                float effectiveRadius = (blockCornerType != BlockCornerType.Full) ? 1.2f : searchRadius;
                if (distance < effectiveRadius && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCell = cell;
                }
            }
        }

        if (bestCell != null && !cellsToHighlight.Contains(bestCell))
        {
            bestCell.SetHighlighted(true);
            cellsToHighlight.Add(bestCell);
        }
    }
}
private bool BlockCornerTypeMatchesCellType(BlockCornerType blockType, CellType cellType)
{
    switch (blockType)
    {
        case BlockCornerType.TopRight:
            return cellType == CellType.TopRight;
        case BlockCornerType.TopLeft:
            return cellType == CellType.TopLeft;
        case BlockCornerType.BottomRight:
            return cellType == CellType.BottomRight;
        case BlockCornerType.BottomLeft:
            return cellType == CellType.BottomLeft;
        case BlockCornerType.Full:
            return cellType == CellType.Full;
        default:
            return false;
    }
}

    private GridManager FindNearestGridManager()
    {
        GridManager[] gridManagers = FindObjectsOfType<GridManager>();
        if (gridManagers.Length == 0)
            return null;

        GridManager nearest = null;
        float minDistance = float.MaxValue;

        foreach (GridManager gm in gridManagers)
        {
            float distance = Vector3.Distance(transform.position, gm.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = gm;
            }
        }

        return nearest;
    }

    public void UpdateOccupiedCells()
    {
        ClearOccupiedCells();

        if (blockParts.Count == 0) return;
        int cellsFound = 0;

        foreach (GameObject part in blockParts)
        {
            if (part == null) continue;

            Vector3 partWorldPos = transform.position + part.transform.localPosition;
            Collider[] hitColliders = Physics.OverlapSphere(partWorldPos, highlightRadius);
            GridCell closestCell = null;
            float closestDistance = float.MaxValue;

            foreach (Collider col in hitColliders)
            {
                GridCell cell = col.GetComponent<GridCell>();
                if (cell != null)
                {
                    float distance = Vector3.Distance(partWorldPos, cell.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestCell = cell;
                    }
                }
            }

            if (closestCell != null)
            {
                closestCell.SetOccupied(true, blockColor, this);
                occupiedCells.Add(closestCell);
                cellsFound++;
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
        DOTween.Kill(transform);
        ClearOccupiedCells();
    }
}