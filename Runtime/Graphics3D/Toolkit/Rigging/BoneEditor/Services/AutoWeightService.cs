#if UNITY_EDITOR
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// Distance-based automatic bone weight assignment.
    /// For each vertex, computes distance to each bone segment (head-to-tail),
    /// weights by inverse-square distance, takes top 4 influences, and normalizes.
    /// </summary>
    public static class AutoWeightService
    {
        /// <summary>
        /// Assigns auto-weights for ALL bones at once, replacing existing weights.
        /// </summary>
        public static void AssignAutoWeights(RigDocument doc)
        {
            if (doc == null || doc.sourceMesh == null || doc.bones == null || doc.bones.Count == 0)
                return;

            UndoHelper.Record(doc, "Auto-Weight All Bones");

            var vertices = doc.sourceMesh.vertices;
            int vertexCount = vertices.Length;
            int boneCount = doc.bones.Count;

            // Ensure vertexWeights array is allocated
            if (doc.vertexWeights == null || doc.vertexWeights.Length != vertexCount)
                doc.vertexWeights = new WeightData[vertexCount];

            // Precompute bone world positions
            var bonePositions = new Vector3[boneCount];
            for (int b = 0; b < boneCount; b++)
                bonePositions[b] = doc.GetBoneWorldPosition(b);

            for (int v = 0; v < vertexCount; v++)
            {
                var vertPos = vertices[v];

                // Compute inverse-square distance weight to each bone segment
                var candidates = new (int boneIndex, float weight)[boneCount];
                int candidateCount = 0;

                for (int b = 0; b < boneCount; b++)
                {
                    var head = bonePositions[b];
                    int parentIdx = doc.bones[b].parentIndex;
                    var tail = parentIdx >= 0 ? bonePositions[parentIdx] : head;

                    float dist = DistanceToSegment(vertPos, head, tail);
                    float minDist = Mathf.Max(dist, 0.0001f);
                    float w = 1f / (minDist * minDist);

                    candidates[candidateCount++] = (b, w);
                }

                // Sort by weight descending and take top 4
                System.Array.Sort(candidates, 0, candidateCount,
                    System.Collections.Generic.Comparer<(int, float)>.Create((a, b) => b.Item2.CompareTo(a.Item2)));

                int influenceCount = Mathf.Min(candidateCount, 4);
                var wd = new WeightData();
                wd.influences = new BoneWeightPair[influenceCount];

                float total = 0f;
                for (int i = 0; i < influenceCount; i++)
                    total += candidates[i].weight;

                if (total < 1e-6f) total = 1f;

                for (int i = 0; i < influenceCount; i++)
                {
                    wd.influences[i] = new BoneWeightPair
                    {
                        boneIndex = candidates[i].boneIndex,
                        weight = candidates[i].weight / total
                    };
                }

                doc.vertexWeights[v] = wd;
            }
        }

        /// <summary>
        /// Assigns auto-weights for a single bone, blending it into existing weights.
        /// Recalculates only the influence of the specified bone on each vertex,
        /// then re-normalizes.
        /// </summary>
        public static void AssignAutoWeightsForBone(RigDocument doc, int boneIndex)
        {
            if (doc == null || doc.sourceMesh == null || doc.bones == null)
                return;
            if (boneIndex < 0 || boneIndex >= doc.bones.Count)
                return;

            UndoHelper.Record(doc, "Auto-Weight Bone: " + doc.bones[boneIndex].name);

            var vertices = doc.sourceMesh.vertices;
            int vertexCount = vertices.Length;
            int boneCount = doc.bones.Count;

            if (doc.vertexWeights == null || doc.vertexWeights.Length != vertexCount)
                doc.vertexWeights = new WeightData[vertexCount];

            // Precompute bone positions
            var bonePositions = new Vector3[boneCount];
            for (int b = 0; b < boneCount; b++)
                bonePositions[b] = doc.GetBoneWorldPosition(b);

            var head = bonePositions[boneIndex];
            int parentIdx = doc.bones[boneIndex].parentIndex;
            var tail = parentIdx >= 0 ? bonePositions[parentIdx] : head;

            for (int v = 0; v < vertexCount; v++)
            {
                var vertPos = vertices[v];
                float dist = DistanceToSegment(vertPos, head, tail);
                float minDist = Mathf.Max(dist, 0.0001f);
                float w = 1f / (minDist * minDist);

                // Compute a reference total so the bone gets a proportional weight
                float refTotal = 0f;
                for (int b = 0; b < boneCount; b++)
                {
                    var bHead = bonePositions[b];
                    int pIdx = doc.bones[b].parentIndex;
                    var bTail = pIdx >= 0 ? bonePositions[pIdx] : bHead;
                    float d = DistanceToSegment(vertPos, bHead, bTail);
                    float md = Mathf.Max(d, 0.0001f);
                    refTotal += 1f / (md * md);
                }

                float normalizedWeight = refTotal > 1e-6f ? w / refTotal : 0f;

                doc.vertexWeights[v].SetWeight(boneIndex, normalizedWeight);
                doc.vertexWeights[v].Normalize();
            }
        }

        /// <summary>
        /// Computes the minimum distance from point p to line segment (a, b).
        /// </summary>
        private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            float sqrLen = ab.sqrMagnitude;

            // Degenerate segment (head == tail): distance to point
            if (sqrLen < 1e-8f)
                return Vector3.Distance(p, a);

            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / sqrLen);
            var projection = a + t * ab;
            return Vector3.Distance(p, projection);
        }
    }
}
#endif
