using UnityEngine;

namespace Snm.Runtime.WaterSystem
{

    public class WaterSurface
    {
        public Quaternion rotation;
        public Vector3 position;
        public Vector2 size;

        //this should be moved to the reflection settings
        public float reflectionDepth = 15f; // meters above water

        public Mesh mesh;
        public Texture reflectionMap;
    }
}