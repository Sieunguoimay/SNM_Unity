using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    [PreferBinarySerialization]
    public class AnimationInstancingData : ScriptableObject
    {
        public List<AnimationInfo> animInfoList;
        public ExtraBoneData boneData;
        public AnimationTextureData animationTextureData;
    }

    [System.Serializable]
    public class AnimationTextureData
    {
        public Texture2D[] bakedBoneTextures;
        public int textureBlockWidth;
        public int textureBlockHeight;
    }

    [System.Serializable]
    public class ExtraBoneData
    {
        public string[] extraBones;
        public Matrix4x4[] extraBindPoses;
    }

    [System.Serializable]
    public class AnimationInfo
    {
        public string animationName;
        public int animationNameHash;
        public int totalFrame;
        public int fps;
        public int animationIndex;
        public int textureIndex;
        public bool rootMotion;
        public WrapMode wrapMode;
        public Vector3[] velocity;
        public Vector3[] angularVelocity;
        public List<AnimationEvent> eventList;

        public class ComparerHash : IComparer<AnimationInfo>
        {
            private readonly AnimationInfo compareTarget = new();

            public AnimationInfo CompareTarget => compareTarget;

            public int Compare(AnimationInfo x, AnimationInfo y)
            {
                return x.animationNameHash.CompareTo(y.animationNameHash);
            }
        }
    }

    [System.Serializable]
    public class AnimationEvent
    {
        public string function;
        public int intParameter;
        public float floatParameter;
        public string stringParameter;
        public string objectParameter;
        public float time;
    }
}