namespace Snm.GrassSystem
{
    public class RenderFeature : IGrassFeature
    {
        private readonly GrassRenderer _renderer;

        public RenderFeature(GrassRenderer renderer)
        {
            _renderer = renderer;
        }

        public void OnUpdate(float deltaTime)
        {
            _renderer.Render();
        }

        public void Dispose() { }
    }
}
