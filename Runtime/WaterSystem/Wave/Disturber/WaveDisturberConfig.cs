using System;

namespace Snm.WaterSystem.Wave
{
    [Serializable]
    public class WaveDisturberConfig
    {
        public bool  enabled            = true;

        /// <summary>Scales entry velocity into wave strength. entrySpeed * scale → strength.</summary>
        public float entryStrengthScale = 1.0f;

        /// <summary>Maximum wave strength produced on water entry (clamped).</summary>
        public float maxEntryStrength   = 1.0f;

        /// <summary>Strength of the continuous wake disturbance while moving through water.</summary>
        public float wakeStrength       = 0.08f;

        /// <summary>Seconds between wake disturbance pulses.</summary>
        public float wakeInterval       = 0.05f;

        /// <summary>Minimum speed (world units/s) required to produce a wake.</summary>
        public float wakeMinSpeed       = 0.3f;
    }
}
