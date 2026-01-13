using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassTramplePainter
    {
        private readonly Material material;

        public GrassTramplePainter(Material material)
        {
            this.material = material;
        }

        public void SetBrush(
            Vector3 worldPos,
            float radius,
            Vector4 color)
        {
            material.SetVector("_BrushParams", new Vector4(worldPos.x, worldPos.z, radius, 0f));
            material.SetVector("_BrushDir", color);
        }
    }
}