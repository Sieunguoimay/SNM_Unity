using System;
using UnityEngine;

namespace Snm.WaterSystem.Rain
{
    [Serializable]
    public class RainConfig
    {
        public bool enabled = true;

        [Tooltip("Strength of each raindrop disturbance.")]
        [Range(0f, 3f)]
        public float intensity = 0.5f;

        [Tooltip("Number of raindrops per second.")]
        [Range(1f, 30f)]
        public float density = 10f;

        [Tooltip("Radius of each raindrop ripple in UV space.")]
        [Range(0.01f, 0.1f)]
        public float dropRadius = 0.03f;
    }
}
