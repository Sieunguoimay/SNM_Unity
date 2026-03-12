using System;

namespace Snm.WaterSystem
{
    /// <summary>
    /// Unified lifecycle interface for all water features.
    /// Managed by the WaterSystem composite — features do NOT register
    /// themselves with UpdateDispatcher.
    /// </summary>
    public interface IWaterFeature : IDisposable
    {
        void OnUpdate(float deltaTime);
        void OnLateUpdate() { }
    }
}
