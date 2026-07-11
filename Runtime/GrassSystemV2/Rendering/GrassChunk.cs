using System;
using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// Runtime state of one loaded chunk: the persistent GPU instance buffer
    /// plus cached culling info. Plain class, not a GameObject — chunks are
    /// owned and updated by <see cref="GrassWorld"/>.
    ///
    /// The instance buffer is uploaded once on load and only re-uploaded when
    /// gameplay mutates instances (cutting). Per-frame visibility never
    /// touches it.
    /// </summary>
    public sealed class GrassChunk : IDisposable
    {
        public readonly Vector2Int Coord;
        public readonly GrassWorldData.ChunkRecord Record;
        public readonly Bounds WorldBounds;

        /// <summary>StructuredBuffer of <see cref="GrassInstance"/>, stride 20 bytes.</summary>
        public GraphicsBuffer InstanceBuffer { get; private set; }

        /// <summary>Updated every frame by the world's chunk pass.</summary>
        public float DistanceToCamera;
        public bool IsVisible;

        public int InstanceCount => Record.instances.Length;

        public GrassChunk(GrassWorldData.ChunkRecord record, float chunkSize, float boundsPadding)
        {
            Record = record;
            Coord = record.coord;
            WorldBounds = GrassGridMath.ChunkBounds(record.coord, chunkSize, record.minY, record.maxY, boundsPadding);
        }

        public void Upload()
        {
            if (InstanceBuffer != null || Record.instances.Length == 0) return;

            InstanceBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                Record.instances.Length,
                GrassInstance.Stride);
            InstanceBuffer.SetData(Record.instances);
        }

        /// <summary>Re-uploads instance data after a CPU-side mutation (cut flags).</summary>
        public void Refresh()
        {
            InstanceBuffer?.SetData(Record.instances);
        }

        public void Dispose()
        {
            InstanceBuffer?.Dispose();
            InstanceBuffer = null;
        }
    }
}
