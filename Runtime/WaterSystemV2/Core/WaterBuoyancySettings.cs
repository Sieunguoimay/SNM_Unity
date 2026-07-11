using System;
using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>Archimedes buoyancy tuning. Defaults match WaterSystem V1.</summary>
    [Serializable]
    public class WaterBuoyancySettings
    {
        public bool enabled = true;

        [Tooltip("Water density in kg/m³ against the colliders' implied density. Gameplay value — real water would be ~1000.")]
        public float waterDensity = 2f;

        [Tooltip("Rigidbody linear damping when fully submerged.")]
        public float linearDragInWater = 3f;

        [Tooltip("Rigidbody angular damping when fully submerged.")]
        public float angularDragInWater = 0.5f;

        [Tooltip("Opposes vertical velocity near the surface (submersion 20–80%) to reduce bobbing.")]
        public float surfaceDampening = 0.2f;
    }
}
