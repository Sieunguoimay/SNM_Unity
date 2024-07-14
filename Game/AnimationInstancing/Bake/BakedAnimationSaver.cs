#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class BakedAnimationSaver
    {
        public static void SaveAll(
            List<AnimationInfo> animInfoList,
            ExtraBoneInfo extraBoneInfo,
            Texture2D[] bakedBoneTextures,
            string savePath)
        {
            var asset = ScriptableObject.CreateInstance<InstanceAnimationData>();
            asset.animInfoList = animInfoList;
            asset.extraBoneInfo = extraBoneInfo;
            asset.bakedBoneTextures = bakedBoneTextures;

            AssetDatabase.CreateAsset(asset, savePath);
            foreach (var t in asset.bakedBoneTextures)
            {
                AssetDatabase.AddObjectToAsset(t, asset);
            }
            AssetDatabase.SaveAssets();
        }
    }
}

#endif