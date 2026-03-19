using System;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    [Serializable]
    public class RainConfig
    {
        public bool enabled = true;

        [Tooltip("Strength of each raindrop disturbance.")]
        [Range(0f, 10f)]
        public float intensity = 3f;

        [Tooltip("Average number of raindrops per second across the surface.")]
        [Range(1f, 500f)]
        public float density = 30f;
    }
}
