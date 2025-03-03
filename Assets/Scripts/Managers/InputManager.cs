using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private Camera gameCamera;
    [SerializeField] private LayerMask blockLayer;
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private float dragZOffset = -1f;
    [SerializeField] private bool useSnapEffect = true;
    [SerializeField] private float highlightSearchRadius = 2.0f;
    #endregion

    #region Private Fields
    private Block selectedBlock;
    private Vector3 dragOffset;
    private bool isDragging = false;
    private GridManager sourceGridManager;
    private string blockColor;
    private int blockPartCount;
    #endregion

    #region Unity Lifecycle Methods
    private void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    private void Update()
    {
        HandleInput();
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchDown(touch.position);
                    break;

                case TouchPhase.Moved:
                    if (selectedBlock != null && isDragging)
                    {
                        HandleTouchDrag(touch.position);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (selectedBlock != null && isDragging)
                    {
                        HandleTouchUp();
                    }
                    break;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleTouchDown(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && selectedBlock != null && isDragging)
            {
                HandleTouchDrag(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0) && selectedBlock != null && isDragging)
            {
                HandleTouchUp();
            }
        }
    }

    private void HandleTouchDown(Vector2 screenPosition)
    {
        if (selectedBlock != null)
        {
            Debug.Log("Block is still processing, ignoring new touch");
            return;
        }

        Block block = FindBlockUnderTouch(screenPosition);
        if (block == null || block.IsFixed() || block.IsMoving())
        {
            return;
        }
        if (TryGetSourceGridManager(block, out GridManager gridManager))
        {
            if (gridManager.IsCompleted())
            {
                gridManager.FixAllBlocksOnBoard();
                return;
            }

            sourceGridManager = gridManager;
            blockColor = block.GetColor();
            blockPartCount = block.GetBlockPartCount();
            sourceGridManager.RegisterBlockParts(blockColor, -blockPartCount);
            sourceGridManager.PrintBoardStatus();
        }
        StartDraggingBlock(block, screenPosition);
    }

    private void HandleTouchDrag(Vector2 screenPosition)
    {
        if (selectedBlock == null || !isDragging)
        {
            return;
        }
        selectedBlock.HighlightGridCells(false);
        Vector3 worldPos = GetWorldPositionFromScreen(screenPosition);
        Vector3 targetPosition = worldPos + dragOffset;
        targetPosition.z = selectedBlock.transform.position.z;
        selectedBlock.OnDrag(targetPosition);
        selectedBlock.HighlightGridCells(true);
    }

    private void HandleTouchUp()
    {
        if (selectedBlock == null || !isDragging)
        {
            return;
        }
        GridManager targetGridManager = null;
        List<GridCell> highlightedCells = GetHighlightedCellsUnderBlock(out targetGridManager);
        bool isValidPlacement = ValidatePlacement(highlightedCells);

        if (isValidPlacement)
        {
            PlaceBlockOnGrid(highlightedCells, targetGridManager);
        }
        else
        {
            ResetBlockPosition();
        }
        selectedBlock.HighlightGridCells(false);
        selectedBlock = null;
        isDragging = false;
        sourceGridManager = null;
    }
    #endregion

    #region Block Interaction
    private Block FindBlockUnderTouch(Vector2 screenPosition)
    {
        Ray ray = gameCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, blockLayer))
        {
            return hit.collider.GetComponentInParent<Block>();
        }
        
        return null;
    }

    private void StartDraggingBlock(Block block, Vector2 screenPosition)
    {
        selectedBlock = block;
        selectedBlock.StartDrag();
        isDragging = true;
        Ray ray = gameCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, blockLayer))
        {
            Vector3 hitPointWorld = hit.point;
            dragOffset = block.transform.position - hitPointWorld;
        }
        Vector3 newPosition = block.transform.position;
        newPosition.z += dragZOffset;
        block.transform.position = newPosition;
        selectedBlock.HighlightGridCells(true);
    }

    private bool ValidatePlacement(List<GridCell> highlightedCells)
    {
        if (highlightedCells == null || selectedBlock == null)
        {
            return false;
        }
        bool noOtherBlockPresent = !IsAnotherBlockPresent(highlightedCells);
        bool allPartsHighlighted = (highlightedCells.Count == selectedBlock.GetBlockPartCount());
        bool correctCellCount = (highlightedCells.Count > 0) && allPartsHighlighted;
        
        return noOtherBlockPresent && correctCellCount;
    }

    private void PlaceBlockOnGrid(List<GridCell> highlightedCells, GridManager targetGridManager)
    {
        Vector3 averagePosition = CalculateAveragePosition(highlightedCells);
        selectedBlock.originalPosition = averagePosition;
        if (useSnapEffect)
        {
            selectedBlock.EndDragWithEffect(averagePosition);
        }
        else
        {
            selectedBlock.EndDragSimple(averagePosition);
            selectedBlock.UpdateOccupiedCells();
        }
        if (targetGridManager != null)
        {
            targetGridManager.RegisterBlockParts(blockColor, blockPartCount);
        }
    }

    private void ResetBlockPosition()
    {
        if (selectedBlock == null) return;
        
        Vector3 originalPos = selectedBlock.originalPosition;
        if (useSnapEffect)
        {
            selectedBlock.ReturnToOriginWithEffect(originalPos);
        }
        else
        {
            selectedBlock.EndDragSimple(originalPos);
        }
        if (sourceGridManager != null && blockColor != null && blockPartCount > 0)
        {
            sourceGridManager.RegisterBlockParts(blockColor, blockPartCount);
        }
    }
    #endregion

    #region Grid Interaction
    private bool TryGetSourceGridManager(Block block, out GridManager gridManager)
    {
        gridManager = null;
        if (block == null) return false;
        
        GridCell closestCell = FindClosestGridCell(block.transform.position);
        if (closestCell != null)
        {
            gridManager = closestCell.GetComponentInParent<GridManager>();
            return gridManager != null;
        }
        
        return false;
    }

    private bool IsAnotherBlockPresent(List<GridCell> highlightedCells)
    {
        if (selectedBlock == null) return false;

        foreach (GridCell cell in highlightedCells)
        {
            Collider[] colliders = Physics.OverlapSphere(cell.transform.position, 0.1f);
            foreach (Collider col in colliders)
            {
                Block otherBlock = col.GetComponentInParent<Block>();
                if (otherBlock != null && otherBlock != selectedBlock)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private List<GridCell> GetHighlightedCellsUnderBlock(out GridManager commonGridManager)
    {
        List<GridCell> highlightedCells = new List<GridCell>();
        commonGridManager = null;

        if (selectedBlock == null)
        {
            Debug.LogWarning("GetHighlightedCellsUnderBlock: selectedBlock is null");
            return null;
        }

        Collider[] hitColliders = Physics.OverlapSphere(selectedBlock.transform.position, highlightSearchRadius * 1.5f);
        Dictionary<GridManager, List<GridCell>> cellsByManager = new Dictionary<GridManager, List<GridCell>>();
        BlockCornerType blockCornerType = selectedBlock.GetCornerType();

        foreach (Collider col in hitColliders)
        {
            GridCell cell = col.GetComponent<GridCell>();
            if (cell != null && cell.IsHighlighted())
            {
                if (blockCornerType != BlockCornerType.Full)
                {
                    bool typeMatches = CheckCornerTypeMatch(blockCornerType, cell.GetCellType());
                    if (!typeMatches)
                    {
                        continue;
                    }
                }
                
                GridManager cellGridManager = cell.GetComponentInParent<GridManager>();
                if (cellGridManager != null)
                {
                    if (!cellsByManager.ContainsKey(cellGridManager))
                    {
                        cellsByManager[cellGridManager] = new List<GridCell>();
                    }

                    cellsByManager[cellGridManager].Add(cell);
                }
            }
        }
        int maxCellCount = 0;
        foreach (var pair in cellsByManager)
        {
            if (pair.Value.Count > maxCellCount)
            {
                maxCellCount = pair.Value.Count;
                commonGridManager = pair.Key;
                highlightedCells = pair.Value;
            }
        }

        if (highlightedCells.Count == 0)
        {
            return null;
        }

        return highlightedCells;
    }

    private bool CheckCornerTypeMatch(BlockCornerType blockType, CellType cellType)
    {
        if (blockType == BlockCornerType.Full || cellType == CellType.Full)
        {
            return true;
        }
        
        CellType equivalentCellType = CellType.Full;
        
        switch (blockType)
        {
            case BlockCornerType.TopRight:
                equivalentCellType = CellType.TopRight;
                break;
            case BlockCornerType.TopLeft:
                equivalentCellType = CellType.TopLeft;
                break;
            case BlockCornerType.BottomRight:
                equivalentCellType = CellType.BottomRight;
                break;
            case BlockCornerType.BottomLeft:
                equivalentCellType = CellType.BottomLeft;
                break;
        }
        
        return equivalentCellType == cellType;
    }

    private Vector3 CalculateAveragePosition(List<GridCell> cells)
    {
        if (cells == null || cells.Count == 0 || selectedBlock == null)
            return Vector3.zero;

        Vector3 averagePosition = Vector3.zero;
        foreach (GridCell cell in cells)
        {
            averagePosition += cell.transform.position;
        }

        averagePosition /= cells.Count;
        averagePosition.z = selectedBlock.originalPosition.z - selectedBlock.orginalZOffset;

        return averagePosition;
    }

    private GridCell FindClosestGridCell(Vector3 position)
    {
        Collider[] hitColliders = Physics.OverlapSphere(position, 1f);
        float closestDistance = float.MaxValue;
        GridCell closestCell = null;

        foreach (Collider col in hitColliders)
        {
            GridCell cell = col.GetComponent<GridCell>();
            if (cell != null)
            {
                float distance = Vector3.Distance(position, cell.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCell = cell;
                }
            }
        }

        return closestCell;
    }
    #endregion

    #region Utility Methods
    private Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
    {
        Ray ray = gameCamera.ScreenPointToRay(screenPosition);
        Plane plane;
        
        if (gameCamera.orthographic)
        {
            plane = new Plane(gameCamera.transform.forward, Vector3.zero);
        }
        else
        {
            plane = new Plane(Vector3.up, Vector3.zero);
        }

        float distance;
        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }
    #endregion
}