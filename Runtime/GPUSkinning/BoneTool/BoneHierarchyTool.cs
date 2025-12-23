using System.Linq;
using Snm.GPUSkinning.BoneWeightTool;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning.Serialize
{
    public class BoneHierarchyTool
    {
        private int[] _parents;

        public int[] Parents => _parents;

        public void AddNew()
        {
            _parents = _parents.Append(-1).ToArray();
        }

        public void SetParents(int[] parents)
        {
            _parents = parents;
        }

        public static void ApplySkeletonPoses(int[] parents, Matrix4x4[] bindposes, Transform[] transforms)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                var tr = transforms[i];
                if (i >= parents.Length) continue;

                var parent = parents[i];
                if (parent < 0 || parent >= transforms.Length) continue;

                tr.SetParent(transforms[parent]);
            }
        }

        public static void ApplyHierarchy(int[] parents, Transform[] transforms)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                var tr = transforms[i];
                if (i >= parents.Length) continue;

                var parent = parents[i];
                if (parent < 0 || parent >= transforms.Length) continue;

                tr.SetParent(transforms[parent]);
            }
        }

        public static int[] ExtractHierarchy(Transform[] boneTransforms)
        {
            var parents = new int[boneTransforms.Length];

            for (int i = 0; i < boneTransforms.Length; i++)
            {
                var bt = boneTransforms[i];
                var parent = bt.parent;

                parents[i] = -1;

                for (int boneIndex = 0; boneIndex < boneTransforms.Length; boneIndex++)
                {
                    var bt2 = boneTransforms[boneIndex];
                    if (bt2 == parent)
                    {
                        parents[i] = boneIndex;
                    }
                }
            }

            return parents;
        }
    }
}