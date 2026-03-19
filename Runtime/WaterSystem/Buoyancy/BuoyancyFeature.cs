namespace Snm.WaterSystem.Buoyancy
{
    /// <summary>
    /// <see cref="IWaterFeature"/> wrapper that drives <see cref="BuoyancyTracker"/> each FixedUpdate.
    /// </summary>
    public class BuoyancyFeature : IWaterFeature
    {
        private readonly BuoyancyTracker _tracker;

        public BuoyancyFeature(BuoyancyTracker tracker) => _tracker = tracker;

        public void OnUpdate(float deltaTime) { }
        public void OnFixedUpdate(float fixedDeltaTime) => _tracker.Update(fixedDeltaTime);
        public void Dispose() { }
    }
}
