using System;
using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{

    public static class BoneWeightConverter
    {
        public static RuntimeBone[] ConvertToBoneDatas(
            BoneWeight[] boneWeights,
            int boneCount)
        {
            var bones = new RuntimeBone[boneCount];

            for (int i = 0; i < bones.Length; i++)
            {
                bones[i] = new RuntimeBone
                {
                    vertices = new()
                };
            }

            for (int v = 0; v < boneWeights.Length; v++)
            {
                var bw = boneWeights[v];

                void AddWeight(int boneIndex, float weight)
                {
                    if (weight > 1e-6f && boneIndex < bones.Length)
                    {
                        var bone = bones[boneIndex];
                        bone.vertices ??= new();
                        bone.vertices.Add(new() { index = v, boneWeight = weight });
                    }
                }

                AddWeight(bw.boneIndex0, bw.weight0);
                AddWeight(bw.boneIndex1, bw.weight1);
                AddWeight(bw.boneIndex2, bw.weight2);
                AddWeight(bw.boneIndex3, bw.weight3);
            }

            return bones;
        }

        public static BoneWeight[] ExtractBoneWeights(RuntimeBone[] bones, int vertexCount)
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
    }
}