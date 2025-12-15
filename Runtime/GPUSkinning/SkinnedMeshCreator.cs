using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    public class SkinnedMeshCreator
    {
        public static Mesh CreateSkinnedMesh(Mesh mesh, BoneWeight[] boneWeights, Matrix4x4[] bindposes)
        {
            ExtractRawBoneWeights(boneWeights, out var boneWeights4, out var boneIndices4);

            var clonedMesh = UnityEngine.Object.Instantiate(mesh);
            clonedMesh.SetUVs(1, boneWeights4);
            clonedMesh.SetUVs(2, boneIndices4);
            clonedMesh.bindposes = bindposes;
            clonedMesh.UploadMeshData(true);

            return clonedMesh;
        }

        private static void ExtractRawBoneWeights(BoneWeight[] boneWeights, out List<Vector4> boneWeights4, out List<Vector4> boneIndices4)
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