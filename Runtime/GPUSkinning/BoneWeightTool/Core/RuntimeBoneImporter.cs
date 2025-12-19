using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEditor;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class RuntimeBoneImporter
    {
        public static RuntimeBone[] Import(SerializeBone[] bones)
        {
            return bones
                .Select(bone => new RuntimeBone
                {
                    bindpose = bone.bindpose,
                    vertices = bone.vertices
                        .Select(v => new RuntimeVertex { index = v.index, boneWeight = v.boneWeight })
                        .ToList(),
                })
                .ToArray();
        }

        public static SerializeBone[] Export(IEnumerable<RuntimeBone> bones)
        {
            return bones
                .Select(bone => new SerializeBone
                {
                    bindpose = bone.bindpose,
                    vertices = bone.vertices
                        .Select(v => new SerializeVertex { index = v.index, boneWeight = v.boneWeight })
                        .ToArray(),
                })
                .ToArray();
        }
    }
}