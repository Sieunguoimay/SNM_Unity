using System;
using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneModifier
    {
        private readonly RuntimeBone bone;

        public BoneModifier(RuntimeBone bone)
        {
            this.bone = bone;
        }

        public void AddVertex(int vertexIndex, float weight)
        {
            bone.vertices ??= new List<RuntimeVertex>();
            bone.vertices.Add(new RuntimeVertex { index = vertexIndex, boneWeight = weight });
        }

        public void ClearVertices()
        {
            bone.vertices?.Clear();
        }

        public void RemoveVertex(int vertexIndex)
        {
            if (bone.vertices == null) return;

            bone.vertices.RemoveAll(v => v.index == vertexIndex);
        }
    }

    public class RuntimeBoneCollection
    {
        private RuntimeBone[] _bones;

        public IReadOnlyList<RuntimeBone> Bones => _bones;

        public event Action OnBonesChanged;

        public void SetBones(RuntimeBone[] bones)
        {
            _bones = bones;
            OnBonesChanged?.Invoke();
        }
    }

    public class RuntimeBoneImporter
    {
        private readonly RuntimeBoneCollection boneCollection;

        public RuntimeBoneCollection BoneCollection => boneCollection;

        public RuntimeBoneImporter(RuntimeBoneCollection boneCollection)
        {
            this.boneCollection = boneCollection;
        }

        public void Import(SerializeBone[] bones)
        {
            var runtimeBones = bones.Select(bone =>
            {
                var runtimeBone = new RuntimeBone();
                if (bone.vertices != null)
                {
                    runtimeBone.vertices = bone.vertices
                        .Select(v => new RuntimeVertex { index = v.index, boneWeight = v.boneWeight })
                        .ToList();
                }
                return runtimeBone;
            }).ToArray();

            boneCollection.SetBones(runtimeBones);
        }

        public SerializeBone[] Export()
        {
            return boneCollection.Bones.Select(bone =>
            {
                var serializeBone = new SerializeBone();
                if (bone.vertices != null)
                {
                    serializeBone.vertices = bone.vertices
                        .Select(v => new SerializeVertex { index = v.index, boneWeight = v.boneWeight })
                        .ToArray();
                }
                return serializeBone;
            }).ToArray();
        }
    }

    public static class BoneWeightConverter
    {
        public static SerializeBone[] ConvertToBoneDatas(BoneWeight[] boneWeights)
        {
            if (boneWeights.Length == 0) return Array.Empty<SerializeBone>();

            int maxBoneIndex = InferBoneCount(boneWeights);
            return ConvertToBoneDatas(boneWeights, maxBoneIndex + 1);
        }

        public static int InferBoneCount(BoneWeight[] boneWeights)
        {
            return boneWeights.Max(w => Mathf.Max(w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3));
        }

        public static SerializeBone[] ConvertToBoneDatas(BoneWeight[] boneWeights, int boneCount)
        {
            var bones = new SerializeBone[boneCount];

            for (int v = 0; v < boneWeights.Length; v++)
            {
                var bw = boneWeights[v];

                void AddWeight(int boneIndex, float weight)
                {
                    if (weight > 1e-6f)
                    {
                        var bone = bones[boneIndex];
                        if (bone == null)
                        {
                            bone = new SerializeBone();
                            bones[boneIndex] = bone;
                        }
                        var vertexList = new List<SerializeVertex>(bone.vertices ?? Array.Empty<SerializeVertex>())
                        {
                            new() { index = v, boneWeight = weight }
                        };
                        bone.vertices = vertexList.ToArray();
                    }
                }

                AddWeight(bw.boneIndex0, bw.weight0);
                AddWeight(bw.boneIndex1, bw.weight1);
                AddWeight(bw.boneIndex2, bw.weight2);
                AddWeight(bw.boneIndex3, bw.weight3);
            }

            return bones;
        }

        public static BoneWeight[] ConvertToBoneWeights(SerializeBone[] bones, int vertexCount)
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