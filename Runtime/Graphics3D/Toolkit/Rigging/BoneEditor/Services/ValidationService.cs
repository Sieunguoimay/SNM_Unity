#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// Validation utilities for rig documents: detects unpainted vertices,
    /// overweight vertices, orphaned bones, and can normalize all weights.
    /// </summary>
    public static class ValidationService
    {
        /// <summary>
        /// Returns indices of vertices whose total weight is less than 0.001.
        /// </summary>
        public static List<int> GetUnpaintedVertices(RigDocument doc)
        {
            var result = new List<int>();
            if (doc == null || doc.vertexWeights == null)
                return result;

            for (int i = 0; i < doc.vertexWeights.Length; i++)
            {
                if (doc.vertexWeights[i].TotalWeight < 0.001f)
                    result.Add(i);
            }

            return result;
        }

        /// <summary>
        /// Returns indices of vertices whose total weight exceeds 1.01.
        /// </summary>
        public static List<int> GetOverweightVertices(RigDocument doc)
        {
            var result = new List<int>();
            if (doc == null || doc.vertexWeights == null)
                return result;

            for (int i = 0; i < doc.vertexWeights.Length; i++)
            {
                if (doc.vertexWeights[i].TotalWeight > 1.01f)
                    result.Add(i);
            }

            return result;
        }

        /// <summary>
        /// Returns indices of bones that influence zero vertices (weight > 0.001).
        /// </summary>
        public static List<int> GetOrphanedBones(RigDocument doc)
        {
            var result = new List<int>();
            if (doc == null || doc.bones == null)
                return result;

            for (int b = 0; b < doc.bones.Count; b++)
            {
                if (GetBoneInfluenceCount(doc, b) == 0)
                    result.Add(b);
            }

            return result;
        }

        /// <summary>
        /// Returns how many vertices are influenced by the specified bone (weight > 0.001).
        /// </summary>
        public static int GetBoneInfluenceCount(RigDocument doc, int boneIndex)
        {
            if (doc == null || doc.vertexWeights == null)
                return 0;

            int count = 0;
            for (int v = 0; v < doc.vertexWeights.Length; v++)
            {
                float w = doc.vertexWeights[v].GetWeight(boneIndex);
                if (w > 0.001f)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Normalizes all vertex weights so each vertex sums to exactly 1.0.
        /// Vertices with zero total weight are left untouched.
        /// </summary>
        public static void NormalizeAllWeights(RigDocument doc)
        {
            if (doc == null || doc.vertexWeights == null)
                return;

            UndoHelper.Record(doc, "Normalize All Weights");

            for (int i = 0; i < doc.vertexWeights.Length; i++)
            {
                if (doc.vertexWeights[i].TotalWeight > 1e-6f)
                    doc.vertexWeights[i].Normalize();
            }
        }
    }
}
#endif
