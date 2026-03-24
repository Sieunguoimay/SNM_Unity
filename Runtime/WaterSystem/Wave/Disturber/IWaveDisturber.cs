using Snm.SurfaceInteraction;

namespace Snm.WaterSystem.Wave
{
    public interface IWaveDisturber : ISurfaceDisturber
    {
        float GetContactRadius(float surfaceY);
    }
}
