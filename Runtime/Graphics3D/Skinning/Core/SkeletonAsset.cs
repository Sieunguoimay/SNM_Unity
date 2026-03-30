using System;
using UnityEngine;

namespace Snm.Graphics3D.GPUSkinning
{
    public class SkeletonAsset : ScriptableObject
    {
        public Skeleton skeleton;
        public Mesh sourceMesh;
    }

    [Serializable]
    public class Skeleton
    {
        public Bone[] bones;
    }

    [Serializable]
    public class Bone
    {
        public string name;
        public int parent;
        public Matrix4x4 bindpose;
    }
}