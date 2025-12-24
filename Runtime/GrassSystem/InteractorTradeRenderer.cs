#if UNITY_EDITOR
#endif
using System;
using Codice.Client.Common.GameUI;
using Snm.Runtime.Dispose;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace Snm.Runtime.GrassSystem
{
    public class InteractorTracePainter
    {
        private readonly RenderTexture renderTexture;
        private readonly Material material;
        private readonly float brushRadius;
        private readonly float brushStrength;
        private readonly WorldCanvas worldCanvas;
        private readonly Action paintCallback;

        public InteractorTracePainter(
            RenderTexture renderTexture,
            Material material,
            float brushRadius,
            float brushStrength,
            WorldCanvas worldCanvas,
            Action paintCallback)
        {
            this.renderTexture = renderTexture;
            this.material = material;
            this.brushRadius = brushRadius;
            this.brushStrength = brushStrength;
            this.worldCanvas = worldCanvas;
            this.paintCallback = paintCallback;
        }

        public void Paint(Vector3 worldPos)
        {
            if (renderTexture == null) return;

            var uv = WorldToUV(worldPos);
            material.SetVector("_BrushParams", new Vector4(uv.x, uv.y, brushRadius, brushStrength));

            var old = RenderTexture.active;
            RenderTexture.active = renderTexture;

            //Rendering goes here..

            GL.PushMatrix();
            GL.LoadOrtho();
            material.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, 6);
            GL.PopMatrix();

            RenderTexture.active = old;

            paintCallback();
        }

        public void ClearOutRenderTexture()
        {
            if (renderTexture == null)
            {
                Debug.LogError("RenderTexture not assigned!");
                return;
            }

            // Store the current active RenderTexture so you can restore it later
            RenderTexture currentRT = RenderTexture.active;

            // Set the target RenderTexture as the active one
            RenderTexture.active = renderTexture;

            // Clear the active RenderTexture with the specified color
            // The first 'true' clears the color buffer, the second clears the depth buffer
            GL.Clear(true, true, Color.clear);

            // Restore the previous active RenderTexture
            RenderTexture.active = currentRT;
        }

        private Vector2 WorldToUV(Vector3 worldPos)
        {
            float u = Mathf.InverseLerp(worldCanvas.worldMin.x, worldCanvas.worldMax.x, worldPos.x);
            float v = Mathf.InverseLerp(worldCanvas.worldMin.y, worldCanvas.worldMax.y, worldPos.z);
            return new Vector2(u, v);
        }
    }

    public class InteractorTraceSystemInstaller
    {
        public IDisposable Install(out VisualElement outRenderTextureVE)
        {
            var renderTexture = CreateRenderTexture(512);

            var worldMin = new Vector2(-10, -10);
            var worldMax = new Vector2(10, 10);
            var worldCanvas = new WorldCanvas
            {
                worldMin = new Vector2(-10, -10),
                worldMax = new Vector2(10, 10)
            };

            var painter = new InteractorTracePainter(
                renderTexture,
                new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/SNM_Unity/Runtime/GrassSystem/WorldTraceBrush.shader")),
                .01f, 1f,
                worldCanvas,
                paintCallback: () => { });
            var renderTextureVE = outRenderTextureVE = CreateTexturePreviewVE(renderTexture, painter, out var disposeCanvasVE);

            var painterMB = new GameObject($"[{nameof(InteractorTracePainterMB)}]").AddComponent<InteractorTracePainterMB>();
            painterMB.SetPainter(painter, paintCallback: renderTextureVE.MarkDirtyRepaint);
            painterMB.SetWorldCanvas(new WorldCanvasChecker(worldCanvas));

            var tracingAreaVisualizer = new WorldTraceAreaVisualizer(worldMin, worldMax);

            return new DisposeCallback(() =>
            {
                tracingAreaVisualizer.Cleanup();
                disposeCanvasVE.Dispose();

                UnityEngine.Object.DestroyImmediate(painterMB.gameObject);

                DestroyRenderTexture(renderTexture);
            });
        }

        private VisualElement CreateTexturePreviewVE(
            RenderTexture renderTexture,
            InteractorTracePainter painter,
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
            var button_Clear = new Button() { text = "Clear", clickable = new(painter.ClearOutRenderTexture) };

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
                graphicsFormat = GraphicsFormat.R8_UNorm,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = false,
                enableRandomWrite = false,
            };

            return RenderTexture.GetTemporary(desc);
        }

        public static void DestroyRenderTexture(RenderTexture renderTexture)
        {
            RenderTexture.ReleaseTemporary(renderTexture);
        }
    }

    public class WorldCanvasChecker
    {
        private readonly WorldCanvas worldCanvas;

        public WorldCanvasChecker(WorldCanvas worldCanvas)
        {
            this.worldCanvas = worldCanvas;
        }

        public bool IsInWorldCanvas(Vector3 worldPos)
        {
            return
                worldPos.x > worldCanvas.worldMin.x && worldPos.x < worldCanvas.worldMax.x
                &&
                worldPos.y > worldCanvas.worldMin.y && worldPos.y < worldCanvas.worldMax.y;
        }
    }
    public class WorldCanvas
    {
        public Vector2 worldMin;
        public Vector2 worldMax;
    }

    public class WorldTraceAreaVisualizer
    {
        private readonly Vector2 worldMin;
        private readonly Vector2 worldMax;

        public WorldTraceAreaVisualizer(Vector2 worldMin, Vector2 worldMax)
        {
            SceneView.duringSceneGui += SceneView_DuringSceneGui;
            this.worldMin = worldMin;
            this.worldMax = worldMax;
        }

        public void Cleanup()
        {
            SceneView.duringSceneGui -= SceneView_DuringSceneGui;
        }

        private void SceneView_DuringSceneGui(SceneView view)
        {
            var old = Handles.color;
            Handles.color = Color.red;
            Handles.DrawWireCube(Vector3.zero, new Vector3(worldMax.x - worldMin.x, 0, worldMax.y - worldMin.y));
            Handles.color = old;
        }
    }

    public class RTTestWindow : EditorWindow
    {
        private VisualElement _renderTextureVE;
        private IDisposable _disposable;

        [MenuItem("Tools/RTTestWindow")]
        private static void OpenWindow()
        {
            GetWindow<RTTestWindow>();
        }

        private void OnEnable()
        {
            _disposable = new InteractorTraceSystemInstaller().Install(out _renderTextureVE);
        }

        private void OnDisable()
        {
            _disposable?.Dispose();
            _disposable = null;
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(_renderTextureVE);
        }
    }
}