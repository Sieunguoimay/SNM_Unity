using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning.Serialize
{
    public class SkeletonAsset : ScriptableObject
    {
        public Skeleton skeleton;
    }

    public static class SkeletonConverterTool
    {
        [UnityEditor.MenuItem("Assets/Prefab/ToSkeletonAsset")]
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

    [Serializable]
    public class Skeleton
    {
        public Bone[] bones;
    }

    [Serializable]
    public class Bone
    {
        public int parent;
        public Matrix4x4 bindpose;
    }
}