#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// A single bone-index + weight pair used in per-vertex skinning data.
    /// </summary>
    [Serializable]
    public struct BoneWeightPair
    {
        public int boneIndex;
        public float weight;
    }

    /// <summary>
    /// Per-vertex weight data holding up to 4 bone influences, sorted by weight descending.
    /// </summary>
    [Serializable]
    public struct WeightData
    {
        public BoneWeightPair[] influences;

        /// <summary>
        /// Sum of all influence weights.
        /// </summary>
        public float TotalWeight
        {
            get
            {
                if (influences == null) return 0f;
                float total = 0f;
                for (int i = 0; i < influences.Length; i++)
                    total += influences[i].weight;
                return total;
            }
        }

        /// <summary>
        /// Sets the weight for the given bone index. If the bone already exists in the
        /// influences array its weight is updated; otherwise it is inserted (replacing
        /// the smallest weight if 4 slots are already used). A weight of zero removes the entry.
        /// </summary>
        public void SetWeight(int boneIndex, float weight)
        {
            EnsureInfluences();

            // Find existing entry for this bone
            int existingSlot = -1;
            for (int i = 0; i < influences.Length; i++)
            {
                if (influences[i].boneIndex == boneIndex && influences[i].weight > 0f)
                {
                    existingSlot = i;
                    break;
                }
            }

            if (existingSlot >= 0)
            {
                if (weight <= 0f)
                {
                    // Remove: shift entries down
                    influences[existingSlot] = new BoneWeightPair { boneIndex = 0, weight = 0f };
                    SortAndCompact();
                }
                else
                {
                    influences[existingSlot].weight = weight;
                    SortAndCompact();
                }
                return;
            }

            // Not present yet — need to add
            if (weight <= 0f) return;

            // Find a free slot (weight == 0)
            for (int i = 0; i < influences.Length; i++)
            {
                if (influences[i].weight <= 0f)
                {
                    influences[i] = new BoneWeightPair { boneIndex = boneIndex, weight = weight };
                    SortAndCompact();
                    return;
                }
            }

            // All 4 slots occupied — replace the smallest if new weight is larger
            int minIdx = 0;
            for (int i = 1; i < influences.Length; i++)
            {
                if (influences[i].weight < influences[minIdx].weight)
                    minIdx = i;
            }

            if (weight > influences[minIdx].weight)
            {
                influences[minIdx] = new BoneWeightPair { boneIndex = boneIndex, weight = weight };
                SortAndCompact();
            }
        }

        /// <summary>
        /// Returns the weight of the specified bone index, or 0 if not present.
        /// </summary>
        public float GetWeight(int boneIndex)
        {
            if (influences == null) return 0f;
            for (int i = 0; i < influences.Length; i++)
            {
                if (influences[i].boneIndex == boneIndex && influences[i].weight > 0f)
                    return influences[i].weight;
            }
            return 0f;
        }

        /// <summary>
        /// Normalizes all influence weights so they sum to 1.0.
        /// </summary>
        public void Normalize()
        {
            if (influences == null) return;
            float total = TotalWeight;
            if (total < 1e-6f) return;

            for (int i = 0; i < influences.Length; i++)
                influences[i].weight /= total;
        }

        /// <summary>
        /// Converts to Unity's BoneWeight struct (4 bone indices + 4 weights).
        /// </summary>
        public BoneWeight ToBoneWeight()
        {
            EnsureInfluences();
            SortAndCompact();

            var bw = new BoneWeight();
            if (influences.Length > 0) { bw.boneIndex0 = influences[0].boneIndex; bw.weight0 = influences[0].weight; }
            if (influences.Length > 1) { bw.boneIndex1 = influences[1].boneIndex; bw.weight1 = influences[1].weight; }
            if (influences.Length > 2) { bw.boneIndex2 = influences[2].boneIndex; bw.weight2 = influences[2].weight; }
            if (influences.Length > 3) { bw.boneIndex3 = influences[3].boneIndex; bw.weight3 = influences[3].weight; }
            return bw;
        }

        private void EnsureInfluences()
        {
            if (influences == null || influences.Length < 4)
            {
                var old = influences;
                influences = new BoneWeightPair[4];
                if (old != null)
                {
                    for (int i = 0; i < old.Length && i < 4; i++)
                        influences[i] = old[i];
                }
            }
        }

        /// <summary>
        /// Sorts influences by weight descending and zeros out entries beyond the top 4.
        /// </summary>
        private void SortAndCompact()
        {
            if (influences == null) return;

            // Simple insertion sort (max 4 elements)
            for (int i = 1; i < influences.Length; i++)
            {
                var key = influences[i];
                int j = i - 1;
                while (j >= 0 && influences[j].weight < key.weight)
                {
                    influences[j + 1] = influences[j];
                    j--;
                }
                influences[j + 1] = key;
            }
        }
    }
}
#endif
