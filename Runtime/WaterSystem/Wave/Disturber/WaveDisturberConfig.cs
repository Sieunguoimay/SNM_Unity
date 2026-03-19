using System;
using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    [Serializable]
    public class WaveDisturberConfig
    {
        public bool enabled = true;

        [Header("Entry Splash")]
        [Range(0f, 5f)]
        [Tooltip("Multiplier applied to entry speed to compute splash strength.")]
        public float entryStrengthScale = 1.0f;

        [Range(0f, 2f)]
        [Tooltip("Maximum splash strength regardless of speed.")]
        public float entryMaxStrength = 1.0f;

        [Header("Wake")]
        [Range(0f, 2f)]
        [Tooltip("Maximum wake strength at full speed.")]
        public float wakeStrength = 1f;

        [Tooltip("Speed-to-strength curve. X = normalized speed (0..1), Y = strength multiplier (0..1).")]
        public AnimationCurve wakeSpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Range(0.01f, 1f)]
        [Tooltip("Spacing between wake stamps (world units) at full speed. Smaller = denser trail.")]
        public float wakeSpacingAtFullSpeed = 0.05f;

        [Range(0.01f, 1f)]
        [Tooltip("Spacing between wake stamps (world units) at minimum speed. Larger = sparser trail.")]
        public float wakeSpacingAtMinSpeed = 0.3f;

        [Range(0f, 1f)]
        [Tooltip("Minimum wake radius in world units. Ensures wake is always visible.")]
        public float wakeMinRadiusWorld = 0.2f;

        [Header("Speed Range")]
        [Range(0f, 5f)]
        [Tooltip("Below this speed, no wake is produced.")]
        public float wakeMinSpeed = 0.1f;

        [Range(0f, 10f)]
        [Tooltip("At or above this speed, wake is at full strength and densest spacing.")]
        public float wakeMaxSpeed = 0.5f;

        [Header("Detection")]
        [Range(0f, 2f)]
        [Tooltip("How far above the surface (world units) a disturber can still produce wake.")]
        public float proximityTolerance = 0.5f;
    }
}
