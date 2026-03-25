using System;

namespace Snm.GrassSystem
{
    public interface IGrassFeature : IDisposable
    {
        void OnUpdate(float deltaTime);
    }
}
