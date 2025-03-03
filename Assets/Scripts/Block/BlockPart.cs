using UnityEngine;

public interface IBlockPart
{
    BlockCornerType GetCornerType();
    void SetMaterial(Material material, bool isFixed);
}
public class BlockPart : MonoBehaviour, IBlockPart
{
    public MeshRenderer meshRenderer;
    [SerializeField] private BlockCornerType cornerType = BlockCornerType.Full;
    
    public BlockCornerType GetCornerType()
    {
        return cornerType;
    }
    
    public void SetMaterial(Material material, bool isFixed = false)
    {
        if (meshRenderer != null && material != null)
        {
            meshRenderer.material = material;
            if (isFixed)
            {
                Color color = meshRenderer.material.color;
                color.a = 0.7f;
                meshRenderer.material.color = color;
            }
        }
    }
}