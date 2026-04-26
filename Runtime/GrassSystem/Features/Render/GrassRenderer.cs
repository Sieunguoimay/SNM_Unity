using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Snm.GrassSystem
{
    public class GrassRenderer : IDisposable
    {
        static readonly int ID_LocalToWorldMatrices = Shader.PropertyToID("_LocalToWorldMatrices");
        static readonly int ID_WorldCanvas = Shader.PropertyToID("_WorldCanvas");
        static readonly int ID_BladeHeight = Shader.PropertyToID("_BladeHeight");

        public Material Material => _material;

        GraphicsBuffer _instanceBuffer;
        GraphicsBuffer _argsBuffer;
        Material _material;
        Mesh _mesh;
        Bounds _worldBounds;
        Matrix4x4[] _allMatrices;
        uint _indexCountPerInstance;

        readonly GraphicsBuffer.IndirectDrawIndexedArgs[] _argsScratch = new GraphicsBuffer.IndirectDrawIndexedArgs[1];

        public void Setup(Mesh mesh, Material material, Matrix4x4[] matrices, Bounds worldBounds)
        {
            _mesh = mesh;
            _material = new Material(material);
            _worldBounds = worldBounds;

            int count = matrices.Length;
            _allMatrices = matrices;

            _instanceBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                count,
                sizeof(float) * 16);
            _instanceBuffer.SetData(matrices);
            _material.SetBuffer(ID_LocalToWorldMatrices, _instanceBuffer);

            _indexCountPerInstance = mesh.GetIndexCount(0);
            _argsScratch[0] = new GraphicsBuffer.IndirectDrawIndexedArgs
            {
                indexCountPerInstance = _indexCountPerInstance,
                instanceCount = (uint)count,
                startIndex = mesh.GetIndexStart(0),
                baseVertexIndex = mesh.GetBaseVertex(0),
                startInstance = 0
            };

            _argsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                1,
                GraphicsBuffer.IndirectDrawIndexedArgs.size);
            _argsBuffer.SetData(_argsScratch);
        }

        public void SetWorldCanvas(Vector4 canvas)
        {
            _material.SetVector(ID_WorldCanvas, canvas);
        }

        public void SetBladeHeight(float height)
        {
            _material.SetFloat(ID_BladeHeight, height);
        }

        public IReadOnlyList<Matrix4x4> AllMatrices => _allMatrices;

        public void UpdateVisibleInstances(Matrix4x4[] matrices, int count)
        {
            if (_instanceBuffer == null || count == 0) return;
            _instanceBuffer.SetData(matrices, 0, 0, count);

            _argsScratch[0] = new GraphicsBuffer.IndirectDrawIndexedArgs
            {
                indexCountPerInstance = _indexCountPerInstance,
                instanceCount = (uint)count,
                startIndex = _mesh.GetIndexStart(0),
                baseVertexIndex = _mesh.GetBaseVertex(0),
                startInstance = 0
            };
            _argsBuffer.SetData(_argsScratch);
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
            if (_material != null)
            {
                UnityEngine.Object.Destroy(_material);
                _material = null;
            }
        }
    }
}
