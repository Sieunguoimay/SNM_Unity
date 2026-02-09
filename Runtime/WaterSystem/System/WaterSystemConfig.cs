using System;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    [Serializable]
    public class WaterSystemConfig
    {
        public Shader waterSurfaceShader;
        public Material waterSurfaceMaterial;
        public bool autoGenerateMesh;
        public Mesh mesh;
        public Vector2 waterSurfaceSize = new(10f, 10f);
        public int reflectionTextureWidth = 256;
    }
}