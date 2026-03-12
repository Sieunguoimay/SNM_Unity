namespace Snm.WaterSystem.Wave
{
    /// <summary>
    /// IWaterFeature wrapper that drives <see cref="WaveDisturberTracker"/> each frame.
    /// Added to the WaterFeatureComposite by <see cref="WaveDisturberInstaller"/>.
    /// </summary>
    public class WaveDisturberFeature : IWaterFeature
    {
        private readonly WaveDisturberTracker _tracker;

        public WaveDisturberFeature(WaveDisturberTracker tracker) => _tracker = tracker;

        public void OnUpdate(float deltaTime) => _tracker.Update(deltaTime);
        public void Dispose() { }
    }
}
