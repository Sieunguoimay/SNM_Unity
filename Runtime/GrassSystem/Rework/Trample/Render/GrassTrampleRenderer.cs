using System.Collections.Generic;
using Snm.SurfaceInteraction;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleRenderer
    {
        private readonly SurfaceStampRenderer _renderer;
        private readonly StampBuffer _stampBuffer;
        private readonly float _fadeSpeed;

        private static readonly int ID_FadeAmount = Shader.PropertyToID("_FadeAmount");
        private static readonly int ID_Brushes = Shader.PropertyToID("_Brushes");
        private static readonly int ID_BrushCount = Shader.PropertyToID("_BrushCount");
        private static readonly int ID_WorldCanvas = Shader.PropertyToID("_WorldCanvas");

        public GrassTrampleRenderer(
            SurfaceStampRenderer renderer,
            StampBuffer stampBuffer,
            SurfaceCanvas canvas,
            float fadeSpeed)
        {
            _renderer = renderer;
            _stampBuffer = stampBuffer;
            _fadeSpeed = fadeSpeed;

            var min = canvas.WorldMin;
            var max = canvas.WorldMax;
            var size = max - min;
            _renderer.Material.SetVector(ID_WorldCanvas, new Vector4(min.x, min.y, size.x, size.y));
        }

        public void FillStamps(IReadOnlyList<GrassTrampleBrush> brushes)
        {
            for (int i = 0; i < brushes.Count; i++)
            {
                var brush = brushes[i];
                if (!brush.isActive) continue;

                float angle = Mathf.Atan2(brush.dir.z, brush.dir.x);
                _stampBuffer.Add(new Vector4(brush.position.x, brush.position.z, angle, brush.radius));
            }
        }

        public void Render(float deltaTime)
        {
            var mat = _renderer.Material;
            mat.SetFloat(ID_FadeAmount, deltaTime * _fadeSpeed);
            _stampBuffer.Upload(mat, ID_Brushes, ID_BrushCount);
            _renderer.Render();
        }

        public void Cleanup()
        {
            _renderer.Dispose();
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
            return rt;
        }

        public static void DestroyRenderTexture(RenderTexture renderTexture)
        {
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }
}
