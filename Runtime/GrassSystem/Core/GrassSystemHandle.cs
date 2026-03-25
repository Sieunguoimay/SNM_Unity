using System;
using Snm.SurfaceInteraction;

namespace Snm.GrassSystem
{
    public class GrassSystemHandle : IDisposable
    {
        private readonly IDisposable _scope;

        public GrassRenderer Renderer { get; }
        public GrassTrample Trample { get; }
        public SurfaceCanvas Canvas { get; }

        public GrassSystemHandle(
            IDisposable scope,
            GrassRenderer renderer,
            GrassTrample trample,
            SurfaceCanvas canvas)
        {
            _scope = scope;
            Renderer = renderer;
            Trample = trample;
            Canvas = canvas;
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
