using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// An object the water pushes up (Archimedes). The water system defines
    /// the contract; gameplay implements it — either directly or via the
    /// drop-in <see cref="WaterFloater"/> component. Register through
    /// <see cref="WaterInteraction"/>.
    /// </summary>
    public interface IWaterFloater
    {
        Rigidbody Rigidbody { get; }

        /// <summary>Volume center in world space (used for the inside-water test).</summary>
        Vector3 WorldPosition { get; }

        /// <summary>Volume (m³) currently below <paramref name="waterY"/>.</summary>
        float GetSubmergedVolume(float waterY);

        /// <summary>Total volume (m³).</summary>
        float GetTotalVolume();
    }
}
