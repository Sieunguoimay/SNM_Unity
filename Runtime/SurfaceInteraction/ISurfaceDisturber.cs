using UnityEngine;

namespace Snm.SurfaceInteraction
{
    public interface ISurfaceDisturber
    {
        Vector3 WorldPosition { get; }
        Vector3 WorldVelocity { get; }
        bool IsTouchingSurface(float surfaceY);
    }
}
