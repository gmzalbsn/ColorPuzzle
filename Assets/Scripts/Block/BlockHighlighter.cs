using System.Collections.Generic;
using UnityEngine;

public static class BlockHighlighter
{
    public static List<GridCell> HighlightAvailableCells(Transform blockTransform, List<GameObject> blockParts, BlockCornerType blockCornerType, GridManager gridManager, float searchRadius)
    {
        List<GridCell> cellsToHighlight = new List<GridCell>();
        
        if (gridManager == null || blockParts.Count == 0)
        {
            return cellsToHighlight;
        }

        Dictionary<Vector2Int, GridCell> validCells = gridManager.GetAllCells();
        if (validCells.Count == 0)
        {
            return cellsToHighlight;
        }

        foreach (GameObject part in blockParts)
        {
            if (part == null) continue;

            Vector3 partWorldPos = blockTransform.position + part.transform.localPosition;
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

        return cellsToHighlight;
    }

    public static void ClearAllHighlights(List<GridCell> cells)
    {
        foreach (GridCell cell in cells)
        {
            if (cell != null)
            {
                cell.SetHighlighted(false);
            }
        }
    }

    private static bool BlockCornerTypeMatchesCellType(BlockCornerType blockType, CellType cellType)
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
}