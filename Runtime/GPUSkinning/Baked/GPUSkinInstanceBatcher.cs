using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.GPUSkinning
{
    /// <summary>
    /// Batches BakedAnimationSkinRenderer instances sharing the same mesh+material
    /// and renders them via Graphics.DrawMeshInstanced (up to 200 per draw call).
    /// Attach to a manager GameObject or use as a singleton.
    /// </summary>
    public class GPUSkinInstanceBatcher : MonoBehaviour
    {
        private static GPUSkinInstanceBatcher _instance;
        public static GPUSkinInstanceBatcher Instance
        {
            get
            {
                if (_instance == null && !_destroyed)
                {
                    var go = new GameObject("[GPUSkinInstanceBatcher]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<GPUSkinInstanceBatcher>();
                }
                return _instance;
            }
        }

        private static bool _destroyed;

        private const int BatchSize = 200;

        private static readonly int FrameIndexId = Shader.PropertyToID("frameIndex");
        private static readonly int PreFrameIndexId = Shader.PropertyToID("preFrameIndex");
        private static readonly int TransitionProgressId = Shader.PropertyToID("transitionProgress");

        private readonly Dictionary<int, BatchGroup> _groups = new();
        private readonly List<BatchGroup> _activeGroups = new();

        private void OnDestroy()
        {
            _destroyed = true;
            _instance = null;
        }

        /// <summary>
        /// Register an instance for batched rendering. Call every frame from LateUpdate
        /// after UpdateSkinning() but instead of calling Render() individually.
        /// </summary>
        public void Submit(BakedAnimationSkinRenderer renderer, Mesh mesh, Material material,
            Matrix4x4 localToWorld, int layer = 0)
        {
            int key = HashCode(mesh, material);

            if (!_groups.TryGetValue(key, out var group))
            {
                group = new BatchGroup(mesh, material, layer);
                _groups[key] = group;
                _activeGroups.Add(group);
            }

            group.Add(localToWorld, renderer.FrameIndex, renderer.PreFrameIndex, renderer.TransitionProgress);
        }

        private void LateUpdate()
        {
            Flush();
        }

        private void Flush()
        {
            for (int i = 0; i < _activeGroups.Count; i++)
            {
                _activeGroups[i].DrawAndReset();
            }
            _activeGroups.Clear();
            _groups.Clear();
        }

        private static int HashCode(Mesh mesh, Material material)
        {
            unchecked
            {
                return mesh.GetHashCode() * 397 ^ material.GetHashCode();
            }
        }

        private class BatchGroup
        {
            private readonly Mesh _mesh;
            private readonly Material _material;
            private readonly int _layer;
            private readonly MaterialPropertyBlock _propertyBlock = new();

            private readonly Matrix4x4[] _matrices = new Matrix4x4[BatchSize];
            private readonly float[] _frameIndices = new float[BatchSize];
            private readonly float[] _preFrameIndices = new float[BatchSize];
            private readonly float[] _transitionProgress = new float[BatchSize];
            private int _count;

            public BatchGroup(Mesh mesh, Material material, int layer)
            {
                _mesh = mesh;
                _material = material;
                _layer = layer;
            }

            public void Add(Matrix4x4 localToWorld, float frameIndex, float preFrameIndex, float transitionProgress)
            {
                if (_count >= BatchSize)
                    DrawBatch();

                _matrices[_count] = localToWorld;
                _frameIndices[_count] = frameIndex;
                _preFrameIndices[_count] = preFrameIndex;
                _transitionProgress[_count] = transitionProgress;
                _count++;
            }

            public void DrawAndReset()
            {
                if (_count > 0)
                    DrawBatch();
            }

            private void DrawBatch()
            {
                _propertyBlock.SetFloatArray(FrameIndexId, _frameIndices);
                _propertyBlock.SetFloatArray(PreFrameIndexId, _preFrameIndices);
                _propertyBlock.SetFloatArray(TransitionProgressId, _transitionProgress);

                for (int sub = 0; sub < _mesh.subMeshCount; sub++)
                {
                    Graphics.DrawMeshInstanced(
                        _mesh, sub, _material,
                        _matrices, _count,
                        _propertyBlock,
                        UnityEngine.Rendering.ShadowCastingMode.On,
                        true, _layer);
                }

                _count = 0;
            }
        }
    }
}
