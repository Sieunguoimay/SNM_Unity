using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning.Serialize
{
    public class SkeletonConverter
    {
        public static Skeleton TransformHierarchyToSkeleton(int[] parents, Matrix4x4[] bindposes)
        {
            return new Skeleton
            {
                bones = bindposes
                    .Select((tr, index) => new Bone
                    {
                        bindpose = tr,
                        parent = parents[index]
                    })
                    .ToArray()
            };
        }

        public static IEnumerable<Transform> TraverseHierarchy(Transform curr)
        {
            yield return curr;
            foreach (Transform c in curr)
            {
                foreach (var t in TraverseHierarchy(c))
                {
                    yield return t;
                }
            }
        }
    }
}