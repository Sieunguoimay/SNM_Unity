using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>Per-frame data shared with the active render tier.</summary>
    public struct GrassFrameContext
    {
        public Camera Camera;
        public Vector3 CameraPosition;
        public Plane[] FrustumPlanes;
        public GrassWorldConfig Config;
    }

    /// <summary>Runtime counters surfaced by the debug overlay / stats panel.</summary>
    public struct GrassStats
    {
        public int TotalInstances;
        public int LoadedChunks;
        public int VisibleChunks;
        public int VisibleInstances;   // exact on Simple tier, async-readback estimate on GPU tier
        public int DrawCalls;
        public long GpuBufferBytes;
        public string TierName;
    }

    /// <summary>
    /// A rendering strategy. Two implementations:
    /// <see cref="GrassGpuDrivenTier"/> (compute cull + indirect draw) and
    /// <see cref="GrassSimpleTier"/> (CPU chunk cull + prefix draw).
    /// Both consume the same chunk buffers and the same shader.
    /// </summary>
    public interface IGrassRenderTier : IDisposable
    {
        string Name { get; }

        /// <summary>Draws all visible chunks for this frame.</summary>
        void Render(List<GrassChunk> visibleChunks, in GrassFrameContext context, ref GrassStats stats);
    }
}
