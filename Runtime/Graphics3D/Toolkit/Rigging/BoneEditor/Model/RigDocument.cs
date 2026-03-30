#if UNITY_EDITOR
using System.Collections.Generic;
using Snm.Graphics3D.GPUSkinning;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// The central data model for the bone tool. Lives in memory only (never saved to disk).
    /// Created via ScriptableObject.CreateInstance. All mutations should go through
    /// UndoHelper.Record for full undo/redo support.
    /// </summary>
    public class RigDocument : ScriptableObject
    {
        public enum ToolModeEnum
        {
            Skeleton = 0,
            Paint = 1,
            Test = 2
        }

        public Mesh sourceMesh;
        public SkeletonAsset sourceSkeletonAsset;
        public GameObject sourcePrefab; // the prefab we imported from (null if starting from scratch)
        public List<BoneData> bones = new List<BoneData>();
        public WeightData[] vertexWeights;
        public ToolModeEnum activeMode = ToolModeEnum.Skeleton;
        public int selectedBoneIndex = -1;

        private static readonly Color[] BoneColorPalette =
        {
            new Color(0.2f, 0.8f, 0.8f), // cyan
            new Color(0.8f, 0.4f, 0.2f), // orange
            new Color(0.3f, 0.8f, 0.3f), // green
            new Color(0.8f, 0.3f, 0.6f), // pink
            new Color(0.5f, 0.5f, 0.9f), // blue
            new Color(0.9f, 0.9f, 0.3f), // yellow
            new Color(0.6f, 0.3f, 0.9f), // purple
            new Color(0.9f, 0.5f, 0.5f), // red
        };

        /// <summary>
        /// Adds a new bone. Returns the index of the new bone.
        /// </summary>
        public int AddBone(string boneName, int parentIndex, Vector3 worldPosition)
        {
            UndoHelper.Record(this, "Add Bone: " + boneName);

            var bindpose = Matrix4x4.TRS(worldPosition, Quaternion.identity, Vector3.one).inverse;
            var bone = new BoneData
            {
                name = boneName,
                parentIndex = parentIndex,
                bindpose = bindpose,
                displayColor = BoneColorPalette[bones.Count % BoneColorPalette.Length]
            };

            bones.Add(bone);
            int newIndex = bones.Count - 1;

            // Initialize vertex weights if mesh is assigned
            EnsureVertexWeights();

            return newIndex;
        }

        /// <summary>
        /// Adds a pre-constructed BoneData. Returns the index of the new bone.
        /// </summary>
        public int AddBone(BoneData bone)
        {
            UndoHelper.Record(this, "Add Bone: " + bone.name);
            bones.Add(bone);
            EnsureVertexWeights();
            return bones.Count - 1;
        }

        /// <summary>
        /// Removes a bone at the given index. Children are reparented to the removed bone's parent.
        /// Vertex weight bone indices are remapped accordingly.
        /// </summary>
        public void RemoveBone(int index)
        {
            if (index < 0 || index >= bones.Count) return;

            UndoHelper.Record(this, "Remove Bone: " + bones[index].name);

            int removedParent = bones[index].parentIndex;

            // Reparent children of the removed bone
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].parentIndex == index)
                    bones[i].parentIndex = removedParent;
            }

            bones.RemoveAt(index);

            // Fix parent indices that pointed above the removed index
            for (int i = 0; i < bones.Count; i++)
            {
                if (bones[i].parentIndex > index)
                    bones[i].parentIndex--;
                else if (bones[i].parentIndex == index)
                    bones[i].parentIndex = -1; // should not happen after reparent, but safety
            }

            // Remap vertex weights
            if (vertexWeights != null)
            {
                for (int v = 0; v < vertexWeights.Length; v++)
                {
                    if (vertexWeights[v].influences == null) continue;
                    for (int w = 0; w < vertexWeights[v].influences.Length; w++)
                    {
                        ref var pair = ref vertexWeights[v].influences[w];
                        if (pair.boneIndex == index)
                        {
                            pair.boneIndex = 0;
                            pair.weight = 0f;
                        }
                        else if (pair.boneIndex > index)
                        {
                            pair.boneIndex--;
                        }
                    }
                }
            }

            // Fix selection
            if (selectedBoneIndex == index)
                selectedBoneIndex = -1;
            else if (selectedBoneIndex > index)
                selectedBoneIndex--;
        }

        /// <summary>
        /// Returns the world-space position of the bone derived from its bindpose.
        /// bindpose is world-to-bone, so inverse gives bone-to-world.
        /// </summary>
        public Vector3 GetBoneWorldPosition(int index)
        {
            if (index < 0 || index >= bones.Count) return Vector3.zero;
            var boneToWorld = bones[index].bindpose.inverse;
            return (Vector3)boneToWorld.GetColumn(3);
        }

        /// <summary>
        /// Sets the bone world position by updating its bindpose.
        /// Preserves the rotation and scale of the existing bindpose.
        /// </summary>
        public void SetBoneWorldPosition(int index, Vector3 worldPosition)
        {
            if (index < 0 || index >= bones.Count) return;

            var oldInverse = bones[index].bindpose.inverse;
            var rotation = oldInverse.rotation;
            var scale = oldInverse.lossyScale;

            var newBoneToWorld = Matrix4x4.TRS(worldPosition, rotation, scale);
            bones[index].bindpose = newBoneToWorld.inverse;
        }

        /// <summary>
        /// Loads bone data from a SkeletonAsset into this document.
        /// </summary>
        public void LoadFromSkeletonAsset(SkeletonAsset asset)
        {
            if (asset == null || asset.skeleton == null || asset.skeleton.bones == null) return;

            UndoHelper.Record(this, "Load Skeleton");

            bones.Clear();
            var srcBones = asset.skeleton.bones;

            for (int i = 0; i < srcBones.Length; i++)
            {
                var src = srcBones[i];
                bones.Add(new BoneData
                {
                    name = !string.IsNullOrEmpty(src.name) ? src.name : "Bone_" + i,
                    parentIndex = src.parent,
                    bindpose = src.bindpose,
                    displayColor = BoneColorPalette[i % BoneColorPalette.Length]
                });
            }

            selectedBoneIndex = -1;
            EnsureVertexWeights();
        }

        /// <summary>
        /// Ensures the vertexWeights array matches the source mesh vertex count.
        /// </summary>
        public void EnsureVertexWeights()
        {
            if (sourceMesh == null) return;
            int vertCount = sourceMesh.vertexCount;
            if (vertexWeights == null || vertexWeights.Length != vertCount)
                vertexWeights = new WeightData[vertCount];
        }

        /// <summary>
        /// Returns the count of unpainted vertices (total weight less than 0.001).
        /// </summary>
        public int GetUnpaintedVertexCount()
        {
            if (vertexWeights == null) return 0;
            int count = 0;
            for (int i = 0; i < vertexWeights.Length; i++)
            {
                if (vertexWeights[i].TotalWeight < 0.001f)
                    count++;
            }
            return count;
        }
    }
}
#endif
