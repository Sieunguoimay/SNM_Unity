using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class MaterialBlock
    {
        public ClonedMaterialBlock[] clonedMaterialBlocks;
#if UNITY_EDITOR
        public Material[] sharedMaterials;
#endif

        public MaterialBlock(IEnumerable<Material[]> clonedMaterials, Material[] sharedMaterials)
        {
            clonedMaterialBlocks = clonedMaterials
                .Select(m => new ClonedMaterialBlock(m))
                .ToArray();
#if UNITY_EDITOR
            this.sharedMaterials = sharedMaterials;
#endif
        }

        public static IEnumerable<Material[]> CloneMaterialsWithTextures(
            Material[] materials, int count, AnimationTextureData textureData)
        {
            for (var textureIndex = 0; textureIndex < count; textureIndex++)
            {
                var copyMaterials = new Material[count];

                for (int i = 0; i != count; ++i)
                {
                    copyMaterials[i] = new Material(materials[i]);
#if UNITY_5_6_OR_NEWER
                    copyMaterials[i].enableInstancing = true;
#endif
                    // if (useInstancing)
                    copyMaterials[i].EnableKeyword("SKINNED_INSTANCING_ON");
                    // else
                    //     copyMaterials[i].DisableKeyword("SKINNED_INSTANCING_ON");

                    copyMaterials[i].EnableKeyword("USE_CONSTANT_BUFFER");
                    copyMaterials[i].DisableKeyword("USE_COMPUTE_BUFFER");

                    if (textureData != null)
                    {
                        copyMaterials[i].SetTexture("_boneTexture", textureData.bakedBoneTextures[textureIndex]);
                        copyMaterials[i].SetInt("_boneTextureWidth", textureData.bakedBoneTextures[textureIndex].width);
                        copyMaterials[i].SetInt("_boneTextureHeight", textureData.bakedBoneTextures[textureIndex].height);
                        copyMaterials[i].SetInt("_boneTextureBlockWidth", textureData.textureBlockWidth);
                        copyMaterials[i].SetInt("_boneTextureBlockHeight", textureData.textureBlockHeight);
                    }
                }

                yield return copyMaterials;
            }
        }
    }
}