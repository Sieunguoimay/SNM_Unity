#if UNITY_EDITOR
#endif
using System;
using UnityEditor;
using UnityEngine;
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
            return new Vector2(u, 1f - v);
        }
    }
}