#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// Mirror utilities for bone weights. Supports mirroring weights across an axis
    /// using name conventions (_L/_R, Left/Right, .L/.R) and spatial vertex matching.
    /// </summary>
    public static class WeightMirrorService
    {
        private static readonly (string left, string right)[] SuffixPairs = new[]
        {
            ("_L", "_R"),
            (".L", ".R"),
            ("_Left", "_Right"),
            ("Left", "Right"),
            ("_left", "_right"),
            ("left", "right"),
            ("_l", "_r"),
            (".l", ".r"),
        };

        /// <summary>
        /// For bones with a left/right suffix, creates or finds the mirrored counterpart bone.
        /// New bones are created with a mirrored bindpose (reflected across X axis).
        /// </summary>
        public static void MirrorBoneNames(RigDocument doc)
        {
            if (doc == null || doc.bones == null)
                return;

            UndoHelper.Record(doc, "Mirror Bone Names");

            int originalCount = doc.bones.Count;

            for (int i = 0; i < originalCount; i++)
            {
                string name = doc.bones[i].name;
                string mirroredName = GetMirroredName(name);

                if (mirroredName == null)
                    continue; // Not a lateralized bone

                // Check if mirror bone already exists
                bool exists = false;
                for (int j = 0; j < doc.bones.Count; j++)
                {
                    if (doc.bones[j].name == mirroredName)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                    continue;

                // Create mirrored bone with reflected bindpose
                var sourceBone = doc.bones[i];
                var mirroredBindpose = MirrorBindposeX(sourceBone.bindpose);

                // Mirror the parent index: if parent is also a lateralized bone, find its mirror
                int mirroredParent = sourceBone.parentIndex;
                if (mirroredParent >= 0)
                {
                    string parentMirrorName = GetMirroredName(doc.bones[mirroredParent].name);
                    if (parentMirrorName != null)
                    {
                        for (int j = 0; j < doc.bones.Count; j++)
                        {
                            if (doc.bones[j].name == parentMirrorName)
                            {
                                mirroredParent = j;
                                break;
                            }
                        }
                    }
                }

                var newBone = new BoneData
                {
                    name = mirroredName,
                    parentIndex = mirroredParent,
                    bindpose = mirroredBindpose,
                    displayColor = new Color(
                        1f - sourceBone.displayColor.r,
                        sourceBone.displayColor.g,
                        sourceBone.displayColor.b,
                        sourceBone.displayColor.a)
                };

                doc.AddBone(newBone);
            }
        }

        /// <summary>
        /// Mirrors weights from a source bone to its mirror counterpart.
        /// For each vertex influenced by the source bone on one side of the mirror axis,
        /// finds the nearest vertex on the opposite side and copies the remapped weight.
        /// </summary>
        /// <param name="doc">The rig document.</param>
        /// <param name="sourceBoneIndex">Index of the bone whose weights to mirror.</param>
        /// <param name="mirrorAxis">Axis to mirror across (0=X, 1=Y, 2=Z). Default: 0 (X).</param>
        /// <param name="tolerance">Maximum distance for vertex matching. Default: 0.001.</param>
        public static void MirrorWeights(RigDocument doc, int sourceBoneIndex,
            int mirrorAxis = 0, float tolerance = 0.001f)
        {
            if (doc == null || doc.sourceMesh == null || doc.bones == null || doc.vertexWeights == null)
                return;

            if (sourceBoneIndex < 0 || sourceBoneIndex >= doc.bones.Count)
                return;

            int mirrorBoneIndex = FindMirrorBone(doc, sourceBoneIndex);
            if (mirrorBoneIndex < 0)
            {
                Debug.LogWarning($"[WeightMirror] No mirror bone found for '{doc.bones[sourceBoneIndex].name}'.");
                return;
            }

            UndoHelper.Record(doc, $"Mirror Weights: {doc.bones[sourceBoneIndex].name} -> {doc.bones[mirrorBoneIndex].name}");

            var vertices = doc.sourceMesh.vertices;
            int vertexCount = vertices.Length;

            // Build spatial lookup for the negative side
            // For each vertex, compute its mirrored position and find the nearest match
            var mirroredVertexMap = BuildMirrorVertexMap(vertices, mirrorAxis, tolerance);

            for (int v = 0; v < vertexCount; v++)
            {
                // Only process vertices on the positive side of the mirror axis
                float axisValue = GetAxisValue(vertices[v], mirrorAxis);
                if (axisValue < -tolerance * 0.5f)
                    continue;

                float sourceWeight = doc.vertexWeights[v].GetWeight(sourceBoneIndex);
                if (sourceWeight < 1e-6f)
                    continue;

                // Find mirror vertex
                if (!mirroredVertexMap.TryGetValue(v, out int mirrorVertex))
                    continue;

                // Set the mirror bone's weight on the mirror vertex
                doc.vertexWeights[mirrorVertex].SetWeight(mirrorBoneIndex, sourceWeight);
                doc.vertexWeights[mirrorVertex].Normalize();
            }
        }

        /// <summary>
        /// Finds the mirror bone for a given bone by name convention.
        /// Supports _L/_R, .L/.R, Left/Right, _left/_right naming patterns.
        /// </summary>
        /// <returns>Index of the mirror bone, or -1 if not found.</returns>
        public static int FindMirrorBone(RigDocument doc, int boneIndex)
        {
            if (doc == null || doc.bones == null || boneIndex < 0 || boneIndex >= doc.bones.Count)
                return -1;

            string mirroredName = GetMirroredName(doc.bones[boneIndex].name);
            if (mirroredName == null)
                return -1;

            for (int i = 0; i < doc.bones.Count; i++)
            {
                if (doc.bones[i].name == mirroredName)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns the mirrored name by swapping left/right suffixes, or null if not a lateralized name.
        /// </summary>
        private static string GetMirroredName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            foreach (var (left, right) in SuffixPairs)
            {
                if (name.EndsWith(left))
                    return name.Substring(0, name.Length - left.Length) + right;
                if (name.EndsWith(right))
                    return name.Substring(0, name.Length - right.Length) + left;
            }

            // Also check for prefix patterns
            foreach (var (left, right) in SuffixPairs)
            {
                if (name.StartsWith(left))
                    return right + name.Substring(left.Length);
                if (name.StartsWith(right))
                    return left + name.Substring(right.Length);
            }

            return null;
        }

        /// <summary>
        /// Builds a map from each vertex on the positive side to its nearest mirror vertex
        /// on the negative side.
        /// </summary>
        private static Dictionary<int, int> BuildMirrorVertexMap(
            Vector3[] vertices, int mirrorAxis, float tolerance)
        {
            var map = new Dictionary<int, int>();
            float toleranceSq = tolerance * tolerance;

            // Separate vertices into positive and negative sides
            var negativeSide = new List<int>();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (GetAxisValue(vertices[i], mirrorAxis) < tolerance * 0.5f)
                    negativeSide.Add(i);
            }

            for (int v = 0; v < vertices.Length; v++)
            {
                float axisValue = GetAxisValue(vertices[v], mirrorAxis);
                if (axisValue < -tolerance * 0.5f)
                    continue;

                // Compute mirrored position
                var mirroredPos = MirrorPosition(vertices[v], mirrorAxis);

                // Find nearest vertex on negative side
                float bestDistSq = float.MaxValue;
                int bestIdx = -1;

                for (int n = 0; n < negativeSide.Count; n++)
                {
                    int negIdx = negativeSide[n];
                    float distSq = (vertices[negIdx] - mirroredPos).sqrMagnitude;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestIdx = negIdx;
                    }
                }

                // Also check center vertices (within tolerance of axis = 0) mapping to themselves
                if (Mathf.Abs(axisValue) < tolerance * 0.5f)
                {
                    // Vertex on the center line maps to itself
                    map[v] = v;
                    continue;
                }

                if (bestIdx >= 0 && bestDistSq <= toleranceSq)
                    map[v] = bestIdx;
            }

            return map;
        }

        private static Vector3 MirrorPosition(Vector3 pos, int axis)
        {
            var result = pos;
            result[axis] = -result[axis];
            return result;
        }

        private static float GetAxisValue(Vector3 v, int axis)
        {
            return v[axis];
        }

        /// <summary>
        /// Mirrors a bindpose matrix across the X axis.
        /// Reflects the translation and rotation components.
        /// </summary>
        private static Matrix4x4 MirrorBindposeX(Matrix4x4 bindpose)
        {
            // Mirror matrix: reflects X axis
            var mirrorMatrix = Matrix4x4.Scale(new Vector3(-1, 1, 1));

            // For bindpose (world-to-bone): mirrored = bindpose * mirror
            // But we need to handle the full transformation:
            // mirroredBindpose = mirror * bindpose * mirror
            return mirrorMatrix * bindpose * mirrorMatrix;
        }
    }
}
#endif
