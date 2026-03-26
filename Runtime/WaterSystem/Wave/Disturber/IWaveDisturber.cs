using Snm.SurfaceInteraction;

namespace Snm.WaterSystem.Wave
{
    public interface IWaveDisturber : ISurfaceDisturber
    {
        float MaxContactRadius { get; }
        float GetContactRadius(float surfaceY);
    }
}
