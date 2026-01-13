using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleRenderer
    {
        private readonly RenderTexture renderTexture;
        private readonly WorldCanvas worldCanvas;
        private readonly float fadeSpeed;
        private readonly RenderTexture renderTexture2;
        private readonly Material material;
        private bool useAasSource;

        public GrassTrampleRenderer(
            Material material,
            RenderTexture renderTexture,
            WorldCanvas worldCanvas, 
            float fadeSpeed)
        {
            this.material = material;
            this.renderTexture = renderTexture;
            this.worldCanvas = worldCanvas;
            this.fadeSpeed = fadeSpeed;
            renderTexture2 = new RenderTexture(renderTexture.descriptor);
            renderTexture2.Create();

            SetTexture();
        }

        public void Cleanup()
        {
            renderTexture2.Release();
        }

        private void SetTexture()
        {
            var origin = worldCanvas.worldMin;
            var size = worldCanvas.worldMax - worldCanvas.worldMin;
            material.SetTexture("_MainTex", renderTexture);
            material.SetVector("_WorldCanvas", new Vector4(origin.x, origin.y, size.x, size.y));
        }

        public void Render(float deltaTime)
        {
            if (renderTexture == null) return;

            material.SetFloat("_Fade", deltaTime * fadeSpeed);

            var rtA = renderTexture;
            var rtB = renderTexture2;

            var src = useAasSource ? rtA : rtB;
            var dst = useAasSource ? rtB : rtA;

            Graphics.Blit(src, dst, material);

            useAasSource = !useAasSource;
        }
    }
}