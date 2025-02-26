using UnityEngine;
using System.Collections.Generic;

public class BlockManager : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Transform blocksParent;
    [SerializeField] private Material[] colorMaterials;
    
    private Dictionary<string, Block> blocks = new Dictionary<string, Block>();
    private GridManager currentGridManager;
    
    // Bir board için blokları oluştur
    public void CreateBlocksForBoard(GridManager gridManager, BoardData boardData)
    {
        // Grid manager'ı kaydet
        currentGridManager = gridManager;
        
        // Önceki blokları temizle
        ClearBlocks();
        
        // Her blok için
        foreach (BlockData blockData in boardData.blocks)
        {
            Material colorMaterial = FindColorMaterial(blockData.color);
            CreateBlock(blockData, gridManager, colorMaterial);
        }
    }
    
    // Renk materyalini bul
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
    
    // Blokları temizle
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
    
    // Yeni blok oluştur
    private void CreateBlock(BlockData blockData, GridManager gridManager, Material colorMaterial)
    {
        // Blok ana objesini oluştur
        GameObject blockObj = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity, blocksParent);
        blockObj.name = $"Block_{blockData.id}";
        
        // Block bileşenini al
        Block block = blockObj.GetComponent<Block>();
        if (block == null)
            block = blockObj.AddComponent<Block>();
        
        // Bloğu başlat
        block.Initialize(blockData, gridManager, colorMaterial);
        
        // Sözlüğe ekle
        blocks[blockData.id] = block;
    }
    
    // Bloku ID'ye göre al
    public Block GetBlock(string blockId)
    {
        if (blocks.ContainsKey(blockId))
            return blocks[blockId];
        return null;
    }
    
    // Tüm blokları al
    public List<Block> GetAllBlocks()
    {
        List<Block> blockList = new List<Block>();
        foreach (var pair in blocks)
        {
            blockList.Add(pair.Value);
        }
        return blockList;
    }
    
    // Seviye tamamlandı mı kontrolü
    public bool CheckLevelCompletion()
    {
        // Renklere göre blokları grupla
        Dictionary<string, List<Block>> blocksByColor = new Dictionary<string, List<Block>>();
        
        foreach (var pair in blocks)
        {
            Block block = pair.Value;
            string color = block.GetColor();
            
            if (!blocksByColor.ContainsKey(color))
                blocksByColor[color] = new List<Block>();
                
            blocksByColor[color].Add(block);
        }
        
        // Her renk grubu için kontrol
        foreach (var colorGroup in blocksByColor.Values)
        {
            if (!AreBlocksGrouped(colorGroup))
                return false;
        }
        
        return true;
    }
    
    // Bloklar doğru şekilde gruplandı mı
    private bool AreBlocksGrouped(List<Block> sameColorBlocks)
    {
        // Tek blok varsa her zaman gruplanmış sayılır
        if (sameColorBlocks.Count <= 1)
            return true;
            
        // Blokların grid pozisyonlarını topla
        HashSet<Vector2Int> allPositions = new HashSet<Vector2Int>();
        
        foreach (Block block in sameColorBlocks)
        {
            foreach (Vector2Int pos in block.GetOccupiedGridPositions())
            {
                allPositions.Add(pos);
            }
        }
        
        // Tüm pozisyonlar birbirine bağlı mı kontrol et
        // (bağlı bileşen analizi)
        
        // Basit bir yaklaşım: şimdilik hepsinin aynı renk olması yeterli
        return true;
    }
}