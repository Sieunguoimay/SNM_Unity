using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    /// <summary>
    /// Low-level GPU skinning data uploader.
    /// Packs bone weights into TEXCOORD1, bone indices into TEXCOORD2,
    /// and uploads bone matrices via MaterialPropertyBlock for per-instance rendering.
    /// </summary>
    public class GPUSkinUploader
    {
        private static readonly int BoneCountId = Shader.PropertyToID("_BoneCount");
        private static readonly int BonesId = Shader.PropertyToID("_Bones");

        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly MaterialPropertyBlock _propertyBlock = new();
        private readonly Matrix4x4[] _skinningMatrices;
        private readonly int _boneCapacity;

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
    }
}
