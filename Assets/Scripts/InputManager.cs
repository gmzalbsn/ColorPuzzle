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
        GridCell closestHighlightedCell = null;
        float closestDistance = float.MaxValue;

        Collider[] hitColliders = Physics.OverlapSphere(selectedBlock.transform.position, 1f);
        foreach (Collider col in hitColliders)
        {
            GridCell cell = col.GetComponent<GridCell>();
            if (cell != null && cell.IsHighlighted())
            {
                float distance = Vector3.Distance(selectedBlock.transform.position, cell.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestHighlightedCell = cell;
                }
            }
        }
        if (closestHighlightedCell != null)
        {
            Vector3 targetPos = closestHighlightedCell.transform.position;
            selectedBlock.EndDragSimple(targetPos);
        }
        else
        {
            selectedBlock.EndDragSimple(selectedBlock.originalPosition);
        }
        selectedBlock.HighlightGridCells(false);

        selectedBlock = null;
        isDragging = false;
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