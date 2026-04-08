using UnityEngine;

namespace Snm.CameraRig
{
    public class CameraTarget
    {
        public Bounds VisibleBounds { get; set; }
        public Vector3? DesiredCamDirection { get; }
        public Vector3 Velocity { get; set; }

        /// <summary>
        /// When set, the camera uses this instead of Velocity for look-ahead.
        /// Used by aiming to pan the camera in the aim direction.
        /// </summary>
        public Vector3? VelocityOverride { get; set; }

        public Vector3 EffectiveVelocity => VelocityOverride ?? Velocity;

        public CameraTarget(
            Bounds visibleBounds,
            Vector3? desiredCamDirection)
        {
            DesiredCamDirection = desiredCamDirection;
            VisibleBounds = visibleBounds;
        }
    }
}
