using System;
using UnityEngine;

namespace Snm.WaterSystem.Rain
{
    [Serializable]
    public class RainConfig
    {
        public bool enabled;

        [Tooltip("Ripple normal map or atlas texture.")]
        public Texture2D rippleTexture;

        [Range(0f, 3f)]
        public float intensity = 1f;

        [Range(0.5f, 10f)]
        public float density = 3f;

        [Range(0.1f, 5f)]
        public float speed = 1f;

        [Range(0.01f, 2f)]
        public float scale = 0.5f;
    }
}
