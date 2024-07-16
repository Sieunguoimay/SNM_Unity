#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class BakedAnimationSaver
    {
        public static void SaveAll(
            InstanceAnimationData animationData,
            string savePath)
        {
            var asset = animationData;
            AssetDatabase.CreateAsset(asset, savePath);
            foreach (var t in asset.animationTextureData.bakedBoneTextures)
            {
                AssetDatabase.AddObjectToAsset(t, asset);
            }
            AssetDatabase.SaveAssets();
        }
    }
}

#endif