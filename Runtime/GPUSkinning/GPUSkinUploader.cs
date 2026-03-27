using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    /// <summary>
    /// Low-level GPU skinning data uploader.
    /// Packs bone weights into TEXCOORD1, bone indices into TEXCOORD2,
    /// and uploads bone matrices via MaterialPropertyBlock for per-instance rendering.
    /// Optionally supports blend shapes via StructuredBuffer.
    /// </summary>
    public class GPUSkinUploader : IDisposable
    {
        private static readonly int BoneCountId = Shader.PropertyToID("_BoneCount");
        private static readonly int BonesId = Shader.PropertyToID("_Bones");
        private static readonly int BlendShapeBufferId = Shader.PropertyToID("_BlendShapeBuffer");
        private static readonly int BlendShapeCountId = Shader.PropertyToID("_BlendShapeCount");
        private static readonly int BlendShapeVertexCountId = Shader.PropertyToID("_BlendShapeVertexCount");
        private static readonly int BlendShapeWeightsId = Shader.PropertyToID("_BlendShapeWeights");

        private const int MaxBlendShapes = 8;

        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly MaterialPropertyBlock _propertyBlock = new();
        private readonly Matrix4x4[] _skinningMatrices;
        private readonly int _boneCapacity;

        private ComputeBuffer _blendShapeBuffer;
        private int _blendShapeCount;
        private float[] _blendShapeWeights;
        private bool _blendShapeWeightsDirty;

        public int BlendShapeCount => _blendShapeCount;

        public GPUSkinUploader(Mesh mesh, Material material, int boneCount)
        {
            _mesh = mesh;
            _material = material;
            _boneCapacity = boneCount;
            _skinningMatrices = new Matrix4x4[boneCount > 0 ? boneCount : 1];
        }

        /// <summary>
        /// Converts mesh bone weights to UV channels and uploads to GPU.
        /// TEXCOORD1 = bone weights (xyzw), TEXCOORD2 = bone indices (xyzw).
        /// </summary>
        public void UploadMeshData()
        {
            var vertexCount = _mesh.vertexCount;
            var boneWeights = _mesh.boneWeights;
            var weights = new List<Vector4>(vertexCount);
            var indices = new List<Vector4>(vertexCount);

            for (int i = 0; i < vertexCount; i++)
            {
                if (i < boneWeights.Length)
                {
                    var bw = boneWeights[i];
                    weights.Add(new Vector4(bw.weight0, bw.weight1, bw.weight2, bw.weight3));
                    indices.Add(new Vector4(bw.boneIndex0, bw.boneIndex1, bw.boneIndex2, bw.boneIndex3));
                }
                else
                {
                    weights.Add(Vector4.zero);
                    indices.Add(Vector4.zero);
                }
            }

            _mesh.SetUVs(1, weights);
            _mesh.SetUVs(2, indices);
            _mesh.UploadMeshData(false);
        }

        /// <summary>
        /// Extracts blend shape deltas from the mesh and uploads to a StructuredBuffer.
        /// Each blend shape stores per-vertex position and normal deltas.
        /// Enables the BLEND_SHAPES_ON keyword on the material.
        /// </summary>
        public void UploadBlendShapeData()
        {
            _blendShapeCount = _mesh.blendShapeCount;
            if (_blendShapeCount <= 0) return;

            int shapeCount = Mathf.Min(_blendShapeCount, MaxBlendShapes);
            int vertexCount = _mesh.vertexCount;

            var deltaPositions = new Vector3[vertexCount];
            var deltaNormals = new Vector3[vertexCount];
            var deltaTangents = new Vector3[vertexCount];

            // Each entry: float3 positionDelta + float3 normalDelta = 6 floats
            var bufferData = new float[shapeCount * vertexCount * 6];

            for (int s = 0; s < shapeCount; s++)
            {
                int frameIndex = _mesh.GetBlendShapeFrameCount(s) - 1;
                _mesh.GetBlendShapeFrameVertices(s, frameIndex, deltaPositions, deltaNormals, deltaTangents);

                int shapeOffset = s * vertexCount * 6;
                for (int v = 0; v < vertexCount; v++)
                {
                    int idx = shapeOffset + v * 6;
                    bufferData[idx + 0] = deltaPositions[v].x;
                    bufferData[idx + 1] = deltaPositions[v].y;
                    bufferData[idx + 2] = deltaPositions[v].z;
                    bufferData[idx + 3] = deltaNormals[v].x;
                    bufferData[idx + 4] = deltaNormals[v].y;
                    bufferData[idx + 5] = deltaNormals[v].z;
                }
            }

            _blendShapeBuffer?.Release();
            // Stride = 6 floats (float3 pos + float3 normal) = 24 bytes
            _blendShapeBuffer = new ComputeBuffer(shapeCount * vertexCount, 24);
            _blendShapeBuffer.SetData(bufferData);

            _blendShapeWeights = new float[MaxBlendShapes];
            _blendShapeCount = shapeCount;

            _material.EnableKeyword("BLEND_SHAPES_ON");
            _propertyBlock.SetBuffer(BlendShapeBufferId, _blendShapeBuffer);
            _propertyBlock.SetInt(BlendShapeCountId, _blendShapeCount);
            _propertyBlock.SetInt(BlendShapeVertexCountId, vertexCount);
            _propertyBlock.SetFloatArray(BlendShapeWeightsId, _blendShapeWeights);
        }

        /// <summary>
        /// Sets the weight for a blend shape by index (0 to BlendShapeCount-1).
        /// Values are typically 0-1 but can exceed that range for exaggerated effects.
        /// </summary>
        public void SetBlendShapeWeight(int shapeIndex, float weight)
        {
            if (_blendShapeWeights == null || shapeIndex < 0 || shapeIndex >= _blendShapeCount) return;
            _blendShapeWeights[shapeIndex] = weight;
            _blendShapeWeightsDirty = true;
        }

        /// <summary>
        /// Uploads blend shape weights to the GPU. Call once per frame after setting weights.
        /// </summary>
        public void UploadBlendShapeWeights()
        {
            if (!_blendShapeWeightsDirty || _blendShapeWeights == null) return;
            _propertyBlock.SetFloatArray(BlendShapeWeightsId, _blendShapeWeights);
            _blendShapeWeightsDirty = false;
        }

        public void SetSkinningMatrix(int boneIndex, Matrix4x4 matrix)
        {
            _skinningMatrices[boneIndex] = matrix;
        }

        /// <summary>
        /// Uploads bone matrices to the GPU via MaterialPropertyBlock (per-instance, no material mutation).
        /// </summary>
        public void UploadBoneMatrices(int boneCount)
        {
            _propertyBlock.SetInt(BoneCountId, boneCount);
            _propertyBlock.SetMatrixArray(BonesId, _skinningMatrices);
        }

        public void Render(Matrix4x4 meshToWorld)
        {
            Graphics.DrawMesh(_mesh, meshToWorld, _material, 0, null, 0, _propertyBlock);
        }

        public void Dispose()
        {
            _blendShapeBuffer?.Release();
            _blendShapeBuffer = null;
        }
    }
}
