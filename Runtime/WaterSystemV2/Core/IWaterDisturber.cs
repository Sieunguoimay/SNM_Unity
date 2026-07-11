using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// An object that stirs the water surface (splash on entry, wake while
    /// moving). The water system defines the contract; gameplay implements it
    /// — either directly or via the drop-in <see cref="WaterDisturber"/>
    /// component. Register through <see cref="WaterInteraction"/>.
    /// </summary>
    public interface IWaterDisturber
    {
        /// <summary>Bottom of the object in world space (lowest point, at the object's XZ center).</summary>
        Vector3 WorldPosition { get; }

        Vector3 WorldVelocity { get; }

        /// <summary>True while the object's vertical span crosses the surface plane.</summary>
        bool IsTouchingSurface(float surfaceY);

        /// <summary>
        /// World-space radius of the circle where the object intersects the
        /// plane at <paramref name="surfaceY"/>. 0 when not intersecting
        /// (fully above or fully below — submerged objects leave no wake).
        /// </summary>
        float GetContactRadius(float surfaceY);
    }
}
