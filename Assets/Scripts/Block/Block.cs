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

public class Block : MonoBehaviour, IDraggable
{
    #region Serialized Fields
    [SerializeField] private GameObject blockPartPrefab;
    [SerializeField] private GameObject fixedBlockPartPrefab;

    [Header("Corner Block Prefabs")] 
    [SerializeField] private GameObject topRightBlockPrefab;
    [SerializeField] private GameObject topLeftBlockPrefab;
    [SerializeField] private GameObject bottomRightBlockPrefab;
    [SerializeField] private GameObject bottomLeftBlockPrefab;
    
    [SerializeField] private float snapDuration = 0.3f;
    [SerializeField] private float returnDuration = 0.25f;
    [SerializeField] private float highlightRadius = 0.4f;
    #endregion
    
    #region Private Fields
    private string blockColor;
    private BlockCornerType blockCornerType;
    private List<GameObject> blockParts = new List<GameObject>();
    private List<IBlockPart> blockPartComponents = new List<IBlockPart>();
    private List<Vector2Int> gridPositions = new List<Vector2Int>();
    private Vector3 dragOffset;
    private bool isDragging = false;
    private bool isMoving = false;
    
    private List<GridCell> occupiedCells = new List<GridCell>();
    private List<GridCell> highlightedCells = new List<GridCell>();
    private Sequence currentTweenSequence;
    #endregion
    
    #region Public Properties
    public bool isFixed;
    public Vector3 originalPosition;
    public float orginalZOffset = 0.005f;
    #endregion

    #region Lifecycle Methods
    private void Start()
    {
        UpdateOriginalPosition();
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
        ClearOccupiedCells();
    }
    #endregion

    #region Initialization
    public void UpdateOriginalPosition()
    {
        originalPosition = transform.position;
    }

    public void Initialize(BlockData data, GridManager gridManager, Material colorMaterial)
    {
        blockColor = data.color;
        isFixed = data.isFixed;
        blockCornerType = (BlockCornerType)data.cornerTypeValue;
        
        foreach (BlockPartPosition partPos in data.parts)
        {
            GridCell cell = gridManager.GetCell(partPos.gridX, partPos.gridY);
            if (cell == null) continue;
            
            gridPositions.Add(new Vector2Int(partPos.gridX, partPos.gridY));
            GameObject prefabToUse = GetPrefabForCornerType(blockCornerType, isFixed);
            
            GameObject part = Instantiate(prefabToUse, cell.GetWorldPosition(), Quaternion.identity, transform);
            part.name = $"Part_{partPos.gridX}_{partPos.gridY}_{blockCornerType}";
            
            IBlockPart blockPart = part.GetComponent<IBlockPart>();
            if (blockPart != null)
            {
                blockPartComponents.Add(blockPart);
                blockPart.SetMaterial(colorMaterial, isFixed);
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

    private GameObject GetPrefabForCornerType(BlockCornerType cornerType, bool isFixed)
    {
        if (isFixed && fixedBlockPartPrefab != null && cornerType == BlockCornerType.Full)
        {
            return fixedBlockPartPrefab;
        }
        if (cornerType == BlockCornerType.Full)
        {
            return blockPartPrefab;
        }
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
    #endregion

    #region Public Methods
    public BlockCornerType GetCornerType()
    {
        return blockCornerType;
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

    public int GetBlockPartCount()
    {
        return blockParts.Count;
    }

    public void SetFixed(bool fixState)
    {
        isFixed = fixState;
        foreach (BlockPart blockPart in blockPartComponents)
        {
            Material currentMaterial = blockPart.GetComponent<BlockPart>()?.meshRenderer?.material;
            if (currentMaterial != null)
            {
                blockPart.SetMaterial(currentMaterial, fixState);
            }
        }
    }
    #endregion

    #region Highlighting
    public void HighlightGridCells(bool highlighted)
    {
        BlockHighlighter.ClearAllHighlights(highlightedCells);
        highlightedCells.Clear();

        if (!highlighted || isFixed)
        {
            return;
        }

        float searchRadius = blockParts.Count <= 6 ? 1.1f : 1.1f;
        GridManager nearestGridManager = FindNearestGridManager();
        
        if (nearestGridManager != null)
        {
            highlightedCells = BlockHighlighter.HighlightAvailableCells(
                transform, 
                blockParts, 
                blockCornerType, 
                nearestGridManager, 
                searchRadius
            );
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
    #endregion
    
    #region IDraggable Implementation
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
    
    public void EndDrag(Vector3 finalPosition)
    {
        EndDragWithEffect(finalPosition);
    }
    
    public void ReturnToOrigin(Vector3 originalPosition)
    {
        ReturnToOriginWithEffect(originalPosition);
    }
    
    public bool IsDraggable()
    {
        return !isFixed;
    }
    
    public bool IsCurrentlyDragging()
    {
        return isDragging;
    }
    
    public Vector3 GetOriginalPosition()
    {
        return originalPosition;
    }
    
    #endregion
    
    #region Drag Implementation Details
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
    #endregion

    #region Cell Occupation
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
    #endregion
}