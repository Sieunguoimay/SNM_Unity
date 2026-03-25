using System;
using System.Collections.Generic;
using Snm.WaterSystem;

namespace Snm.GrassSystem
{
    public sealed class GrassFeatureComposite : IUpdateTarget, IDisposable
    {
        private readonly List<IGrassFeature> _features = new();

        public void Add(IGrassFeature feature) => _features.Add(feature);

        public void Update(float deltaTime)
        {
            foreach (var f in _features)
                f.OnUpdate(deltaTime);
        }

        public void Dispose()
        {
            for (int i = _features.Count - 1; i >= 0; i--)
                _features[i].Dispose();

            _features.Clear();
        }
    }
}
