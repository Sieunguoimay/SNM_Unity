#if UNITY_EDITOR
using Snm.Graphics3D.GPUSkinning;
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// Export utilities: creates/updates SkeletonAssets, writes bone weights to meshes,
    /// builds and saves skinned prefabs, and provides a one-click export pipeline.
    /// </summary>
    public static class ExportService
    {
        /// <summary>
        /// Creates or updates a SkeletonAsset from the bones in the RigDocument.
        /// </summary>
        /// <param name="doc">Source rig document.</param>
        /// <param name="existing">Optional existing asset to update in-place. If null, a new asset is created.</param>
        /// <returns>The created or updated SkeletonAsset.</returns>
        public static SkeletonAsset ExportSkeletonAsset(RigDocument doc, SkeletonAsset existing = null)
        {
            if (doc == null || doc.bones == null || doc.bones.Count == 0)
            {
                Debug.LogError("[ExportService] Cannot export SkeletonAsset: no bones in document.");
                return null;
            }

            int boneCount = doc.bones.Count;
            var bones = new Bone[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                bones[i] = new Bone
                {
                    parent = doc.bones[i].parentIndex,
                    bindpose = doc.bones[i].bindpose
                };
            }

            var skeleton = new Skeleton { bones = bones };

            if (existing != null)
            {
                Undo.RecordObject(existing, "Update SkeletonAsset");
                existing.skeleton = skeleton;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                Debug.Log($"[ExportService] Updated SkeletonAsset: {AssetDatabase.GetAssetPath(existing)}");
                return existing;
            }

            // Create new asset
            var asset = ScriptableObject.CreateInstance<SkeletonAsset>();
            asset.skeleton = skeleton;

            string meshPath = doc.sourceMesh != null ? AssetDatabase.GetAssetPath(doc.sourceMesh) : "";
            string defaultDir = !string.IsNullOrEmpty(meshPath)
                ? System.IO.Path.GetDirectoryName(meshPath)
                : "Assets";
            string defaultName = doc.sourceMesh != null ? doc.sourceMesh.name + "_Skeleton" : "NewSkeleton";

            string path = EditorUtility.SaveFilePanel(
                "Save Skeleton Asset",
                defaultDir,
                defaultName,
                "asset");

            if (string.IsNullOrEmpty(path))
            {
                Object.DestroyImmediate(asset);
                return null;
            }

            // Convert to relative path
            path = FileUtil.GetProjectRelativePath(path);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[ExportService] Path must be inside the Assets folder.");
                Object.DestroyImmediate(asset);
                return null;
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ExportService] Created SkeletonAsset at: {path}");
            return asset;
        }

        /// <summary>
        /// Writes bone weights and bindposes directly onto the source mesh asset.
        /// </summary>
        public static void ExportMeshWeights(RigDocument doc)
        {
            if (doc == null || doc.sourceMesh == null)
            {
                Debug.LogError("[ExportService] Cannot export mesh weights: no source mesh.");
                return;
            }

            if (doc.bones == null || doc.bones.Count == 0)
            {
                Debug.LogError("[ExportService] Cannot export mesh weights: no bones.");
                return;
            }

            var mesh = doc.sourceMesh;
            int vertexCount = mesh.vertexCount;
            int boneCount = doc.bones.Count;

            // Write bone weights
            var boneWeights = new BoneWeight[vertexCount];
            if (doc.vertexWeights != null)
            {
                for (int v = 0; v < vertexCount && v < doc.vertexWeights.Length; v++)
                    boneWeights[v] = doc.vertexWeights[v].ToBoneWeight();
            }
            mesh.boneWeights = boneWeights;

            // Write bindposes
            var bindposes = new Matrix4x4[boneCount];
            for (int i = 0; i < boneCount; i++)
                bindposes[i] = doc.bones[i].bindpose;
            mesh.bindposes = bindposes;

            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ExportService] Exported weights and bindposes to mesh: {mesh.name}");
        }

        /// <summary>
        /// Builds a skinned prefab and saves it to disk.
        /// </summary>
        /// <returns>Asset path of the saved prefab, or null on failure/cancellation.</returns>
        public static string BuildAndSavePrefab(RigDocument doc, Material material)
        {
            var root = PrefabBuilderService.BuildSkinnedPrefab(doc, material);
            if (root == null)
                return null;

            string meshPath = doc.sourceMesh != null ? AssetDatabase.GetAssetPath(doc.sourceMesh) : "";
            string defaultDir = !string.IsNullOrEmpty(meshPath)
                ? System.IO.Path.GetDirectoryName(meshPath)
                : "Assets";
            string defaultName = doc.sourceMesh != null ? doc.sourceMesh.name + "_Skinned" : "SkinnedPrefab";

            string path = EditorUtility.SaveFilePanel(
                "Save Skinned Prefab",
                defaultDir,
                defaultName,
                "prefab");

            if (string.IsNullOrEmpty(path))
            {
                Object.DestroyImmediate(root);
                return null;
            }

            path = FileUtil.GetProjectRelativePath(path);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[ExportService] Path must be inside the Assets folder.");
                Object.DestroyImmediate(root);
                return null;
            }

            // Save the cloned mesh as a sub-asset of the prefab
            var smr = root.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                string meshAssetPath = path.Replace(".prefab", "_Mesh.asset");
                AssetDatabase.CreateAsset(smr.sharedMesh, meshAssetPath);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ExportService] Saved skinned prefab at: {path}");
            return path;
        }

        /// <summary>
        /// One-click pipeline: export mesh weights, build and save prefab, log success.
        /// </summary>
        public static void OneClickPipeline(RigDocument doc, Material material)
        {
            // Step 1: Export mesh weights
            ExportMeshWeights(doc);

            // Step 2: Build and save prefab
            string prefabPath = BuildAndSavePrefab(doc, material);

            if (!string.IsNullOrEmpty(prefabPath))
                Debug.Log($"[ExportService] One-click pipeline complete. Prefab saved at: {prefabPath}");
            else
                Debug.LogWarning("[ExportService] One-click pipeline: prefab save was cancelled or failed.");
        }
    }
}
#endif
