using System;
using Snm.Runtime.Unity;
using Snm.SurfaceInteraction;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleRenderer : IDisposable
    {
        private readonly SurfaceStampRenderer _renderer;
        private readonly float _fadeSpeed;

        private static readonly int ID_FadeAmount = Shader.PropertyToID("_FadeAmount");
        private static readonly int ID_Brushes = Shader.PropertyToID("_Brushes");
        private static readonly int ID_BrushCount = Shader.PropertyToID("_BrushCount");
        private static readonly int ID_WorldCanvas = Shader.PropertyToID("_WorldCanvas");

        public StampBuffer StampBuffer { get; }

        public GrassTrampleRenderer(
            SurfaceStampRenderer renderer,
            StampBuffer stampBuffer,
            SurfaceCanvas canvas,
            float fadeSpeed)
        {
            _renderer = renderer;
            StampBuffer = stampBuffer;
            _fadeSpeed = fadeSpeed;

            var min = canvas.WorldMin;
            var max = canvas.WorldMax;
            var size = max - min;
            _renderer.Material.SetVector(ID_WorldCanvas, new Vector4(min.x, min.y, size.x, size.y));
        }

        public void Render(float deltaTime)
        {
            var mat = _renderer.Material;
            mat.SetFloat(ID_FadeAmount, deltaTime * _fadeSpeed);
            StampBuffer.Upload(mat, ID_Brushes, ID_BrushCount);
            _renderer.Render();
        }

        public void Dispose()
        {
            _renderer.Dispose();
        }

        // public static RenderTexture CreateRenderTexture(int size)
        // {
        //     var desc = new RenderTextureDescriptor(size, size)
        //     {
        //         graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat,
        //         depthBufferBits = 0,
        //         msaaSamples = 1,
        //         sRGB = false,
        //         enableRandomWrite = false,
        //     };

        //     var rt = new RenderTexture(desc)
        //     {
        //         filterMode = FilterMode.Bilinear,
        //         wrapMode = TextureWrapMode.Clamp,
        //         useMipMap = false,
        //         autoGenerateMips = false
        //     };
        //     rt.Create();
        //     return rt;
        // }

        // public static void DestroyRenderTexture(RenderTexture renderTexture)
        // {
        //     renderTexture.Release();
        //     UnityEngineUtility.DestroyObject(renderTexture);
        // }
    }
}
