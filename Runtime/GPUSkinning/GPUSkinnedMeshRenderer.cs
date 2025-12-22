using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    public class GPUSkinnedMeshRendererCore
    {
        private const int MAX_BONES = 256;

        private readonly Mesh mesh;
        private readonly Material material;
        private readonly Matrix4x4[] skinningMatrices = new Matrix4x4[MAX_BONES];

        public GPUSkinnedMeshRendererCore(
            Mesh mesh,
            Material material)
        {
            this.mesh = mesh;
            this.material = material;
        }

        public void UploadMeshDataViaMesh()
        {
            ConvertToRaw(mesh.vertexCount, mesh.boneWeights, out var boneWeights4, out var boneIndices4);
            mesh.SetUVs(1, boneWeights4);
            mesh.SetUVs(2, boneIndices4);
            mesh.UploadMeshData(true);
        }

        public void SetSkinningMatrix(int boneIndex, Matrix4x4 matrix)
        {
            skinningMatrices[boneIndex] = matrix;
        }

        public void UploadBoneMatricesViaMaterial(int boneCount)
        {
            material.SetInt("_BoneCount", boneCount);
            material.SetMatrixArray("_Bones", skinningMatrices);
        }

        public void Render(Matrix4x4 meshToWorld)
        {
            Graphics.DrawMesh(mesh, meshToWorld, material, 0);
        }

        public static void ConvertToRaw(int vertexCount, BoneWeight[] boneWeights, out List<Vector4> boneWeights4, out List<Vector4> boneIndices4)
        {
            boneWeights4 = new List<Vector4>(vertexCount);
            boneIndices4 = new List<Vector4>(vertexCount);

            for (int i = 0; i < vertexCount; i++)
            {
                var w = Vector4.zero;
                var idx = Vector4.zero;

                if (i < boneWeights.Length)
                {
                    var bw = boneWeights[i];
                    w = new Vector4(bw.weight0, bw.weight1, bw.weight2, bw.weight3);
                    idx = new Vector4(bw.boneIndex0, bw.boneIndex1, bw.boneIndex2, bw.boneIndex3);
                }

                boneWeights4.Add(w);
                boneIndices4.Add(idx);
            }
        }
    }

    public class GPUSkinnedMeshRenderer
    {
        private readonly GPUSkinnedMeshRendererCore core;
        private readonly Matrix4x4[] bindposes;
        private readonly Transform[] boneTransforms;
        private readonly Transform meshTransform;

        public GPUSkinnedMeshRenderer(
            Mesh mesh,
            Matrix4x4[] bindposes,
            Material material,
            Transform[] boneTransforms,
            Transform meshTransform)
        {
            core = new(mesh, material);
            this.bindposes = bindposes;
            this.boneTransforms = boneTransforms;
            this.meshTransform = meshTransform;
        }

        public void SetupMesh()
        {
            core.UploadMeshDataViaMesh();
        }

        public void SetupMaterial()
        {
            FillBoneMatrices(boneTransforms);

            core.UploadBoneMatricesViaMaterial(boneTransforms.Length);
        }

        public void Render()
        {
            core.Render(meshTransform.localToWorldMatrix);
        }

        private void FillBoneMatrices(Transform[] boneTransforms)
        {
            for (int i = 0; i < boneTransforms.Length; i++)
            {
                var boneToWorld = boneTransforms[i].localToWorldMatrix * bindposes[i];
                core.SetSkinningMatrix(i, boneToWorld);
            }
        }
    }
}