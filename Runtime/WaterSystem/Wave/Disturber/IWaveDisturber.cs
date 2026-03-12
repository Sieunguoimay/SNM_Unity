using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    /// <summary>
    /// Implemented by any object that can create wave disturbances
    /// when it enters or moves through the water surface.
    /// </summary>
    public interface IWaveDisturber
    {
        Vector3 WorldPosition { get; }
        Vector3 WorldVelocity { get; }
        float   Radius        { get; }
    }
}
