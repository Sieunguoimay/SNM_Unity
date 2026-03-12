using System;
using UnityEngine;

namespace Snm.WaterSystem.Caustics
{
    [Serializable]
    public class CausticsConfig
    {
        public bool enabled = true;
        public Texture2D causticsTexture;

        [Range(0f, 5f)]
        public float strength = 1f;

        [Range(0.01f, 1f)]
        public float scale = 0.1f;

        [Range(0f, 0.5f)]
        public float speed = 0.05f;

        [Range(0f, 0.01f)]
        public float split = 0.003f;

        [Tooltip("Enable chromatic aberration split (6 samples). Disable for 2 samples on mobile.")]
        public bool chromaticSplit = true;
    }
}
