using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassFieldRenderer
    {
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly MaterialPropertyBlock mpb = new();
        private Matrix4x4[] _matrices;

        public GrassFieldRenderer(Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;
        }

        public void SetMatrices(Matrix4x4[] matrices)
        {
            _matrices = matrices;
        }

        public void SetWindConfig(Texture2D windMap, float strength, float speed, Vector2 mapSize)
        {
            material.SetTexture("_WindMap", windMap);
            material.SetVector("_WindParams", new Vector4(strength, speed, mapSize.x, mapSize.y));
        }

        public void SetTrampleConfig(RenderTexture trampleRT, WorldCanvas worldCanvas)
        {
            var worldPos = worldCanvas.worldMin;
            var size = worldCanvas.worldMax - worldCanvas.worldMin;

            material.SetTexture("_TrampleMap", trampleRT);
            material.SetVector("_TrampleMap_ST", new Vector4(worldPos.x, worldPos.y, size.x, size.y));
        }

        public void Render()
        {
            if (_matrices == null) return;

            Graphics.DrawMeshInstanced(mesh, 0, material, _matrices, _matrices.Length, mpb);
        }
    }
}