using System.Linq;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class MaterialBlock
    {
        public ClonedMaterialBlock[] clonedMaterialBlocks;

        public MaterialBlock(Material[] sharedMaterials, int count, AnimationTextureData textureData)
        {
            clonedMaterialBlocks = MaterialBlockCloner
                .CloneMaterialsWithTextures(sharedMaterials, count, textureData)
                .Select(m => ClonedMaterialBlock.Create(m))
                .ToArray();
        }
    }
}