#if UNITY_EDITOR
#endif
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

        public void SetInteractor(Vector3 position, float radius)
        {
            material.SetVector("_InteractorPosAndRadius", new Vector4(position.x, position.y, position.z, radius));
        }

        public void SetTrampleRT(RenderTexture trampleRT, WorldCanvas worldCanvas)
        {
            var worldPos = worldCanvas.worldMin;
            var size = worldCanvas.worldMax - worldCanvas.worldMin;

            material.SetTexture("_TrampleRT", trampleRT);
            material.SetVector("_TrampleRect", new Vector4(worldPos.x, worldPos.y, size.x, size.y));
        }

        public void SetupSway(int count)
        {
            var randoms = new Vector4[count];

            for (int i = 0; i < count; i++)
            {
                randoms[i] = new Vector4(
                    Random.value,   // phase
                    Random.value,
                    Random.value,
                    Random.Range(0.5f, 1.2f) // stiffness
                );
            }

            mpb.SetVectorArray("_Random", randoms);
        }

        public void Render()
        {
            if (_matrices == null) return;

            Graphics.DrawMeshInstanced(mesh, 0, material, _matrices, _matrices.Length, mpb);
        }
    }
}