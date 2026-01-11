using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleRenderer
    {
        private readonly RenderTexture renderTexture;
        private readonly RenderTexture renderTexture2;
        private readonly Material material;
        private bool useAasSource;

        public GrassTrampleRenderer(Material material, RenderTexture renderTexture)
        {
            this.material = material;

            renderTexture2 = new RenderTexture(renderTexture.descriptor);
            renderTexture2.Create();
        }

        public void Cleanup()
        {
            renderTexture2.Release();
        }

        public void Render(float deltaTime)
        {
            if (renderTexture == null) return;

            material.SetFloat("_DeltaTime", deltaTime);

            var rtA = renderTexture;
            var rtB = renderTexture2;

            var src = useAasSource ? rtA : rtB;
            var dst = useAasSource ? rtB : rtA;

            Graphics.Blit(src, dst, material);

            useAasSource = !useAasSource;
        }
    }
}