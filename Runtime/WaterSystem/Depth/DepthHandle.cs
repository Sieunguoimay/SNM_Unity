using System;

namespace Snm.WaterSystem.Depth
{
    public class DepthHandle : IDisposable
    {
        private readonly DepthFeature feature;

        public DepthHandle(DepthFeature feature)
        {
            this.feature = feature;
        }

        public void Dispose()
        {
            feature.Dispose();
        }
    }
}
