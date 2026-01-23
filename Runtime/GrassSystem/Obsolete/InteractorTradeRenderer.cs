#if UNITY_EDITOR
#endif
using System;
using UnityEngine;

namespace Snm.Runtime.GrassSystem.Obsolete
{
    public class InteractorTracePainter
    {
        private readonly RenderTexture renderTexture;
        private readonly RenderTexture renderTexture2;
        private readonly Material material;
        private readonly float brushRadius;
        private readonly WorldCanvas worldCanvas;
        private bool useAasSource;

        public InteractorTracePainter(
            RenderTexture renderTexture,
            RenderTexture renderTexture2,
            Material material,
            float brushRadius,
            WorldCanvas worldCanvas)
        {
            this.renderTexture = renderTexture;
            this.renderTexture2 = renderTexture2;
            this.material = material;
            this.brushRadius = brushRadius;
            this.worldCanvas = worldCanvas;
        }

        public void SetTexture()
        {
            var origin = worldCanvas.worldMin;
            var size = worldCanvas.worldMax - worldCanvas.worldMin;
            material.SetTexture("_MainTex", renderTexture);
            material.SetVector("_WorldCanvas", new Vector4(origin.x, origin.y, size.x, size.y));
        }

        public void Paint(Vector3 worldPos, Vector3 brushDir, float deltaTime)
        {
            if (renderTexture == null) return;

            material.SetVector("_BrushParams", new Vector4(worldPos.x, worldPos.y, worldPos.z, brushRadius));
            material.SetVector("_BrushDir", new Vector4(brushDir.x, brushDir.y, brushDir.z, deltaTime));
            material.SetFloat("_DeltaTime", deltaTime);

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
    }
}