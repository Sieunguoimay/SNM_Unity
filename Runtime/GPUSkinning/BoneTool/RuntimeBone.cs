using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    public class RuntimeBone
    {
        public int parent;
        public Matrix4x4 bindpose;
        public List<RuntimeVertex> vertices;
    }

    public class RuntimeVertex
    {
        public int index;
        public float boneWeight;
    }
}