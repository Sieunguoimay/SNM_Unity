using System.Collections.Generic;
using System.Linq;
using Snm.Runtime.GPUSkinning;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class RuntimeBoneImporter
    {
        public static RuntimeBone[] Import(SerializeBone[] bones, Matrix4x4[] bindposes, int[] parents)
        {
            return bones
                .Select((bone, index) => new RuntimeBone
                {
                    bindpose = bindposes[index],
                    parent = parents[index],
                    vertices = bone.vertices
                        .Select(v => new RuntimeVertex { index = v.index, boneWeight = v.boneWeight })
                        .ToList(),
                })
                .ToArray();
        }

        public static (SerializeBone[], Bone[]) Export(IEnumerable<RuntimeBone> bones)
        {
            return (
                bones
                    .Select(bone => new SerializeBone
                    {
                        vertices = bone.vertices
                            .Select(v => new SerializeVertex { index = v.index, boneWeight = v.boneWeight })
                            .ToArray(),
                    })
                    .ToArray(),
                bones
                    .Select(bone => new Bone
                    {
                        bindpose = bone.bindpose,
                        parent = bone.parent,
                    })
                    .ToArray());
        }
    }
}