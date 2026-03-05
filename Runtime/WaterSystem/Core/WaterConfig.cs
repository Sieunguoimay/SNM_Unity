using System;
using UnityEngine;

namespace Snm.WaterSystem
{
    [Serializable]
    public class WaterConfig
    {
        public Shader waterSurfaceShader;
        public Material waterSurfaceMaterial;
        public bool autoGenerateMesh;
        public Mesh mesh;
        public Vector2 waterSurfaceSize = new(10f, 10f);
        public int reflectionTextureWidth = 256;
    }
}