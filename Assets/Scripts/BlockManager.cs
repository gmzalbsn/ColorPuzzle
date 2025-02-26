using UnityEngine;
using System.Collections.Generic;

public class BlockManager : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Transform blocksParent;
    [SerializeField] private Material[] colorMaterials;
    
    private Dictionary<string, Block> blocks = new Dictionary<string, Block>();
    private GridManager currentGridManager;
    public void CreateBlocksForBoard(GridManager gridManager, BoardData boardData)
    {
        currentGridManager = gridManager;
        ClearBlocks();
        foreach (BlockData blockData in boardData.blocks)
        {
            Material colorMaterial = FindColorMaterial(blockData.color);
            CreateBlock(blockData, gridManager, colorMaterial);
        }
    }
    private Material FindColorMaterial(string colorName)
    {
        foreach (Material mat in colorMaterials)
        {
            if (mat.name.ToLower().Contains(colorName.ToLower()))
            {
                return mat;
            }
        }
        return null;
    }
    private void ClearBlocks()
    {
        blocks.Clear();
        
        if (blocksParent != null)
        {
            foreach (Transform child in blocksParent)
            {
                Destroy(child.gameObject);
            }
        }
    }
    private void CreateBlock(BlockData blockData, GridManager gridManager, Material colorMaterial)
    {
        GameObject blockObj = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity, blocksParent);
        blockObj.name = $"Block_{blockData.id}";
        Block block = blockObj.GetComponent<Block>();
        if (block == null)
            block = blockObj.AddComponent<Block>();
        block.Initialize(blockData, gridManager, colorMaterial);
        blocks[blockData.id] = block;
    }
}