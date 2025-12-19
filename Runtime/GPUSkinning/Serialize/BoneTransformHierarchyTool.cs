using System.Linq;
using Snm.GPUSkinning.BoneWeightTool;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning.Serialize
{
    public class BoneTransformHierarchyTool
    {
        private readonly BoneTransformMB[] boneTransforms;

        public BoneTransformHierarchyTool(BoneTransformMB[] boneTransforms)
        {
            this.boneTransforms = boneTransforms;
        }

        public void ApplyHierarchy(int[] parents)
        {
            ApplyHierarchy(parents, boneTransforms.Select(bt => bt.transform).ToArray());
        }

        public static void ApplyHierarchy(int[] parents, Transform[] transforms)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                var tr = transforms[i];
                if (i >= parents.Length) continue;

                var parent = parents[i];
                if (parent >= transforms.Length) continue;

                tr.SetParent(transforms[parent]);
            }
        }

        public int[] GetHierarchy()
        {
            var parents = new int[boneTransforms.Length];

            for (int i = 0; i < boneTransforms.Length; i++)
            {
                var bt = boneTransforms[i];
                var parent = bt.transform.parent;

                parents[i] = -1;

                for (int boneIndex = 0; boneIndex < boneTransforms.Length; boneIndex++)
                {
                    BoneTransformMB bt2 = boneTransforms[boneIndex];
                    if (bt2.transform == parent)
                    {
                        parents[i] = boneIndex;
                    }
                }
            }

            return parents;
        }
    }
}