using System;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning.Serialize
{
    public class SkeletonAsset : ScriptableObject
    {
        public Skeleton skeleton;
    }

    [Serializable]
    public class Skeleton
    {
        public Bone[] bones;
    }

    [Serializable]
    public class Bone
    {
        public int parent;
        public Matrix4x4 bindpose;
    }
}