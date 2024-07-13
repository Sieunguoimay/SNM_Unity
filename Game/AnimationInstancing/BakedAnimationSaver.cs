using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class BakedAnimationSaver
    {
        public static void SaveAll(
            List<AnimationInfo> animInfoList,
            ExtraBoneInfo extraBoneInfo,
            Texture2D[] bakedBoneTexture)
        {
            UnityEditor.EditorUtility.ClearProgressBar();
        }
    }
}