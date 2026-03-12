using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionRenderScheduler
    {
        private readonly int _frameInterval;

        public ReflectionRenderScheduler(int frameInterval)
        {
            _frameInterval = frameInterval;
        }

        public bool ShouldRender(bool dirty)
        {
            if (dirty) return true;
            if (_frameInterval <= 1) return true;
            return Time.frameCount % _frameInterval == 0;
        }
    }
}
