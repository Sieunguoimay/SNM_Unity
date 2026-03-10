using System;

namespace Snm.WaterSystem.Caustics
{
    public class CausticsHandle : IDisposable
    {
        private readonly CausticsFeature feature;

        public CausticsHandle(CausticsFeature feature)
        {
            this.feature = feature;
        }

        public void Dispose()
        {
            feature.Dispose();
        }
    }
}
