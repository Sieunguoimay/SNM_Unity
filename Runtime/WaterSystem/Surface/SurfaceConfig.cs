using System;
using UnityEngine;

namespace Snm.WaterSystem.Surface
{
    [Serializable]
    public class SurfaceConfig
    {
        public Shader waterSurfaceShader;
        public Material waterSurfaceMaterial;
        public bool autoGenerateMesh = true;
        public Mesh mesh;
        public Vector2 size = new(50f, 50f);
    }
}
