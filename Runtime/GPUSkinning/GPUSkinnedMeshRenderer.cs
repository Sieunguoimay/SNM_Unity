using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    public class GPUSkinnedMeshRenderer
    {
        private const int MAX_BONES = 256;

        private readonly Mesh mesh;
        private readonly Material material;
        private readonly Transform[] boneTransforms;
        private readonly Transform meshTransform;
        private readonly Matrix4x4[] boneMatrices = new Matrix4x4[MAX_BONES];

        public GPUSkinnedMeshRenderer(
            Mesh mesh,
            Material material,
            Transform[] boneTransforms,
            Transform meshTransform)
        {
            this.mesh = mesh;
            this.material = material;
            this.boneTransforms = boneTransforms;
            this.meshTransform = meshTransform;
        }

        public void SetupMesh()
        {
            ConvertToRaw(mesh.boneWeights, out var boneWeights4, out var boneIndices4);

            mesh.SetUVs(1, boneWeights4);
            mesh.SetUVs(2, boneIndices4);
            mesh.UploadMeshData(true);
        }

        public void SetupMaterial()
        {
            FillBoneMatrices(boneTransforms, mesh.bindposes, boneMatrices);

            material.SetInt("_BoneCount", boneMatrices.Length);
            material.SetMatrixArray("_Bones", boneMatrices);
        }

        public void Render()
        {
            Graphics.DrawMesh(mesh, meshTransform.localToWorldMatrix, material, 0);
        }

        private static void FillBoneMatrices(
            Transform[] boneTransforms,
            Matrix4x4[] bindposes,
            Matrix4x4[] boneMatrices)
        {
            for (int i = 0; i < boneTransforms.Length; i++)
            {
                var boneToWorld = boneTransforms[i].localToWorldMatrix;
                var bindpose = bindposes[i];

                boneMatrices[i] = boneToWorld * bindpose;
            }
        }

        public static void ConvertToRaw(BoneWeight[] boneWeights, out List<Vector4> boneWeights4, out List<Vector4> boneIndices4)
        {
            boneWeights4 = new List<Vector4>(boneWeights.Length);
            boneIndices4 = new List<Vector4>(boneWeights.Length);
            for (int i = 0; i < boneWeights.Length; i++)
            {
                var bw = boneWeights[i];
                var w = new Vector4(bw.weight0, bw.weight1, bw.weight2, bw.weight3);
                var idx = new Vector4(bw.boneIndex0, bw.boneIndex1, bw.boneIndex2, bw.boneIndex3);
                boneWeights4.Add(w);
                boneIndices4.Add(idx);
            }
        }
    }
}