using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// Every painted blade of a world, stored per chunk. This is the single
    /// source of truth: the brush and volume tools write here in the editor,
    /// the runtime uploads each chunk to the GPU once on load.
    ///
    /// Instances inside a chunk are organized (see <see cref="OrganizeChunk"/>)
    /// as: sorted by type into contiguous ranges, then deterministically
    /// shuffled within each range. The shuffle lets the Simple tier thin
    /// density by drawing a prefix of the range — no rebuild, no popping
    /// pattern artifacts.
    /// </summary>
    [CreateAssetMenu(fileName = "GrassWorldData", menuName = "Snm/Grass System V2/Grass World Data")]
    public class GrassWorldData : ScriptableObject
    {
        [Serializable]
        public struct TypeRange
        {
            public byte typeIndex;
            public int start;
            public int count;
        }

        [Serializable]
        public class ChunkRecord
        {
            public Vector2Int coord;
            public GrassInstance[] instances = Array.Empty<GrassInstance>();
            public TypeRange[] typeRanges = Array.Empty<TypeRange>();
            public float minY;
            public float maxY;
        }

        [Tooltip("Chunk size (meters) this data was painted with. Kept on the data so world and data cannot silently disagree.")]
        public float chunkSize = 16f;

        [Tooltip("Grass types paintable in this world. Instances reference these by index — do not reorder after painting.")]
        public GrassType[] types = Array.Empty<GrassType>();

        [SerializeField] List<ChunkRecord> chunks = new();

        Dictionary<Vector2Int, ChunkRecord> _lookup;

        public IReadOnlyList<ChunkRecord> Chunks => chunks;

        public int TotalInstanceCount
        {
            get
            {
                int total = 0;
                foreach (var chunk in chunks) total += chunk.instances.Length;
                return total;
            }
        }

        public ChunkRecord FindChunk(Vector2Int coord)
        {
            EnsureLookup();
            return _lookup.TryGetValue(coord, out var record) ? record : null;
        }

        public ChunkRecord GetOrCreateChunk(Vector2Int coord)
        {
            var record = FindChunk(coord);
            if (record != null) return record;

            record = new ChunkRecord { coord = coord };
            chunks.Add(record);
            _lookup[coord] = record;
            return record;
        }

        /// <summary>
        /// Adds instances, routing each to its chunk by position. Touched
        /// chunks are re-organized. Editor-time API (brush / volume fill).
        /// </summary>
        public void AddInstances(IReadOnlyList<GrassInstance> newInstances)
        {
            if (newInstances == null || newInstances.Count == 0) return;

            var pending = new Dictionary<Vector2Int, List<GrassInstance>>();
            foreach (var instance in newInstances)
            {
                var coord = GrassGridMath.WorldToChunk(instance.Position, chunkSize);
                if (!pending.TryGetValue(coord, out var list))
                {
                    list = new List<GrassInstance>();
                    pending[coord] = list;
                }
                list.Add(instance);
            }

            foreach (var (coord, list) in pending)
            {
                var record = GetOrCreateChunk(coord);
                var merged = new GrassInstance[record.instances.Length + list.Count];
                record.instances.CopyTo(merged, 0);
                list.CopyTo(merged, record.instances.Length);
                record.instances = merged;
                OrganizeChunk(record);
            }
        }

        /// <summary>
        /// Removes instances within an XZ radius. <paramref name="typeMask"/>
        /// is a bitmask over type indices (-1 = all types). Returns the number
        /// of removed instances. Empty chunks are deleted.
        /// </summary>
        public int RemoveInRadius(Vector3 center, float radius, int typeMask = -1)
        {
            float sqrRadius = radius * radius;
            int removed = 0;

            for (int c = chunks.Count - 1; c >= 0; c--)
            {
                var record = chunks[c];
                if (GrassGridMath.SqrDistanceToChunkXZ(center, record.coord, chunkSize) > sqrRadius) continue;

                var kept = new List<GrassInstance>(record.instances.Length);
                foreach (var instance in record.instances)
                {
                    bool typeMatch = typeMask == -1 || (typeMask & (1 << instance.TypeIndex)) != 0;
                    float dx = instance.Position.x - center.x;
                    float dz = instance.Position.z - center.z;
                    if (typeMatch && dx * dx + dz * dz <= sqrRadius)
                    {
                        removed++;
                        continue;
                    }
                    kept.Add(instance);
                }

                if (kept.Count == record.instances.Length) continue;

                if (kept.Count == 0)
                {
                    chunks.RemoveAt(c);
                    _lookup?.Remove(record.coord);
                    continue;
                }

                record.instances = kept.ToArray();
                OrganizeChunk(record);
            }

            return removed;
        }

        /// <summary>
        /// Sorts a chunk's instances by type into contiguous ranges, shuffles
        /// deterministically within each range (density-prefix trick), and
        /// recomputes the vertical bounds.
        /// </summary>
        public void OrganizeChunk(ChunkRecord record)
        {
            if (record.instances.Length == 0)
            {
                record.typeRanges = Array.Empty<TypeRange>();
                record.minY = record.maxY = 0f;
                return;
            }

            Array.Sort(record.instances, (a, b) => a.TypeIndex.CompareTo(b.TypeIndex));

            var ranges = new List<TypeRange>();
            int rangeStart = 0;
            for (int i = 1; i <= record.instances.Length; i++)
            {
                if (i == record.instances.Length || record.instances[i].TypeIndex != record.instances[rangeStart].TypeIndex)
                {
                    ranges.Add(new TypeRange
                    {
                        typeIndex = record.instances[rangeStart].TypeIndex,
                        start = rangeStart,
                        count = i - rangeStart,
                    });
                    rangeStart = i;
                }
            }
            record.typeRanges = ranges.ToArray();

            // Fisher-Yates within each type range, seeded by chunk coord so the
            // order is stable across sessions and machines.
            var rng = new System.Random(GrassGridMath.ChunkSeed(record.coord));
            foreach (var range in record.typeRanges)
            {
                for (int i = range.count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (record.instances[range.start + i], record.instances[range.start + j]) =
                        (record.instances[range.start + j], record.instances[range.start + i]);
                }
            }

            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var instance in record.instances)
            {
                if (instance.Position.y < minY) minY = instance.Position.y;
                if (instance.Position.y > maxY) maxY = instance.Position.y;
            }
            record.minY = minY;
            record.maxY = maxY;
        }

        /// <summary>Marks per-instance cut flags inside a radius. Returns affected chunk records.</summary>
        public List<ChunkRecord> MarkCut(Vector3 center, float radius)
        {
            float sqrRadius = radius * radius;
            var touched = new List<ChunkRecord>();

            foreach (var record in chunks)
            {
                if (GrassGridMath.SqrDistanceToChunkXZ(center, record.coord, chunkSize) > sqrRadius) continue;

                bool changed = false;
                for (int i = 0; i < record.instances.Length; i++)
                {
                    if (record.instances[i].IsCut) continue;
                    float dx = record.instances[i].Position.x - center.x;
                    float dz = record.instances[i].Position.z - center.z;
                    if (dx * dx + dz * dz > sqrRadius) continue;

                    record.instances[i].IsCut = true;
                    changed = true;
                }

                if (changed) touched.Add(record);
            }

            return touched;
        }

        /// <summary>Clears all cut flags (e.g. on session restart when cuts should not persist).</summary>
        public void ClearCutFlags()
        {
            foreach (var record in chunks)
            {
                for (int i = 0; i < record.instances.Length; i++)
                {
                    record.instances[i].IsCut = false;
                }
            }
        }

        public int IndexOfType(GrassType type)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == type) return i;
            }
            return -1;
        }

        void EnsureLookup()
        {
            if (_lookup != null && _lookup.Count == chunks.Count) return;
            _lookup = new Dictionary<Vector2Int, ChunkRecord>(chunks.Count);
            foreach (var record in chunks) _lookup[record.coord] = record;
        }

        void OnValidate()
        {
            _lookup = null; // serialized data may have changed under us (undo/redo)
        }
    }
}
