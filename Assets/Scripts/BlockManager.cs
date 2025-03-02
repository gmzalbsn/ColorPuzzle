using UnityEngine;
using System.Collections.Generic;

public class BlockManager : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Transform blocksParent;
    [SerializeField] private Material[] colorMaterials;

    private Dictionary<string, Block> blocks = new Dictionary<string, Block>();

     public void CreateBlocksForBoard(GridManager gridManager, BoardData boardData)
    {
        if (gridManager == null)
        {
            return;
        }
        
        if (boardData == null || boardData.blocks == null)
        {
            return;
        }
        
        Transform boardBlocksParent = new GameObject($"Blocks_{boardData.id}").transform;
        boardBlocksParent.SetParent(gridManager.transform);

        foreach (BlockData blockData in boardData.blocks)
        {
            Material colorMaterial = FindColorMaterial(blockData.color);
            if (colorMaterial != null)
            {
                CreateBlock(blockData, gridManager, colorMaterial, boardBlocksParent);
            }
        }
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
}