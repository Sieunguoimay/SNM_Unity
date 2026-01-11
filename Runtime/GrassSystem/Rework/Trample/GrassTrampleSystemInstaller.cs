using System;
using Snm.Runtime.Dispose;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTrampleBrushManager
    {
        
    }

    public class GrassTrampleSystemInstaller
    {
        public GrassTrampleSystemHandle Install()
        {
            var renderTexture = CreateRenderTexture(512);
            var material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/SNM_Unity/Runtime/GrassSystem/WorldTraceBrush.shader"));
            var renderer = new GrassTrampleRenderer(material, renderTexture);
            var rendererMB = new GameObject("[GrassTrampleRendererMB]").AddComponent<GrassTrampleRendererMB>();
            var painter = new GrassTramplePainter(material);

            rendererMB.SetRenderer(renderer);

            return new GrassTrampleSystemHandle(
                renderTexture,
                cleanupCallback: () =>
                {
                    UnityEngine.Object.DestroyImmediate(rendererMB.gameObject);
                    UnityEngine.Object.DestroyImmediate(material);
                    renderer.Cleanup();
                    DestroyRenderTexture(renderTexture);
                });
        }

        public RenderTexture CreateRenderTexture(int size)
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