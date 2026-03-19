using UnityEngine;

namespace Snm.WaterSystem.Buoyancy
{
    /// <summary>
    /// What the water system needs from an object that should float.
    /// Mirrors <see cref="Wave.IWaveDisturber"/> — the water system defines the contract,
    /// the game layer implements it.
    /// </summary>
    public interface IBuoyant
    {
        Rigidbody Rigidbody { get; }
        Collider Collider { get; }
        Vector3 WorldPosition { get; }

        /// <summary>
        /// Returns the volume (m³) of the collider currently below <paramref name="waterY"/>.
        /// Implementors compute this analytically per collider type.
        /// </summary>
        float GetSubmergedVolume(float waterY);

        /// <summary>Total collider volume (m³).</summary>
        float GetTotalVolume();
    }
}
