using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// The PC/console tier: per-instance frustum culling, LOD selection and
    /// density thinning all run in a compute shader (GrassV2Cull.compute).
    /// Surviving instances are compacted into per-(type, LOD) visible buffers
    /// and drawn with RenderMeshIndirect — the CPU never touches instances and
    /// chunk buffers are never re-uploaded (V1 re-uploaded every frame).
    ///
    /// Flow per frame:
    ///   1. zero the counters buffer (one tiny SetData)
    ///   2. one Cull dispatch per (visible chunk, type range)
    ///   3. one WriteArgs dispatch per (type, LOD) — copies counter into args
    ///   4. one RenderMeshIndirect per (type, LOD)
    /// </summary>
    public sealed class GrassGpuDrivenTier : IGrassRenderTier
    {
        public string Name => "GPU-Driven";

        static class Ids
        {
            public static readonly int Instances = Shader.PropertyToID("_Instances");
            public static readonly int VisibleLod0 = Shader.PropertyToID("_VisibleLod0");
            public static readonly int VisibleLod1 = Shader.PropertyToID("_VisibleLod1");
            public static readonly int Counters = Shader.PropertyToID("_Counters");
            public static readonly int Args = Shader.PropertyToID("_Args");
            public static readonly int CounterBase = Shader.PropertyToID("_CounterBase");
            public static readonly int CounterIndex = Shader.PropertyToID("_CounterIndex");
            public static readonly int RangeStart = Shader.PropertyToID("_RangeStart");
            public static readonly int RangeCount = Shader.PropertyToID("_RangeCount");
            public static readonly int MaxVisible = Shader.PropertyToID("_MaxVisible");
            public static readonly int FrustumPlanes = Shader.PropertyToID("_FrustumPlanes");
            public static readonly int CameraPos = Shader.PropertyToID("_CameraPos");
            public static readonly int Distances = Shader.PropertyToID("_Distances");
            public static readonly int CullRadius = Shader.PropertyToID("_CullRadius");
        }

        const int CullThreadGroupSize = 64;
        const int StatsReadbackInterval = 15;

        /// <summary>GPU resources for one grass type: two compacted visible buffers + indirect args.</summary>
        sealed class TypeSet
        {
            public GrassType Type;
            public GraphicsBuffer VisibleLod0;
            public GraphicsBuffer VisibleLod1;   // minimal dummy when the type has no LOD1 mesh
            public GraphicsBuffer ArgsLod0;
            public GraphicsBuffer ArgsLod1;
            public GraphicsBuffer ArgsStagingLod0;
            public GraphicsBuffer ArgsStagingLod1;

            public void Dispose()
            {
                VisibleLod0?.Dispose();
                VisibleLod1?.Dispose();
                ArgsLod0?.Dispose();
                ArgsLod1?.Dispose();
                ArgsStagingLod0?.Dispose();
                ArgsStagingLod1?.Dispose();
            }
        }

        readonly GrassWorldData _data;
        readonly GrassTypeMaterials _materials;
        readonly ComputeShader _cullShader;
        readonly int _cullKernel;
        readonly int _writeArgsKernel;
        readonly TypeSet[] _typeSets;
        readonly GraphicsBuffer _counters;
        readonly uint[] _counterZeros;
        readonly Vector4[] _planeVectors = new Vector4[6];
        readonly MaterialPropertyBlock _propertyBlock = new();

        int _framesSinceReadback;
        int _lastReadbackVisible;

        public GrassGpuDrivenTier(
            GrassWorldData data,
            GrassTypeMaterials materials,
            ComputeShader cullShader,
            GrassWorldConfig config)
        {
            _data = data;
            _materials = materials;
            _cullShader = cullShader;
            _cullKernel = cullShader.FindKernel("CullInstances");
            _writeArgsKernel = cullShader.FindKernel("WriteArgs");

            _typeSets = new TypeSet[data.types.Length];
            for (int i = 0; i < data.types.Length; i++)
            {
                var type = data.types[i];
                if (type == null || !type.IsValid) continue;

                int budget = Mathf.Max(1024, config.maxVisibleInstancesPerType);
                int lod1Budget = type.HasLod1 ? budget : 16; // dummy allocation keeps bindings valid

                _typeSets[i] = new TypeSet
                {
                    Type = type,
                    VisibleLod0 = new GraphicsBuffer(GraphicsBuffer.Target.Structured, budget, GrassInstance.Stride),
                    VisibleLod1 = new GraphicsBuffer(GraphicsBuffer.Target.Structured, lod1Budget, GrassInstance.Stride),
                    ArgsLod0 = CreateArgsBuffer(type.meshLod0,
                        GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.CopyDestination),
                    ArgsLod1 = CreateArgsBuffer(type.HasLod1 ? type.meshLod1 : type.meshLod0,
                        GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.CopyDestination),
                    ArgsStagingLod0 = CreateArgsBuffer(type.meshLod0,
                        GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource),
                    ArgsStagingLod1 = CreateArgsBuffer(type.HasLod1 ? type.meshLod1 : type.meshLod0,
                        GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource),
                };
            }

            _counters = new GraphicsBuffer(GraphicsBuffer.Target.Structured, data.types.Length * 2, sizeof(uint));
            _counterZeros = new uint[data.types.Length * 2];
        }

        // D3D11 refuses compute UAVs on IndirectArguments buffers ("Failed to
        // create Compute Buffer UAV"), so WriteArgs patches instanceCount in a
        // plain Structured staging copy and Graphics.CopyBuffer moves the 20
        // bytes into the real args buffer. Both buffers hold the same 5 uints.
        static GraphicsBuffer CreateArgsBuffer(Mesh mesh, GraphicsBuffer.Target target)
        {
            var buffer = (target & GraphicsBuffer.Target.Structured) != 0
                ? new GraphicsBuffer(target, 5, sizeof(uint))
                : new GraphicsBuffer(target, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            buffer.SetData(new[]
            {
                mesh.GetIndexCount(0),
                0u, // instanceCount, written by the WriteArgs kernel each frame
                mesh.GetIndexStart(0),
                mesh.GetBaseVertex(0),
                0u, // startInstance
            });
            return buffer;
        }

        public void Render(List<GrassChunk> visibleChunks, in GrassFrameContext context, ref GrassStats stats)
        {
            var config = context.Config;

            // 1. Reset per-frame counters.
            _counters.SetData(_counterZeros);

            // 2. Frame-constant cull params.
            for (int i = 0; i < 6; i++)
            {
                var plane = context.FrustumPlanes[i];
                _planeVectors[i] = new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
            }
            _cullShader.SetVectorArray(Ids.FrustumPlanes, _planeVectors);
            _cullShader.SetVector(Ids.CameraPos, context.CameraPosition);
            _cullShader.SetBuffer(_cullKernel, Ids.Counters, _counters);

            // 3. Cull dispatch per (chunk, type range).
            foreach (var chunk in visibleChunks)
            {
                if (chunk.InstanceBuffer == null) continue;

                foreach (var range in chunk.Record.typeRanges)
                {
                    var set = range.typeIndex < _typeSets.Length ? _typeSets[range.typeIndex] : null;
                    if (set == null) continue;

                    // Types without a LOD1 mesh route everything to LOD0.
                    float lodDistance = set.Type.HasLod1 ? config.lodDistance : float.MaxValue;
                    float cullRadius = set.Type.BladeHeight * set.Type.scaleRange.y + 0.5f;

                    _cullShader.SetBuffer(_cullKernel, Ids.Instances, chunk.InstanceBuffer);
                    _cullShader.SetBuffer(_cullKernel, Ids.VisibleLod0, set.VisibleLod0);
                    _cullShader.SetBuffer(_cullKernel, Ids.VisibleLod1, set.VisibleLod1);
                    _cullShader.SetInt(Ids.CounterBase, range.typeIndex * 2);
                    _cullShader.SetInt(Ids.RangeStart, range.start);
                    _cullShader.SetInt(Ids.RangeCount, range.count);
                    _cullShader.SetInt(Ids.MaxVisible, set.VisibleLod0.count);
                    _cullShader.SetVector(Ids.Distances,
                        new Vector4(lodDistance, config.densityFalloffStart, config.maxDrawDistance, 0f));
                    _cullShader.SetFloat(Ids.CullRadius, cullRadius);

                    int groups = (range.count + CullThreadGroupSize - 1) / CullThreadGroupSize;
                    _cullShader.Dispatch(_cullKernel, groups, 1, 1);
                }
            }

            // 4. Copy counters into indirect args, then draw.
            //    Draw bounds cover the whole visible radius — culling already
            //    happened in the kernel, this only keeps SRP batching sane.
            var drawBounds = new Bounds(
                context.CameraPosition,
                Vector3.one * (config.maxDrawDistance * 2f + config.chunkSize));

            for (int typeIndex = 0; typeIndex < _typeSets.Length; typeIndex++)
            {
                var set = _typeSets[typeIndex];
                var material = _materials.Get(typeIndex);
                if (set == null || material == null) continue;

                DispatchWriteArgs(typeIndex * 2, set.ArgsStagingLod0, set.ArgsLod0, set.VisibleLod0.count);
                DrawIndirect(set.Type.meshLod0, material, set.VisibleLod0, set.ArgsLod0, drawBounds, ref stats);

                if (set.Type.HasLod1)
                {
                    DispatchWriteArgs(typeIndex * 2 + 1, set.ArgsStagingLod1, set.ArgsLod1, set.VisibleLod1.count);
                    DrawIndirect(set.Type.meshLod1, material, set.VisibleLod1, set.ArgsLod1, drawBounds, ref stats);
                }
            }

            UpdateVisibleStats(ref stats);
        }

        void DispatchWriteArgs(int counterIndex, GraphicsBuffer staging, GraphicsBuffer args, int maxVisible)
        {
            _cullShader.SetBuffer(_writeArgsKernel, Ids.Counters, _counters);
            _cullShader.SetBuffer(_writeArgsKernel, Ids.Args, staging);
            _cullShader.SetInt(Ids.CounterIndex, counterIndex);
            _cullShader.SetInt(Ids.MaxVisible, maxVisible);
            _cullShader.Dispatch(_writeArgsKernel, 1, 1, 1);
            Graphics.CopyBuffer(staging, args);
        }

        void DrawIndirect(
            Mesh mesh,
            Material material,
            GraphicsBuffer visible,
            GraphicsBuffer args,
            Bounds bounds,
            ref GrassStats stats)
        {
            _propertyBlock.Clear();
            _propertyBlock.SetBuffer(GrassTypeMaterials.ShaderIds.Instances, visible);
            _propertyBlock.SetInt(GrassTypeMaterials.ShaderIds.BaseIndex, 0);

            var renderParams = new RenderParams(material)
            {
                worldBounds = bounds,
                matProps = _propertyBlock,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = true,
            };
            Graphics.RenderMeshIndirect(renderParams, mesh, args);
            stats.DrawCalls++;
        }

        /// <summary>
        /// Visible-instance count lives on the GPU; read it back asynchronously
        /// every few frames for the stats panel (never blocks the pipeline).
        /// </summary>
        void UpdateVisibleStats(ref GrassStats stats)
        {
            if (++_framesSinceReadback >= StatsReadbackInterval)
            {
                _framesSinceReadback = 0;
                AsyncGPUReadback.Request(_counters, request =>
                {
                    if (request.hasError) return;
                    var counters = request.GetData<uint>();
                    long total = 0;
                    for (int i = 0; i < counters.Length; i++) total += counters[i];
                    _lastReadbackVisible = (int)System.Math.Min(total, int.MaxValue);
                });
            }
            stats.VisibleInstances = _lastReadbackVisible;
        }

        public long GpuBufferBytes
        {
            get
            {
                long bytes = (long)_counters.count * sizeof(uint);
                foreach (var set in _typeSets)
                {
                    if (set == null) continue;
                    bytes += (long)set.VisibleLod0.count * GrassInstance.Stride;
                    bytes += (long)set.VisibleLod1.count * GrassInstance.Stride;
                }
                return bytes;
            }
        }

        public void Dispose()
        {
            foreach (var set in _typeSets) set?.Dispose();
            _counters?.Dispose();
        }
    }
}
