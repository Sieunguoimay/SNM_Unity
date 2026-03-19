using System;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    [Serializable]
    public class WaveConfig
    {
        public bool enabled = true;
        public Shader simulationShader;
        public Shader displayShader;
        public int textureSize = 512;

        [Header("Simulation")]
        [Tooltip("Energy retained per frame (0 = instant decay, 1 = no decay). Consistent regardless of iteration count.")]
        [Range(0.9f, 1f)]
        public float damping = 0.93f;

        [Tooltip("Wave equation coefficient. Higher = faster ripple expansion per iteration. Above 0.5 is unstable.")]
        [Range(0.01f, 0.5f)]
        public float waveSpeed = 0.2f;

        [Tooltip("Simulation steps per frame. More = waves travel further per frame, at higher GPU cost.")]
        [Range(1, 10)]
        public int iterationsPerFrame = 2;

        public float heightfieldStrength = 1f;

        [Range(0f, 10f)]
        public float waveNormalStrength = 1f;

        public int displayMode;

        [Header("Rain")]
        public RainConfig rain = new();

        [Header("Disturbers")]
        public WaveDisturberConfig disturber = new();
    }
}
