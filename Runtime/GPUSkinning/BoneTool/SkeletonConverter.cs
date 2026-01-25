using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Snm.Runtime.GPUSkinning.Serialize
{
#if UNITY_EDITOR
    public static class SkeletonConverterTool
    {
        [MenuItem("Assets/Prefab/ToSkeletonAsset")]
        private static void PrefabToSkeletonAsset()
        {
            if (Selection.activeObject is GameObject go)
            {
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                var bindposes = smr != null ? smr.sharedMesh.bindposes : SkeletonConverter.TraverseHierarchy(go.transform).Select(tr => tr.worldToLocalMatrix * go.transform.localToWorldMatrix).ToArray();
                var bones = smr != null ? smr.bones : SkeletonConverter.TraverseHierarchy(go.transform).ToArray();
                var parents = bones.Select(b => Array.IndexOf(bones, b.parent)).ToArray();

                var skeleton = SkeletonConverter.TransformHierarchyToSkeleton(parents, bindposes);
                var asset = ScriptableObject.CreateInstance<SkeletonAsset>();
                asset.skeleton = skeleton;
                asset.name = go.name;
                var selectedPath = AssetDatabase.GetAssetPath(go);
                var outputPath = AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(selectedPath) + "/" + asset.name + ".asset");
                AssetDatabase.CreateAsset(asset, outputPath);
            }
        }
    }
#endif

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