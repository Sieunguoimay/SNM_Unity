using System;
using UnityEngine;

namespace Snm.GrassSystem
{
    [Serializable]
    public class GrassSystemConfig
    {
        [Header("Grid")]
        public Vector2Int gridSize = new(50, 50);
        public Vector2 cellSpacing = new(0.5f, 0.5f);

        [Header("Rendering")]
        public Mesh grassMesh;
        public Material grassMaterial;

        [Header("Wind")]
        public Texture2D windMap;
        public Vector2 windMapScale = new(10, 10);
        public float windScrollSpeed = 0.01f;
        public float windStrength = 1f;

        [Header("Trample")]
        public bool trampleEnabled = true;
        public Shader trampleShader;
        public float trampleFadeSpeed = 0.1f;
        public float trampleHoldTime = 0.5f;
        public float disturbMinOffset = 0.01f;
        public int trampleResolution = 256;
    }
}
