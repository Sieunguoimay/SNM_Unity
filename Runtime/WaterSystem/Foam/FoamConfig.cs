using System;
using UnityEngine;

namespace Snm.WaterSystem.Foam
{
    [Serializable]
    public class FoamConfig
    {
        public bool enabled = true;
        public Texture2D foamTexture;

        [Range(0f, 3f)]
        public float strength = 1f;

        [Range(0f, 5f)]
        public float depthThreshold = .5f;

        [Range(0.01f, 2f)]
        public float scale = 0.5f;

        [Range(0f, 0.5f)]
        public float speed = 0.05f;
    }
}
