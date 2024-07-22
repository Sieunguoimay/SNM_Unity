using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    [PreferBinarySerialization]
    public class AnimationData : ScriptableObject
    {
        public List<AnimationInfo> animInfoList;
        public ExtraBoneInfo extraBoneInfo;
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
    public class ExtraBoneInfo
    {
        public string[] extraBoneNames;
        public Matrix4x4[] extraBindPoseMatrices;
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