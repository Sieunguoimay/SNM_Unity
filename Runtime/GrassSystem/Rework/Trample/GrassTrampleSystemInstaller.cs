using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.Runtime.GrassSystem
{

    public class GrassTrampleBrush
    {
        public Vector3 position;
        public float radius;
        public float strength;
    }

    public class GrassTrampleBrushRegistry
    {
        private readonly List<GrassTrampleBrush> brushes = new();

        public void Register(GrassTrampleBrush brush) { brushes.Add(brush); }
        public void Unregister(GrassTrampleBrush brush) { brushes.Remove(brush); }
        public IReadOnlyList<GrassTrampleBrush> GetBrushes() => brushes;
    }

    [Serializable]
    public class GrassTrampleSystemConfig
    {
        public Shader shader;
        public float brushMinOffset = 0.01f;
        public float fadeSpeed = 0.1f;
    }

    public class GrassTrampleSystemInstaller
    {
        public GrassTrampleSystemHandle Install(
            GrassTrampleSystemConfig config, 
            int textureSize, 
            WorldCanvas worldCanvas)
        {
            var renderTexture = CreateRenderTexture(textureSize);
            var material = new Material(config.shader);
            var renderer = new GrassTrampleRenderer(material, renderTexture, worldCanvas, config.fadeSpeed);
            var painter = new GrassTramplePainter(material);
            var brushRegistry = new GrassTrampleBrushRegistry();
            var brushDriver = new GrassTrampleBrushDriver(brushRegistry, painter, minOffset: config.brushMinOffset, worldCanvas);

            var rendererMB = new GameObject("[GrassTrampleRendererMB]").AddComponent<GrassTrampleRendererMB>();
            var updaterMB = new GameObject("[GrassTrampleUpdaterMB]").AddComponent<GrassTrampleUpdaterMB>();

            updaterMB.SetDriver(brushDriver);
            rendererMB.SetRenderer(renderer);

            return new GrassTrampleSystemHandle(
                renderTexture,
                cleanupCallback: () =>
                {
                    UnityEngine.Object.DestroyImmediate(updaterMB.gameObject);
                    UnityEngine.Object.DestroyImmediate(rendererMB.gameObject);
                    UnityEngine.Object.DestroyImmediate(material);
                    renderer.Cleanup();
                    DestroyRenderTexture(renderTexture);
                }, brushRegistry);
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