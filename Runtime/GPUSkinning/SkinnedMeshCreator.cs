using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    public class BoneDataModifier
    {
        private readonly BoneData[] bones;

        public BoneDataModifier(BoneData[] bones)
        {
            this.bones = bones;
        }

        public void SetVertexWeights(int boneIndex, VertexData[] vertexWeights)
        {
            bones[boneIndex].vertices = new List<VertexData>(vertexWeights);
        }

        public void SetVertex(int boneIndex, int vertexIndex, float weight)
        {
            var bone = bones[boneIndex];
            if (bone.vertices == null)
            {
                bone.vertices = new List<VertexData> { new() { index = vertexIndex, boneWeight = weight } };
            }
            else
            {
                bone.vertices.Add(new() { index = vertexIndex, boneWeight = weight });
            }
        }
    }

    public class BoneWeightExtractor
    {
        public static BoneWeight[] ExtractBoneWeights(BoneData[] bones, int vertexCount)
        {
            var weights = new List<(int bone, float weight)>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                weights[i] = new List<(int, float)>();

            // Fill accumulators
            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                var cfg = bones[boneIndex];
                if (cfg.vertices == null) continue;

                foreach (var vbw in cfg.vertices)
                {
                    if (vbw.index < 0 || vbw.index >= vertexCount)
                        continue;

                    weights[vbw.index].Add((boneIndex, vbw.boneWeight));
                }
            }

            // Convert accumulators → BoneWeight array
            var final = new BoneWeight[vertexCount];

            for (int v = 0; v < vertexCount; v++)
            {
                var list = weights[v];

                if (list.Count == 0)
                {
                    final[v] = new BoneWeight(); // no weight
                    continue;
                }

                // Sort by weight desc
                list.Sort((a, b) => b.weight.CompareTo(a.weight));

                // Unity supports only 4 weights
                var count = Mathf.Min(list.Count, 4);

                var total = 0f;
                for (int i = 0; i < count; i++)
                    total += list[i].weight;
                if (total < 1e-6f) total = 1f; // avoid division by zero

                var bw = new BoneWeight();

                if (count > 0) { bw.boneIndex0 = list[0].bone; bw.weight0 = list[0].weight / total; }
                if (count > 1) { bw.boneIndex1 = list[1].bone; bw.weight1 = list[1].weight / total; }
                if (count > 2) { bw.boneIndex2 = list[2].bone; bw.weight2 = list[2].weight / total; }
                if (count > 3) { bw.boneIndex3 = list[3].bone; bw.weight3 = list[3].weight / total; }

                final[v] = bw;
            }
            return final;
        }

        public static void ExtractRawBoneWeights(BoneWeight[] boneWeights, out List<Vector4> boneWeights4, out List<Vector4> boneIndices4)
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

    public class BoneDataCreator
    {
        public static BoneData[] CreateBones(int boneCount)
        {
            var bones = new BoneData[boneCount];

            for (int i = 0; i < boneCount; i++)
            {
                bones[i] = new BoneData();
            }

            return bones;
        }
    }

    public class BoneData
    {
        public List<VertexData> vertices;
    }

    public class VertexData
    {
        public int index;
        public float boneWeight;
    }

    public class SkinnedMeshCreator
    {
        public static Mesh CreateSkinnedMesh(Mesh mesh, BoneWeight[] boneWeights)
        {
            BoneWeightExtractor.ExtractRawBoneWeights(boneWeights, out var boneWeights4, out var boneIndices4);

            var clonedMesh = Object.Instantiate(mesh);
            clonedMesh.SetUVs(1, boneWeights4);
            clonedMesh.SetUVs(2, boneIndices4);
            clonedMesh.UploadMeshData(true);

            return clonedMesh;
        }
    }
}