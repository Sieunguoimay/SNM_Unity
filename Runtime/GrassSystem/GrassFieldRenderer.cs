using UnityEngine;

namespace Snm.Runtime.GrassSystem
{

    public class GrassFieldRenderer
    {
        private readonly Mesh mesh;
        private readonly Material material;
        private GraphicsBuffer _instanceBuffer;
        private GraphicsBuffer _argsBuffer;

        public GrassFieldRenderer(Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;
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
                GraphicsBuffer.Target.Raw,
                instanceCount,
                sizeof(float) * 16);
            _instanceBuffer.SetData(matrices);

            material.SetBuffer("_LocalToWorldMatrices", _instanceBuffer);

            _argsBuffer = CreateArgsBuffer(instanceCount);
        }

        private GraphicsBuffer CreateArgsBuffer(int instanceCount)
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
            material.SetVector("_WindParams", new Vector4(windData.strength, windData.scrollSpeed, windData.mapSize.x, windData.mapSize.y));
        }

        public void SetWorldCanvas(WorldCanvas worldCanvas)
        {
            var worldPos = worldCanvas.worldMin;
            var size = worldCanvas.worldMax - worldCanvas.worldMin;
            material.SetVector("_WorldCanvas", new Vector4(worldPos.x, worldPos.y, size.x, size.y));
        }

        public void SetTrampleConfig(RenderTexture trampleRT)
        {
            // var worldPos = worldCanvas.worldMin;
            // var size = worldCanvas.worldMax - worldCanvas.worldMin;

            material.SetTexture("_TrampleMap", trampleRT);
            // material.SetVector("_TrampleMap_ST", new Vector4(worldPos.x, worldPos.y, size.x, size.y));
        }

        public void Render()
        {
            if (_instanceBuffer == null) return;

            Graphics.RenderMeshIndirect(new(material), mesh, _argsBuffer);
        }
    }
}