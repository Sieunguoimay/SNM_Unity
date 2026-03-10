using Snm.Runtime.Unity;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleRenderer
    {
        private readonly RenderTexture renderTexture;
        private readonly WorldCanvas worldCanvas;
        private readonly float fadeSpeed;
        private readonly RenderTexture renderTexture2;
        private readonly Material material;

        private bool _useAsSource;
        private BrushRenderBatch[] _brushBatches;

        public GrassTrampleRenderer(
            Shader brushShader,
            RenderTexture renderTexture,
            WorldCanvas worldCanvas,
            float fadeSpeed)
        {
            var material = new Material(brushShader);

            this.material = material;
            this.renderTexture = renderTexture;
            this.worldCanvas = worldCanvas;
            this.fadeSpeed = fadeSpeed;

            renderTexture2 = new RenderTexture(renderTexture.descriptor);
            renderTexture2.Create();

            UploadTexture();
        }

        public void Cleanup()
        {
            UnityEngineUtility.DestroyObject(material);
            renderTexture2.Release();
        }

        private void UploadTexture()
        {
            var origin = worldCanvas.worldMin;
            var size = worldCanvas.worldMax - worldCanvas.worldMin;
            material.SetTexture("_MainTex", renderTexture);
            material.SetVector("_WorldCanvas", new Vector4(origin.x, origin.y, size.x, size.y));
        }

        public void SetBrushBatches(BrushRenderBatch[] brushBatches)
        {
            _brushBatches = brushBatches;
        }

        public void Render(float deltaTime)
        {
            if (_brushBatches == null) return;

            var batchCount = _brushBatches.Length;

            for (int i = 0; i < batchCount; i++)
            {
                if (i == batchCount - 1)
                {
                    material.SetFloat("_FadeAmount", deltaTime * fadeSpeed);
                }
                else
                {
                    material.SetFloat("_FadeAmount", 0f);
                }
                UploadBrushes(material, _brushBatches[i]);

                var rtA = renderTexture;
                var rtB = renderTexture2;

                var src = _useAsSource ? rtA : rtB;
                var dst = _useAsSource ? rtB : rtA;

                Graphics.Blit(src, dst, material);

                _useAsSource = !_useAsSource;
            }
        }

        private static void UploadBrushes(Material material, BrushRenderBatch brushBatch)
        {
            material.SetInt("_BrushCount", brushBatch.brushCount);
            material.SetVectorArray("_Brush_PosDir", brushBatch.brushes_PosDir);
            material.SetFloatArray("_Brush_Radius", brushBatch.brushes_Radius);
            brushBatch.brushCount = 0;
        }

        public static RenderTexture CreateRenderTexture(int size)
        {
            var desc = new RenderTextureDescriptor(size, size)
            {
                graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = false,
                enableRandomWrite = false,
            };

            var rt = RenderTexture.GetTemporary(desc);
            // rt.filterMode = FilterMode.Point;
            // rt.wrapMode = TextureWrapMode.Clamp;
            // rt.useMipMap = false;
            // rt.autoGenerateMips = false;
            return rt;
        }

        public static void DestroyRenderTexture(RenderTexture renderTexture)
        {
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }
}