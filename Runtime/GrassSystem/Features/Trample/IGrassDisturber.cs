using Snm.SurfaceInteraction;

namespace Snm.GrassSystem
{
    public interface IGrassDisturber : ISurfaceDisturber
    {
        float MaxContactRadius { get; }
        float GetContactRadius(float surfaceY);

        /// <summary>
        /// When true, this disturber's position is sent directly to the grass rendering shader
        /// for smooth per-frame trample direction. Use for important objects like the player.
        /// </summary>
        bool SmoothTrample => false;
    }
}
