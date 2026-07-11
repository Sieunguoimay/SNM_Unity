using System;
using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Tuning for the GPU wave heightfield simulation and everything that
    /// feeds it (rain, disturber splashes/wakes). Read live every frame by
    /// <see cref="WaveSimulation"/> — tweak in play mode freely; only
    /// enabled/textureSize/stampCapacity changes trigger a rebuild.
    /// </summary>
    [Serializable]
    public class WaterWaveSettings
    {
        public bool enabled = true;

        [Tooltip("Resolution of the simulation texture. Structural — changing it rebuilds the sim.")]
        public int textureSize = 512;

        [Tooltip("Max disturbances per frame uploaded to the GPU. Structural — changing it rebuilds the sim.")]
        [Range(8, 256)]
        public int stampCapacity = 64;

        [Header("Simulation")]
        [Tooltip("Energy retained per frame (0.9 = fast decay, 1 = no decay). Consistent regardless of iteration count.")]
        [Range(0.9f, 1f)]
        public float damping = 0.93f;

        [Tooltip("Wave equation coefficient. Higher = faster ripple expansion per iteration. Above 0.5 is unstable.")]
        [Range(0.01f, 0.5f)]
        public float waveSpeed = 0.2f;

        [Tooltip("Simulation steps per frame. More = waves travel further per frame, at higher GPU cost.")]
        [Range(1, 10)]
        public int iterationsPerFrame = 2;

        [Tooltip("How strongly the heightfield perturbs surface normals.")]
        [Range(0f, 10f)]
        public float normalStrength = 1f;

        [Header("Rain")]
        public RainSettings rain = new();

        [Header("Disturbers")]
        public DisturberSettings disturber = new();

        [Serializable]
        public class RainSettings
        {
            public bool enabled;

            [Tooltip("Strength of each raindrop disturbance.")]
            [Range(0f, 10f)]
            public float intensity = 3f;

            [Tooltip("Average raindrops per second across the whole surface.")]
            [Range(1f, 500f)]
            public float density = 30f;
        }

        [Serializable]
        public class DisturberSettings
        {
            public bool enabled = true;

            [Header("Entry Splash")]
            [Tooltip("Multiplier applied to entry speed to compute splash strength.")]
            [Range(0f, 5f)]
            public float entryStrengthScale = 1f;

            [Tooltip("Maximum splash strength regardless of speed.")]
            [Range(0f, 2f)]
            public float entryMaxStrength = 1f;

            [Header("Wake")]
            [Tooltip("Maximum wake strength at full speed.")]
            [Range(0f, 2f)]
            public float wakeStrength = 1f;

            [Tooltip("Speed-to-strength curve. X = normalized speed (0..1), Y = strength multiplier (0..1).")]
            public AnimationCurve wakeSpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            [Tooltip("Spacing between wake stamps (world units) at full speed. Smaller = denser trail.")]
            [Range(0.01f, 1f)]
            public float wakeSpacingAtFullSpeed = 0.05f;

            [Tooltip("Spacing between wake stamps (world units) at minimum speed. Larger = sparser trail.")]
            [Range(0.01f, 1f)]
            public float wakeSpacingAtMinSpeed = 0.3f;

            [Tooltip("Minimum wake radius in world units. Ensures the wake is always visible.")]
            [Range(0f, 1f)]
            public float wakeMinRadiusWorld = 0.2f;

            [Header("Speed Range")]
            [Tooltip("Below this speed, no wake is produced.")]
            [Range(0f, 5f)]
            public float wakeMinSpeed = 0.1f;

            [Tooltip("At or above this speed, wake is at full strength and densest spacing.")]
            [Range(0f, 10f)]
            public float wakeMaxSpeed = 0.5f;

            [Header("Detection")]
            [Tooltip("How far above the surface (world units) a disturber can still produce wake.")]
            [Range(0f, 2f)]
            public float proximityTolerance = 0.5f;
        }
    }
}
