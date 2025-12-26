#if UNITY_EDITOR
#endif
using System;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class InteractorTracePainter
    {
        private readonly RenderTexture renderTexture;
        private readonly RenderTexture renderTexture2;
        private readonly Material material;
        private readonly float brushRadius;
        private readonly float brushStrength;
        private readonly WorldCanvas worldCanvas;
        private readonly Action paintCallback;
        private bool useAasSource;

        public InteractorTracePainter(
            RenderTexture renderTexture,
            RenderTexture renderTexture2,
            Material material,
            float brushRadius,
            float brushStrength,
            WorldCanvas worldCanvas,
            Action paintCallback)
        {
            this.renderTexture = renderTexture;
            this.renderTexture2 = renderTexture2;
            this.material = material;
            this.brushRadius = brushRadius;
            this.brushStrength = brushStrength;
            this.worldCanvas = worldCanvas;
            this.paintCallback = paintCallback;
        }

        public void SetTexture()
        {
            material.SetTexture("_MainTex", renderTexture);
        }

        public void Paint(Vector3 worldPos, Vector3 brushColor)
        {
            if (renderTexture == null) return;

            var uv = WorldToUV(worldPos);
            material.SetVector("_BrushParams", new Vector4(uv.x, uv.y, brushRadius, brushStrength));
            material.SetVector("_BrushColor", brushColor);//new Vector4(worldPos.x, worldPos.y, worldPos.z, 1));

            var rtA = renderTexture;
            var rtB = renderTexture2;

            var src = useAasSource ? rtA : rtB;
            var dst = useAasSource ? rtB : rtA;

            Graphics.Blit(src, dst, material);

            useAasSource = !useAasSource;
        }

        public void ClearOutRenderTextures()
        {
            ClearOutRenderTexture(renderTexture);
            ClearOutRenderTexture(renderTexture2);
        }

        public static void ClearOutRenderTexture(RenderTexture renderTexture)
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
            // float u = Mathf.InverseLerp(worldCanvas.worldMin.x, worldCanvas.worldMax.x, worldPos.x);
            // float v = Mathf.InverseLerp(worldCanvas.worldMin.y, worldCanvas.worldMax.y, worldPos.z);
            // return new Vector2(u, 1f - v);
            var uv = (new Vector2(worldPos.x, worldPos.z) - worldCanvas.worldMin) / (worldCanvas.worldMax - worldCanvas.worldMin);
            return new Vector2(uv.x, 1f - uv.y);
        }
    }
}