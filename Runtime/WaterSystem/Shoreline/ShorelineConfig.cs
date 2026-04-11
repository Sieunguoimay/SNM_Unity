using System;
using UnityEngine;

namespace Snm.WaterSystem.Shoreline
{
    [Serializable]
    public class ShorelineConfig
    {
        public bool enabled;

        [Range(1, 5)]
        public int waveCount = 3;

        [Range(0f, 2f)]
        public float speed = 0.5f;

        [Range(0f, 3f)]
        public float foamStrength = 1f;

        [Range(0.1f, 5f)]
        public float foamScale = 1f;
    }
}
