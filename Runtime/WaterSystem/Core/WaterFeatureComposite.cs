using System.Collections.Generic;

namespace Snm.WaterSystem
{
    /// <summary>
    /// Single IUpdateTarget + ILateUpdateTarget that drives all IWaterFeature instances.
    /// Registered once with UpdateDispatcher — features never self-register.
    /// </summary>
    public sealed class WaterFeatureComposite : IUpdateTarget, ILateUpdateTarget
    {
        private readonly List<IWaterFeature> _features = new();

        public void Add(IWaterFeature feature) => _features.Add(feature);

        public void Update(float deltaTime)
        {
            foreach (var f in _features)
                f.OnUpdate(deltaTime);
        }

        public void LateUpdate()
        {
            foreach (var f in _features)
                f.OnLateUpdate();
        }
    }
}
