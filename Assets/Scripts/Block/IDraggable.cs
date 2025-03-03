using UnityEngine;

public interface IDraggable
{
    void StartDrag();
    void OnDrag(Vector3 position);
    void EndDrag(Vector3 finalPosition);
    void ReturnToOrigin(Vector3 originalPosition);
    bool IsDraggable();
    bool IsCurrentlyDragging();
    Vector3 GetOriginalPosition();
}