#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneToolV2
{
    /// <summary>
    /// Builds a SkinnedMeshRenderer prefab from a RigDocument.
    /// Creates a GameObject hierarchy mirroring the bone tree, clones the mesh
    /// with baked weights and bindposes, and wires up the SkinnedMeshRenderer.
    /// </summary>
    public static class PrefabBuilderService
    {
        /// <summary>
        /// Creates a fully configured skinned prefab in memory (not yet saved to disk).
        /// </summary>
        /// <param name="doc">The rig document containing mesh, bones, and weights.</param>
        /// <param name="material">Material to assign to the SkinnedMeshRenderer.</param>
        /// <returns>Root GameObject of the prefab hierarchy, or null on failure.</returns>
        public static GameObject BuildSkinnedPrefab(RigDocument doc, Material material)
        {
            if (doc == null || doc.sourceMesh == null || doc.bones == null || doc.bones.Count == 0)
            {
                Debug.LogError("[PrefabBuilder] Invalid RigDocument: missing mesh or bones.");
                return null;
            }

            int boneCount = doc.bones.Count;
            string meshName = doc.sourceMesh.name;
            if (string.IsNullOrEmpty(meshName))
                meshName = "SkinnedMesh";

            // Create root GameObject
            var root = new GameObject(meshName);

            // Create bone GameObjects and parent them according to BoneData.parentIndex
            var boneTransforms = new Transform[boneCount];
            int firstRootBone = -1;

            for (int i = 0; i < boneCount; i++)
            {
                var boneData = doc.bones[i];
                var boneGO = new GameObject(boneData.name);
                boneTransforms[i] = boneGO.transform;

                if (boneData.parentIndex >= 0 && boneData.parentIndex < boneCount)
                {
                    boneGO.transform.SetParent(boneTransforms[boneData.parentIndex], false);
                }
                else
                {
                    // Root bone: parent to the root GameObject
                    boneGO.transform.SetParent(root.transform, false);
                    if (firstRootBone < 0)
                        firstRootBone = i;
                }

                // Set bone position from bindpose.inverse
                // bindpose maps world -> bone local, so inverse gives bone world transform
                var worldMatrix = boneData.bindpose.inverse;
                var worldPos = (Vector3)worldMatrix.GetColumn(3);
                var worldRot = worldMatrix.rotation;
                var worldScale = worldMatrix.lossyScale;

                // Set as local transform relative to parent
                if (boneData.parentIndex >= 0 && boneData.parentIndex < boneCount)
                {
                    // Compute local transform from parent
                    var parentWorld = doc.bones[boneData.parentIndex].bindpose.inverse;
                    var localMatrix = parentWorld.inverse * worldMatrix;
                    boneGO.transform.localPosition = (Vector3)localMatrix.GetColumn(3);
                    boneGO.transform.localRotation = localMatrix.rotation;
                    boneGO.transform.localScale = localMatrix.lossyScale;
                }
                else
                {
                    boneGO.transform.localPosition = worldPos;
                    boneGO.transform.localRotation = worldRot;
                    boneGO.transform.localScale = worldScale;
                }
            }

            // Clone mesh and bake weights + bindposes
            var skinnedMesh = Object.Instantiate(doc.sourceMesh);
            skinnedMesh.name = meshName + "_Skinned";

            // Bake bone weights
            int vertexCount = skinnedMesh.vertexCount;
            var boneWeights = new BoneWeight[vertexCount];
            if (doc.vertexWeights != null)
            {
                for (int v = 0; v < vertexCount && v < doc.vertexWeights.Length; v++)
                    boneWeights[v] = doc.vertexWeights[v].ToBoneWeight();
            }
            skinnedMesh.boneWeights = boneWeights;

            // Bake bindposes
            var bindposes = new Matrix4x4[boneCount];
            for (int i = 0; i < boneCount; i++)
                bindposes[i] = doc.bones[i].bindpose;
            skinnedMesh.bindposes = bindposes;

            // Add SkinnedMeshRenderer to root
            var smr = root.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = skinnedMesh;
            smr.bones = boneTransforms;
            smr.rootBone = firstRootBone >= 0 ? boneTransforms[firstRootBone] : boneTransforms[0];

            if (material != null)
                smr.sharedMaterial = material;

            // Compute bounds
            smr.localBounds = skinnedMesh.bounds;

            return root;
        }
    }
}
#endif
