using UnityEngine;
using System.Collections.Generic;

public class BlockManager : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Transform blocksParent;
    [SerializeField] private Material[] colorMaterials;
    #endregion

    #region Private Fields
    private Dictionary<string, Block> blocks = new Dictionary<string, Block>();
    #endregion

    #region Public Methods
    public void CreateBlocksForBoard(GridManager gridManager, BoardData boardData)
    {
        if (!ValidateInputs(gridManager, boardData))
        {
            return;
        }
        
        Transform boardBlocksParent = CreateBoardParent(gridManager, boardData);
        
        foreach (BlockData blockData in boardData.blocks)
        {
            Material colorMaterial = FindColorMaterial(blockData.color);
            if (colorMaterial != null)
            {
                CreateBlock(blockData, gridManager, colorMaterial, boardBlocksParent);
            }
        }
    }
    #endregion

    #region Private Methods
    private bool ValidateInputs(GridManager gridManager, BoardData boardData)
    {
        if (gridManager == null)
        {
            Debug.LogWarning("GridManager is null");
            return false;
        }
        
        if (boardData == null || boardData.blocks == null)
        {
            Debug.LogWarning("BoardData or boardData.blocks is null");
            return false;
        }

        return true;
    }

    private Transform CreateBoardParent(GridManager gridManager, BoardData boardData)
    {
        Transform boardBlocksParent = new GameObject($"Blocks_{boardData.id}").transform;
        boardBlocksParent.SetParent(gridManager.transform);
        return boardBlocksParent;
    }
    
    private void CreateBlock(BlockData blockData, GridManager gridManager, Material colorMaterial, Transform parent)
    {
        if (blockPrefab == null)
        {
            return;
        }
        
        GameObject blockObj = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity, parent);
        blockObj.name = $"{parent.name}_{blockData.id}";
        
        Block block = blockObj.GetComponent<Block>();
        if (block == null)
        {
            block = blockObj.AddComponent<Block>();
        }
        
        block.Initialize(blockData, gridManager, colorMaterial);
        
        string uniqueKey = $"{parent.name}_{blockData.id}";
        blocks[uniqueKey] = block;
    }

    private Material FindColorMaterial(string colorName)
    {
        if (colorMaterials == null || colorMaterials.Length == 0)
        {
            return null;
        }
        
        foreach (Material mat in colorMaterials)
        {
            if (mat == null) continue;
            
            if (mat.name.ToLower().Contains(colorName.ToLower()))
            {
                return mat;
            }
        }
        return null;
    }
    #endregion
}