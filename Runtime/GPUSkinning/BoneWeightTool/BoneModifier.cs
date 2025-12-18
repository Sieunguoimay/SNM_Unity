using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
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

        public void SetBindpose(Matrix4x4 bindpose)
        {
            bone.bindpose = bindpose;
        }

        public bool ContainsVertex(int vertex)
        {
            return bone.vertices.Any(v => v.index == vertex);
        }
    }
}