using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassFieldRenderer
    {
        private readonly Mesh mesh;
        private readonly Material material;
        private GraphicsBuffer _instanceBuffer;
        private GraphicsBuffer _argsBuffer;
        private Bounds _worldBounds;

        public GrassFieldRenderer(Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;
            this.material.enableInstancing = true;
        }

        public void Cleanup()
        {
            _argsBuffer?.Dispose();
            _instanceBuffer?.Dispose();

            _instanceBuffer = null;
            _argsBuffer = null;
        }

        public void SetMatrices(Matrix4x4[] matrices)
        {
            var instanceCount = matrices.Length;

            _instanceBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                instanceCount,
                sizeof(float) * 16);
            _instanceBuffer.SetData(matrices);

            material.SetBuffer("_LocalToWorldMatrices", _instanceBuffer);

            _argsBuffer = CreateArgsBuffer(mesh, instanceCount);
        }

        private static GraphicsBuffer CreateArgsBuffer(Mesh mesh, int instanceCount)
        {
            var args = new GraphicsBuffer.IndirectDrawIndexedArgs
            {
                indexCountPerInstance = mesh.GetIndexCount(0),
                instanceCount = (uint)instanceCount,
                startIndex = mesh.GetIndexStart(0),
                baseVertexIndex = mesh.GetBaseVertex(0),
                startInstance = 0
            };

            var argsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                1,
                GraphicsBuffer.IndirectDrawIndexedArgs.size);
            argsBuffer.SetData(new[] { args });

            return argsBuffer;
        }

        public void SetWindConfig(WindConfig windData)
        {
            material.SetTexture("_WindMap", windData.dudvMap);
            material.SetVector("_WindParams", new Vector4(windData.strength, windData.scrollSpeed, windData.mapScale.x, windData.mapScale.y));
        }

        public void SetWorldCanvas(SurfaceCanvas canvas)
        {
            var min = canvas.WorldMin;
            var max = canvas.WorldMax;
            var size = max - min;
            material.SetVector("_WorldCanvas", new Vector4(min.x, min.y, size.x, size.y));
        }

        public void SetTrampleMap(Texture trampleMap)
        {
            material.SetTexture("_TrampleMap", trampleMap);
        }

        public void SetWorldBounds(Bounds worldBounds)
        {
            _worldBounds = worldBounds;
        }

        public void Render()
        {
            if (_instanceBuffer == null) return;

            var rparams = new RenderParams(material) { worldBounds = _worldBounds };

            Graphics.RenderMeshIndirect(rparams, mesh, _argsBuffer);
        }
    }
}