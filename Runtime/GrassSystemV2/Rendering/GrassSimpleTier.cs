using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// The safe-everywhere tier: no compute shaders. CPU culls at chunk
    /// granularity only; per-instance work is avoided entirely by the
    /// density-prefix trick — instances in each chunk's type range are
    /// pre-shuffled at bake time, so drawing the first N of the range thins
    /// density smoothly with distance. LOD switches per chunk.
    ///
    /// Cost model: one draw per (visible chunk, type range). Instance buffers
    /// never change after upload; only the instance count per draw varies.
    /// </summary>
    public sealed class GrassSimpleTier : IGrassRenderTier
    {
        public string Name => "Simple";

        readonly GrassWorldData _data;
        readonly GrassTypeMaterials _materials;
        readonly MaterialPropertyBlock _propertyBlock = new();

        public GrassSimpleTier(GrassWorldData data, GrassTypeMaterials materials)
        {
            _data = data;
            _materials = materials;
        }

        public void Render(List<GrassChunk> visibleChunks, in GrassFrameContext context, ref GrassStats stats)
        {
            var config = context.Config;

            foreach (var chunk in visibleChunks)
            {
                if (chunk.InstanceBuffer == null) continue;

                float density = GrassGridMath.DensityFactor(
                    chunk.DistanceToCamera, config.densityFalloffStart, config.maxDrawDistance);
                if (density <= 0f) continue;

                bool useLod1 = chunk.DistanceToCamera > config.lodDistance;

                foreach (var range in chunk.Record.typeRanges)
                {
                    var type = range.typeIndex < _data.types.Length ? _data.types[range.typeIndex] : null;
                    var material = _materials.Get(range.typeIndex);
                    if (type == null || material == null) continue;

                    int drawCount = Mathf.CeilToInt(range.count * density);
                    if (drawCount <= 0) continue;

                    var mesh = useLod1 && type.HasLod1 ? type.meshLod1 : type.meshLod0;

                    _propertyBlock.Clear();
                    _propertyBlock.SetBuffer(GrassTypeMaterials.ShaderIds.Instances, chunk.InstanceBuffer);
                    _propertyBlock.SetInt(GrassTypeMaterials.ShaderIds.BaseIndex, range.start);

                    var renderParams = new RenderParams(material)
                    {
                        worldBounds = chunk.WorldBounds,
                        matProps = _propertyBlock,
                        shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                        receiveShadows = true,
                    };
                    Graphics.RenderMeshPrimitives(renderParams, mesh, 0, drawCount);

                    stats.DrawCalls++;
                    stats.VisibleInstances += drawCount;
                }
            }
        }

        public void Dispose()
        {
            // Chunk buffers are owned by GrassWorld; nothing to release here.
        }
    }
}
