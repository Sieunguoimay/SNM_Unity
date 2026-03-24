using Snm.SurfaceInteraction;

namespace Snm.GrassSystem
{
    public interface IGrassDisturber : ISurfaceDisturber
    {
        float GetContactRadius(float surfaceY);
    }
}
