using System;

namespace Snm.WaterSystem.Buoyancy
{
    [Serializable]
    public class BuoyancyConfig
    {
        public bool enabled = true;

        /// <summary>Water density in kg/m³. Real water ≈ 1000.</summary>
        public float waterDensity = 2f;

        /// <summary>Rigidbody linear drag applied when fully submerged.</summary>
        public float linearDragInWater = .5f;

        /// <summary>Rigidbody angular drag applied when fully submerged.</summary>
        public float angularDragInWater = .5f;

        /// <summary>
        /// Opposes vertical velocity near the surface (submersion 0.2–0.8) to reduce oscillation.
        /// Force = -verticalVelocity * surfaceDampening * mass.
        /// </summary>
        public float surfaceDampening = 0.2f;
    }
}
