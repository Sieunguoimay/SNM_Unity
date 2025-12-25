#if UNITY_EDITOR
#endif
using System;
using Snm.Runtime.Dispose;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace Snm.Runtime.GrassSystem
{

    public class InteractorTraceSystemInstaller
    {
        public IDisposable Install(
            float brushSize,
            out Action openWindowAction,
            out RenderTexture outRenderTexture,
            out Transform painterTransform,
            out WorldCanvas worldCanvas)
        {
            var renderTexture = outRenderTexture = CreateRenderTexture(512);
            var renderTexture2 = CreateRenderTexture(512);

            var worldMin = new Vector2(-10, -10);
            var worldMax = new Vector2(10, 10);
            worldCanvas = new WorldCanvas
            {
                worldMin = new Vector2(-10, -10),
                worldMax = new Vector2(10, 10)
            };

            var canvasSize = worldMax - worldMin;

            var painter = new InteractorTracePainter(
                renderTexture,
                renderTexture2,
                new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/SNM_Unity/Runtime/GrassSystem/WorldTraceBrush.shader")),
                brushSize / canvasSize.x, 1f,
                worldCanvas,
                paintCallback: () => { });

            painter.SetTexture();

            var renderTextureVE = CreateTexturePreviewVE(
                renderTexture, 
                clearClickCallback: painter.ClearOutRenderTextures, 
                out var disposeCanvasVE);

            var painterMB = new GameObject($"[{nameof(InteractorTracePainterMB)}]").AddComponent<InteractorTracePainterMB>();
            painterMB.gameObject.hideFlags = HideFlags.DontSave;
            painterMB.SetPainter(painter, paintCallback: renderTextureVE.MarkDirtyRepaint);
            painterMB.SetWorldCanvas(new WorldCanvasChecker(worldCanvas));

            var tracingAreaVisualizer = new WorldCanvasVisualizer(worldCanvas);

            AnyVEWindow window = null;
            openWindowAction = () => window = AnyVEWindow.Open(renderTextureVE);
            painterTransform = painterMB.transform;

            return new DisposeCallback(() =>
            {
                window?.Close();

                tracingAreaVisualizer.Cleanup();
                disposeCanvasVE.Dispose();

                if (painterMB) UnityEngine.Object.DestroyImmediate(painterMB.gameObject);

                DestroyRenderTexture(renderTexture2);
                DestroyRenderTexture(renderTexture);
            });
        }

        private VisualElement CreateTexturePreviewVE(
            RenderTexture renderTexture,
            Action clearClickCallback,
            out IDisposable disposable)
        {
            var root = new VisualElement();
            var canvasVE = new VisualElement
            {
                style = {
                    width = 200,
                    height = 200,
                    backgroundImage = Background.FromRenderTexture(renderTexture),
                }
            };
            var button_Clear = new Button() { text = "Clear", clickable = new(clearClickCallback) };

            root.Add(canvasVE);
            root.Add(button_Clear);

            disposable = new DisposeCallback(() =>
            {
                canvasVE.style.backgroundImage = null;
            });

            return root;
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