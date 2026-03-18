using System;

namespace Snm.WaterSystem.Wave
{
    [Serializable]
    public class WaveDisturberConfig
    {
        public bool enabled = true;

        /// <summary>Scales entry velocity into wave strength. entrySpeed * scale -> strength.</summary>
        public float entryStrengthScale = 1.0f;

        /// <summary>Maximum wave strength produced on water entry (clamped).</summary>
        public float maxEntryStrength = 1.0f;

        /// <summary>Maximum wake strength per frame while moving through water.</summary>
        public float wakeStrength = 1f;

        /// <summary>Scales movement speed into wake strength. Higher = stronger wakes at lower speeds.</summary>
        public float wakeMaxSpeed = 0.5f;

        /// <summary>Minimum speed (world units/s) required to produce a wake.</summary>
        public float wakeMinSpeed = 0.1f;

        /// <summary>How far above the water surface (world units) the disturber can still produce a wake.</summary>
        public float wakeProximityTolerance = 0.5f;

        /// <summary>Minimum disturbance radius in UV space for wake. Prevents invisible zero-radius stamps.</summary>
        public float wakeMinUVRadius = 0.015f;

        /// <summary>Minimum UV distance between consecutive wake disturbances. Smaller = denser trail.</summary>
        public float wakeUVStep = 0.001f;
    }
}
