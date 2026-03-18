using UnityEngine;

namespace Snm.WaterSystem.Wave
{
    public interface IWaveDisturber
    {
        Vector3 WorldPosition { get; }
        Vector3 WorldVelocity { get; }
        bool    IsTouchingWater(float waterY);
        float   GetContactRadius(float waterY);
    }
}
