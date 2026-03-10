using System;
using UnityEngine;

namespace Snm.WaterSystem.Surface
{
    [Serializable]
    public class SurfaceConfig
    {
        public Shader waterSurfaceShader;
        public Material waterSurfaceMaterial;
        public bool autoGenerateMesh;
        public Mesh mesh;
        public Vector2 size = new(10f, 10f);
    }
}
