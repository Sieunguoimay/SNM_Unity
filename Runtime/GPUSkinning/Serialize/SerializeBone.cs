using System;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning.Serialize
{

    [Serializable]
    public class BoneHierarchy
    {
        public int[] parentIndices;
    }

    [Serializable]
    public class SerializeBone
    {
        public Matrix4x4 bindpose;
        public SerializeVertex[] vertices;
    }

    [Serializable]
    public class SerializeVertex
    {
        public int index;
        public float boneWeight;
    }
}