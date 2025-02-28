using System.Collections.Generic;
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
    Ray ray = gameCamera.ScreenPointToRay(screenPosition);
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit, 100f, blockLayer))
    {
        Block block = hit.collider.GetComponentInParent<Block>();
        if (block != null && !block.IsFixed())
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
        bool correctCellCount = validTarget && (highlightedCells.Count > 0);
        if (!validTarget || !noOtherBlockPresent || !correctCellCount)
        {
            ResetBlockPosition();
            selectedBlock.HighlightGridCells(false);
            selectedBlock = null;
            isDragging = false;
            return;
        }
        PlaceBlockOnAveragePosition(highlightedCells);
    
        if (targetGridManager != null)
        {
            targetGridManager.RegisterBlockParts(blockColor, blockPartCount);
        }
    
        selectedBlock.HighlightGridCells(false);
        selectedBlock = null;
        isDragging = false;
        sourceGridManager = null;
    }
    private bool IsAnotherBlockPresent(List<GridCell> highlightedCells)
    {
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
    float searchRadius = 2.0f;

    Collider[] hitColliders = Physics.OverlapSphere(selectedBlock.transform.position, searchRadius);
    
    foreach (Collider col in hitColliders)
    {
        GridCell cell = col.GetComponent<GridCell>();
        if (cell != null)
        {
            
            if (cell.IsHighlighted())
            {
                highlightedCells.Add(cell);
                GridManager cellGridManager = cell.GetComponentInParent<GridManager>();
                
                if (commonGridManager == null)
                {
                    commonGridManager = cellGridManager;
                }
                else if (commonGridManager != cellGridManager)
                {
                    return null;
                }
            }
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
        if (highlightedCells.Count == 0)
            return;

        Vector3 averagePosition = Vector3.zero;

        foreach (GridCell cell in highlightedCells)
        {
            averagePosition += cell.transform.position;
        }

        averagePosition /= highlightedCells.Count;
        averagePosition.z = selectedBlock.originalPosition.z - selectedBlock.orginalZOffset;
        selectedBlock.EndDragSimple(averagePosition);
        selectedBlock.originalPosition = averagePosition;
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