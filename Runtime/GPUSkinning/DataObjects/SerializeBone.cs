using System;

namespace Snm.Runtime.GPUSkinning.Serialize
{
    [Serializable]
    public class SerializeBone
    {
        public SerializeVertex[] vertices;
    }

    [Serializable]
    public class SerializeVertex
    {
        public int index;
        public float boneWeight;
    }
}