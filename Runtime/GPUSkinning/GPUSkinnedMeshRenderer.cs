using System.Linq;
using Snm.GPUSkinning.BoneWeightTool;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    public class GPUSkinnedMeshRenderer
    {
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly Transform[] boneTransforms;
        private readonly Transform meshTransform;
        private readonly Matrix4x4[] boneMatrices;
        private readonly Matrix4x4[] bindposes;

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
            boneMatrices = new Matrix4x4[boneTransforms.Length];
            bindposes = this.boneTransforms.Select(bt => bt.worldToLocalMatrix * this.meshTransform.localToWorldMatrix).ToArray();
        }

        public void SetupMesh()
        {
            BoneWeightConverter.ConvertToRaw(mesh.boneWeights, out var boneWeights4, out var boneIndices4);

            mesh.SetUVs(1, boneWeights4);
            mesh.SetUVs(2, boneIndices4);
            mesh.UploadMeshData(true);
        }

        public void SetupMaterial()
        {
            FillBoneMatrices(boneTransforms, bindposes, boneMatrices);

            material.SetInt("_BoneCount", boneMatrices.Length);

            if (boneMatrices.Length > 0)
            {
                material.SetMatrixArray("_Bones", boneMatrices);
            }
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
    }
}