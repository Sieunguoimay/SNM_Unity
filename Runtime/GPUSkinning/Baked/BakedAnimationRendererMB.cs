using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Snm.Runtime.GPUSkinning
{
    /// <summary>
    /// Renders a mesh using pre-baked animation textures.
    /// Assign a mesh, material, and baked AnimationInstancingData asset.
    /// When useInstancing is true (default), automatically batches with other
    /// BakedAnimationRendererMB instances sharing the same mesh+material via DrawMeshInstanced.
    /// Supports sparse bone overrides for IK/look-at on top of baked animation.
    /// </summary>
    public class BakedAnimationRendererMB : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private GameObject sourcePrefab;

        [Header("Runtime (auto-filled by Bake & Setup)")]
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;
        [SerializeField] private AnimationInstancingData bakedData;
        [SerializeField] private int defaultAnimation;

        [Header("Rendering")]
        [SerializeField] private bool useInstancing = true;
        [SerializeField] private ShadowCastingMode shadowCasting = ShadowCastingMode.On;
        [SerializeField] private bool receiveShadows = true;

        private Material _runtimeMaterial;
        private Material _overrideMaterial;
        private BakedAnimationPlayer _player;
        private AnimationTextureData _textureData;
        private MaterialPropertyBlock _propertyBlock;
        private Matrix4x4[] _bindposes;

        private static readonly int BoneTextureId = Shader.PropertyToID("_boneTexture");
        private static readonly int BoneTextureWidthId = Shader.PropertyToID("_boneTextureWidth");
        private static readonly int BoneTextureHeightId = Shader.PropertyToID("_boneTextureHeight");
        private static readonly int BoneTextureBlockWidthId = Shader.PropertyToID("_boneTextureBlockWidth");
        private static readonly int BoneTextureBlockHeightId = Shader.PropertyToID("_boneTextureBlockHeight");
        private static readonly int FrameIndexId = Shader.PropertyToID("frameIndex");
        private static readonly int PreFrameIndexId = Shader.PropertyToID("preFrameIndex");
        private static readonly int TransitionProgressId = Shader.PropertyToID("transitionProgress");
        private static readonly int BoneOverrideCountId = Shader.PropertyToID("_BoneOverrideCount");
        private static readonly int BoneOverrideIndicesId = Shader.PropertyToID("_BoneOverrideIndices");
        private static readonly int BoneOverrideMatricesId = Shader.PropertyToID("_BoneOverrideMatrices");
        private static readonly int BoneOverrideWeightsId = Shader.PropertyToID("_BoneOverrideWeights");

        private const int MaxBoneOverrides = 8;

        // Bone override state
        private readonly struct BoneOverrideSlot
        {
            public readonly int BoneIndex;
            public readonly Matrix4x4 Matrix;
            public readonly float Weight;

            public BoneOverrideSlot(int boneIndex, Matrix4x4 matrix, float weight)
            {
                BoneIndex = boneIndex;
                Matrix = matrix;
                Weight = weight;
            }
        }

        private readonly List<BoneOverrideSlot> _overrides = new(MaxBoneOverrides);
        private readonly float[] _overrideIndices = new float[MaxBoneOverrides];
        private readonly Matrix4x4[] _overrideMatrices = new Matrix4x4[MaxBoneOverrides];
        private readonly float[] _overrideWeights = new float[MaxBoneOverrides];
        private bool _hasOverrides;

        /// <summary>
        /// Animation playback controller. Use to Play, CrossFade, Pause, etc.
        /// </summary>
        public BakedAnimationPlayer Player => _player;

        /// <summary>
        /// True if any bone overrides are currently active.
        /// When active, this instance renders individually (not instanced).
        /// </summary>
        public bool HasBoneOverrides => _hasOverrides;

        // --- Shared material pool ---
        private static readonly Dictionary<long, Material> SharedMaterials = new();
        private static readonly Dictionary<long, int> SharedMaterialRefCounts = new();
        private long _materialKey;

        private void OnEnable()
        {
            Setup();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            Cleanup();
            Setup();
        }

        private void Setup()
        {
            if (mesh == null || material == null || bakedData == null) return;

            _textureData = bakedData.animationTextureData;
            _runtimeMaterial = GetOrRetainSharedMaterial(material, bakedData, _textureData);
            _player = new BakedAnimationPlayer(bakedData);
            _player.Play(defaultAnimation);
            _propertyBlock = new MaterialPropertyBlock();
            _bindposes = mesh.bindposes;
        }

        private void Cleanup()
        {
            ReleaseSharedMaterial(_materialKey);
            _runtimeMaterial = null;
            if (_overrideMaterial != null) { Destroy(_overrideMaterial); _overrideMaterial = null; }
            _player = null;
            _propertyBlock = null;
            _bindposes = null;
            _overrides.Clear();
            _hasOverrides = false;
        }

        // =============================================
        // Bone Override API
        // =============================================

        /// <summary>
        /// Override a bone's matrix in root-local space (same space as baked matrices).
        /// Use this when you already have the matrix in the correct space:
        ///   overrideMatrix = character.worldToLocalMatrix * desiredBoneWorldMatrix * mesh.bindposes[boneIndex]
        /// </summary>
        /// <param name="boneIndex">Index of the bone in the skeleton.</param>
        /// <param name="rootLocalMatrix">The bone matrix in root-local space.</param>
        /// <param name="weight">Blend weight (0 = fully baked, 1 = fully overridden).</param>
        public void SetBoneOverride(int boneIndex, Matrix4x4 rootLocalMatrix, float weight = 1f)
        {
            if (_overrides.Count >= MaxBoneOverrides)
            {
                for (int i = 0; i < _overrides.Count; i++)
                {
                    if (_overrides[i].BoneIndex == boneIndex)
                    {
                        _overrides[i] = new BoneOverrideSlot(boneIndex, rootLocalMatrix, weight);
                        _hasOverrides = true;
                        return;
                    }
                }
                Debug.LogWarning($"BoneOverride: max {MaxBoneOverrides} overrides reached, ignoring bone {boneIndex}");
                return;
            }

            for (int i = 0; i < _overrides.Count; i++)
            {
                if (_overrides[i].BoneIndex == boneIndex)
                {
                    _overrides[i] = new BoneOverrideSlot(boneIndex, rootLocalMatrix, weight);
                    _hasOverrides = true;
                    return;
                }
            }

            _overrides.Add(new BoneOverrideSlot(boneIndex, rootLocalMatrix, weight));
            _hasOverrides = true;
        }

        /// <summary>
        /// Override a bone using a desired world-space matrix.
        /// Automatically converts to root-local space using the character's transform and mesh bindpose.
        /// </summary>
        /// <param name="boneIndex">Index of the bone in the skeleton.</param>
        /// <param name="desiredWorldMatrix">The desired bone transform in world space.</param>
        /// <param name="weight">Blend weight (0 = fully baked, 1 = fully overridden).</param>
        public void SetBoneOverrideWorld(int boneIndex, Matrix4x4 desiredWorldMatrix, float weight = 1f)
        {
            if (_bindposes == null || boneIndex < 0 || boneIndex >= _bindposes.Length) return;
            var rootLocalMatrix = transform.worldToLocalMatrix * desiredWorldMatrix * _bindposes[boneIndex];
            SetBoneOverride(boneIndex, rootLocalMatrix, weight);
        }

        /// <summary>
        /// Remove a specific bone override. The bone returns to its baked animation pose.
        /// </summary>
        public void ClearBoneOverride(int boneIndex)
        {
            for (int i = _overrides.Count - 1; i >= 0; i--)
            {
                if (_overrides[i].BoneIndex == boneIndex)
                {
                    _overrides.RemoveAt(i);
                    break;
                }
            }
            _hasOverrides = _overrides.Count > 0;
        }

        /// <summary>
        /// Remove all bone overrides. All bones return to baked animation.
        /// The instance will resume instanced batching if useInstancing is true.
        /// </summary>
        public void ClearAllBoneOverrides()
        {
            _overrides.Clear();
            _hasOverrides = false;
        }

        // =============================================
        // Rendering
        // =============================================

        private void LateUpdate()
        {
            if (_player == null || _runtimeMaterial == null) return;

            _player.Update(Time.deltaTime);

            if (!_hasOverrides && useInstancing)
            {
                GPUSkinInstanceBatcher.Instance.Submit(
                    mesh, _runtimeMaterial,
                    transform.localToWorldMatrix,
                    _player.FrameIndex, _player.PreFrameIndex, _player.TransitionProgress,
                    gameObject.layer, shadowCasting, receiveShadows);
            }
            else
            {
                var mat = _hasOverrides ? GetOrCreateOverrideMaterial() : _runtimeMaterial;
                SetPropertyBlock();
                if (_hasOverrides) SetOverridePropertyBlock();
                Graphics.DrawMesh(mesh, transform.localToWorldMatrix, mat,
                    gameObject.layer, null, 0, _propertyBlock,
                    shadowCasting, receiveShadows);
            }
        }

        private void SetPropertyBlock()
        {
            int texIdx = _player.TextureIndex;
            if (_textureData?.bakedBoneTextures != null && texIdx < _textureData.bakedBoneTextures.Length)
            {
                var tex = _textureData.bakedBoneTextures[texIdx];
                _propertyBlock.SetTexture(BoneTextureId, tex);
                _propertyBlock.SetInt(BoneTextureWidthId, tex.width);
                _propertyBlock.SetInt(BoneTextureHeightId, tex.height);
                _propertyBlock.SetInt(BoneTextureBlockWidthId, _textureData.textureBlockWidth);
                _propertyBlock.SetInt(BoneTextureBlockHeightId, _textureData.textureBlockHeight);
            }
            _propertyBlock.SetFloat(FrameIndexId, _player.FrameIndex);
            _propertyBlock.SetFloat(PreFrameIndexId, _player.PreFrameIndex);
            _propertyBlock.SetFloat(TransitionProgressId, _player.TransitionProgress);
        }

        private void SetOverridePropertyBlock()
        {
            int count = Mathf.Min(_overrides.Count, MaxBoneOverrides);
            for (int i = 0; i < count; i++)
            {
                _overrideIndices[i] = _overrides[i].BoneIndex;
                _overrideMatrices[i] = _overrides[i].Matrix;
                _overrideWeights[i] = _overrides[i].Weight;
            }
            _propertyBlock.SetInt(BoneOverrideCountId, count);
            _propertyBlock.SetFloatArray(BoneOverrideIndicesId, _overrideIndices);
            _propertyBlock.SetMatrixArray(BoneOverrideMatricesId, _overrideMatrices);
            _propertyBlock.SetFloatArray(BoneOverrideWeightsId, _overrideWeights);
        }

        private Material GetOrCreateOverrideMaterial()
        {
            if (_overrideMaterial != null) return _overrideMaterial;

            _overrideMaterial = Instantiate(_runtimeMaterial);
            _overrideMaterial.name = $"{_runtimeMaterial.name}_Override";
            _overrideMaterial.EnableKeyword("BONE_OVERRIDE_ON");
            _overrideMaterial.enableInstancing = false;
            return _overrideMaterial;
        }

        // =============================================
        // Shared material management
        // =============================================

        private Material GetOrRetainSharedMaterial(
            Material baseMaterial, AnimationInstancingData data, AnimationTextureData texData)
        {
            _materialKey = SharedMaterialKey(baseMaterial, data);

            if (!SharedMaterials.TryGetValue(_materialKey, out var mat) || mat == null)
            {
                mat = Instantiate(baseMaterial);
                mat.name = $"{baseMaterial.name}_Baked_{data.name}";
                mat.EnableKeyword("BAKED_SKINNING_ON");
                mat.DisableKeyword("GPU_SKINNING_ON");
                mat.enableInstancing = true;

                if (texData?.bakedBoneTextures != null && texData.bakedBoneTextures.Length > 0)
                {
                    var tex = texData.bakedBoneTextures[0];
                    mat.SetTexture(BoneTextureId, tex);
                    mat.SetInt(BoneTextureWidthId, tex.width);
                    mat.SetInt(BoneTextureHeightId, tex.height);
                    mat.SetInt(BoneTextureBlockWidthId, texData.textureBlockWidth);
                    mat.SetInt(BoneTextureBlockHeightId, texData.textureBlockHeight);
                }

                SharedMaterials[_materialKey] = mat;
                SharedMaterialRefCounts[_materialKey] = 0;
            }

            SharedMaterialRefCounts[_materialKey]++;
            return mat;
        }

        private static void ReleaseSharedMaterial(long key)
        {
            if (!SharedMaterialRefCounts.ContainsKey(key)) return;

            SharedMaterialRefCounts[key]--;
            if (SharedMaterialRefCounts[key] <= 0)
            {
                if (SharedMaterials.TryGetValue(key, out var mat) && mat != null)
                    DestroyImmediate(mat);
                SharedMaterials.Remove(key);
                SharedMaterialRefCounts.Remove(key);
            }
        }

        private static long SharedMaterialKey(Material baseMaterial, AnimationInstancingData data)
        {
            unchecked
            {
                return ((long)baseMaterial.GetInstanceID() << 32) | (uint)data.GetInstanceID();
            }
        }
    }
}
