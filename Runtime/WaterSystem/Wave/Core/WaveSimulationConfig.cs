using System;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    [Serializable]
    public class WaveSimulationConfig
    {
        [Range(0.9f, 1f)]
        public float damping = 0.99f;

        [Range(0.01f, 0.5f)]
        public float waveSpeed = 0.45f;

        [Range(0.1f, 10f)]
        public float waveSpreadSpeed = 1f;

        public float heightfieldStrength = 1f;

        public int displayMode; // 0 = height, 1 = normal, 2 = heightfield
    }
}
