using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    [SerializeField] private LayerMask blockLayer;
    [SerializeField] private BlockManager blockManager;
    [SerializeField] private float dragZOffset = -1f;

    private Block selectedBlock;
    private Vector3 dragOffset;
    private bool isDragging = false;

    private GridManager sourceGridManager;
    private string blockColor;
    private int blockPartCount;
    [SerializeField] private float returnDuration = 0.2f;
    [SerializeField] private bool useSnapEffect = true;
    [SerializeField] private float highlightSearchRadius = 2.0f;

    private void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    private void Update()
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
                        HandleTouchUp(touch.position);
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
                HandleTouchUp(Input.mousePosition);
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

        Ray ray = gameCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, blockLayer))
        {
            Block block = hit.collider.GetComponentInParent<Block>();
            if (block != null && !block.IsFixed() && !block.IsMoving())
            {
                GridCell closestCell = FindClosestGridCell(block.transform.position);
                if (closestCell != null)
                {
                    sourceGridManager = closestCell.GetComponentInParent<GridManager>();
                    if (sourceGridManager != null && sourceGridManager.IsCompleted())
                    {
                        sourceGridManager.FixAllBlocksOnBoard();
                        return;
                    }

                    blockColor = block.GetColor();
                    blockPartCount = block.GetBlockPartCount();
                    if (sourceGridManager != null)
                    {
                        sourceGridManager.RegisterBlockParts(blockColor, -blockPartCount);
                        sourceGridManager.PrintBoardStatus();
                    }
                }

                selectedBlock = block;
                selectedBlock.StartDrag();
                isDragging = true;

                Vector3 hitPointWorld = hit.point;
                dragOffset = block.transform.position - hitPointWorld;

                Vector3 newPosition = block.transform.position;
                newPosition.z += dragZOffset;
                block.transform.position = newPosition;

                selectedBlock.HighlightGridCells(true);
            }
        }
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

    private void HandleTouchUp(Vector2 screenPosition)
    {
        if (selectedBlock == null || !isDragging)
        {
            return;
        }

        GridManager targetGridManager = null;
        List<GridCell> highlightedCells = GetHighlightedCellsUnderBlock(out targetGridManager);
        bool validTarget = (highlightedCells != null);
        bool noOtherBlockPresent = validTarget && !IsAnotherBlockPresent(highlightedCells);

        bool allPartsHighlighted = validTarget && (highlightedCells.Count == selectedBlock.GetBlockPartCount());

        bool correctCellCount = validTarget && (highlightedCells.Count > 0) && allPartsHighlighted;

        if (!validTarget || !noOtherBlockPresent || !correctCellCount)
        {
            if (useSnapEffect)
            {
                ResetBlockPositionWithEffect();
            }
            else
            {
                ResetBlockPosition();
            }

            selectedBlock.HighlightGridCells(false);
            selectedBlock = null;
            isDragging = false;
            return;
        }

        if (useSnapEffect)
        {
            PlaceBlockOnAveragePositionWithEffect(highlightedCells);
        }
        else
        {
            PlaceBlockOnAveragePosition(highlightedCells);
        }

        if (targetGridManager != null)
        {
            targetGridManager.RegisterBlockParts(blockColor, blockPartCount);
        }

        selectedBlock.HighlightGridCells(false);
        selectedBlock = null;
        isDragging = false;
        sourceGridManager = null;
    }

    private void PlaceBlockOnAveragePositionWithEffect(List<GridCell> highlightedCells)
    {
        if (highlightedCells == null || highlightedCells.Count == 0 || selectedBlock == null)
        {
            Debug.LogWarning("PlaceBlockOnAveragePositionWithEffect: Invalid parameters");
            return;
        }

        Vector3 averagePosition = CalculateAveragePosition(highlightedCells);
        selectedBlock.originalPosition = averagePosition;
        selectedBlock.EndDragWithEffect(averagePosition);
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

        int totalHighlighted = 0;

        Collider[] hitColliders = Physics.OverlapSphere(selectedBlock.transform.position, highlightSearchRadius * 1.5f);
        Dictionary<GridManager, List<GridCell>> cellsByManager = new Dictionary<GridManager, List<GridCell>>();

        foreach (Collider col in hitColliders)
        {
            GridCell cell = col.GetComponent<GridCell>();
            if (cell != null)
            {
                if (cell.IsHighlighted())
                {
                    totalHighlighted++;

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

    private void PlaceBlockOnAveragePosition(List<GridCell> highlightedCells)
    {
        if (highlightedCells == null || highlightedCells.Count == 0 || selectedBlock == null)
        {
            return;
        }

        Vector3 averagePosition = CalculateAveragePosition(highlightedCells);
        selectedBlock.originalPosition = averagePosition;
        selectedBlock.EndDragSimple(averagePosition);
        selectedBlock.UpdateOccupiedCells();
    }

    private Vector3 CalculateAveragePosition(List<GridCell> cells)
    {
        if (cells.Count == 0 || selectedBlock == null)
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

    private void ResetBlockPositionWithEffect()
    {
        if (selectedBlock == null)
        {
            return;
        }

        Vector3 originalPos = selectedBlock.originalPosition;
        selectedBlock.ReturnToOriginWithEffect(originalPos);

        if (sourceGridManager != null && blockColor != null && blockPartCount > 0)
        {
            sourceGridManager.RegisterBlockParts(blockColor, blockPartCount);
        }
    }

    private void ResetBlockPosition()
    {
        if (selectedBlock == null)
        {
            return;
        }

        Vector3 originalPos = selectedBlock.originalPosition;
        selectedBlock.EndDragSimple(originalPos);

        if (sourceGridManager != null && blockColor != null && blockPartCount > 0)
        {
            sourceGridManager.RegisterBlockParts(blockColor, blockPartCount);
        }
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
}