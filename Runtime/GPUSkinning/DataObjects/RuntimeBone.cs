using System.Collections.Generic;

namespace Snm.Runtime.GPUSkinning
{
    public class RuntimeBone
    {
        public List<RuntimeVertex> vertices;
    }

    public class RuntimeVertex
    {
        public int index;
        public float boneWeight;
    }
}