using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Snm.GrassSystem
{
    public class GrassRenderer : IDisposable
    {
        static readonly int ID_LocalToWorldMatrices = Shader.PropertyToID("_LocalToWorldMatrices");
        static readonly int ID_WindMap = Shader.PropertyToID("_WindMap");
        static readonly int ID_WindParams = Shader.PropertyToID("_WindParams");
        static readonly int ID_WorldCanvas = Shader.PropertyToID("_WorldCanvas");
        static readonly int ID_TrampleMap = Shader.PropertyToID("_TrampleMap");
        static readonly int ID_BladeHeight = Shader.PropertyToID("_BladeHeight");

        public Material Material => _material;

        GraphicsBuffer _instanceBuffer;
        GraphicsBuffer _argsBuffer;
        Material _material;
        Mesh _mesh;
        Bounds _worldBounds;

        public void Setup(Mesh mesh, Material material, Matrix4x4[] matrices, Bounds worldBounds)
        {
            _mesh = mesh;
            _material = material;
            _worldBounds = worldBounds;

            int count = matrices.Length;

            _instanceBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                count,
                sizeof(float) * 16);
            _instanceBuffer.SetData(matrices);
            _material.SetBuffer(ID_LocalToWorldMatrices, _instanceBuffer);

            var args = new GraphicsBuffer.IndirectDrawIndexedArgs
            {
                indexCountPerInstance = mesh.GetIndexCount(0),
                instanceCount = (uint)count,
                startIndex = mesh.GetIndexStart(0),
                baseVertexIndex = mesh.GetBaseVertex(0),
                startInstance = 0
            };

            _argsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                1,
                GraphicsBuffer.IndirectDrawIndexedArgs.size);
            _argsBuffer.SetData(new[] { args });
        }

        public void SetWind(Texture2D map, float strength, float speed, Vector2 scale)
        {
            _material.SetTexture(ID_WindMap, map);
            _material.SetVector(ID_WindParams, new Vector4(strength, speed, scale.x, scale.y));
        }

        public void SetWorldCanvas(Vector4 canvas)
        {
            _material.SetVector(ID_WorldCanvas, canvas);
        }

        public void SetTrampleMap(Texture texture)
        {
            _material.SetTexture(ID_TrampleMap, texture);
        }

        public void SetBladeHeight(float height)
        {
            _material.SetFloat(ID_BladeHeight, height);
        }

        public void Render()
        {
            if (_instanceBuffer == null) return;

            var rparams = new RenderParams(_material) { worldBounds = _worldBounds };
            Graphics.RenderMeshIndirect(rparams, _mesh, _argsBuffer);
        }

        public void Dispose()
        {
            _instanceBuffer?.Dispose();
            _instanceBuffer = null;
            _argsBuffer?.Dispose();
            _argsBuffer = null;
        }
    }
}
